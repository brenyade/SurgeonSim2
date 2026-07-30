using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Scoring;
using TraumaSurgeon.Surgery;
using TraumaSurgeon.Tools;
using UnityEngine;

namespace TraumaSurgeon.Staff
{
    /// <summary>Command metadata for the wheel UI.</summary>
    public struct StaffCommandInfo
    {
        public StaffCommand Command;
        public string Label;
        public string Spoken;
        public StaffRole Role;
        public KeyCode Shortcut;
    }

    /// <summary>
    /// Routes the player's orders to the right team member, applies the effect and manages the
    /// assistance budget (difficulties limit how much help you can call for).
    /// </summary>
    public class StaffCommandSystem : MonoBehaviour
    {
        public static readonly StaffCommandInfo[] Commands =
        {
            new StaffCommandInfo { Command = StaffCommand.IncreaseAnesthesia, Label = "Increase anaesthesia",
                Spoken = "Deepen the anaesthetic.", Role = StaffRole.Anesthesiologist, Shortcut = KeyCode.F1 },
            new StaffCommandInfo { Command = StaffCommand.DecreaseAnesthesia, Label = "Lighten anaesthesia",
                Spoken = "Lighten him up a little.", Role = StaffRole.Anesthesiologist, Shortcut = KeyCode.F2 },
            new StaffCommandInfo { Command = StaffCommand.GiveBlood, Label = "Give blood",
                Spoken = "Hang a unit of packed cells.", Role = StaffRole.Anesthesiologist, Shortcut = KeyCode.F3 },
            new StaffCommandInfo { Command = StaffCommand.StartCompressions, Label = "Start compressions",
                Spoken = "Start compressions!", Role = StaffRole.SurgicalAssistant, Shortcut = KeyCode.F4 },
            new StaffCommandInfo { Command = StaffCommand.PrepareDefibrillator, Label = "Prepare defibrillator",
                Spoken = "Charge the defibrillator.", Role = StaffRole.CirculatingNurse, Shortcut = KeyCode.F5 },
            new StaffCommandInfo { Command = StaffCommand.Suction, Label = "Suction",
                Spoken = "Suction here, please.", Role = StaffRole.SurgicalAssistant, Shortcut = KeyCode.F6 },
            new StaffCommandInfo { Command = StaffCommand.HoldRetraction, Label = "Hold retraction",
                Spoken = "Hold this retractor.", Role = StaffRole.SurgicalAssistant, Shortcut = KeyCode.F7 },
            new StaffCommandInfo { Command = StaffCommand.CallAnotherSurgeon, Label = "Call another surgeon",
                Spoken = "Get me another pair of hands.", Role = StaffRole.ConsultantSurgeon, Shortcut = KeyCode.F8 },
            new StaffCommandInfo { Command = StaffCommand.RequestImaging, Label = "Request imaging",
                Spoken = "Put the imaging back up.", Role = StaffRole.CirculatingNurse, Shortcut = KeyCode.F9 },
            new StaffCommandInfo { Command = StaffCommand.PrepareEmergencyClosure, Label = "Emergency closure",
                Spoken = "Prepare for damage control closure.", Role = StaffRole.ScrubNurse, Shortcut = KeyCode.F10 },
            new StaffCommandInfo { Command = StaffCommand.CountSponges, Label = "Count sponges",
                Spoken = "Give me a sponge count.", Role = StaffRole.CirculatingNurse, Shortcut = KeyCode.F11 },
            new StaffCommandInfo { Command = StaffCommand.GiveAntibiotics, Label = "Give antibiotics",
                Spoken = "Two grams of cefazolin, please.", Role = StaffRole.Anesthesiologist, Shortcut = KeyCode.F12 }
        };

        private readonly Dictionary<StaffRole, StaffAIController> _staff =
            new Dictionary<StaffRole, StaffAIController>();

        private PatientController _patient;
        private ObjectiveManager _objectives;
        private ToolController _tools;
        private SpongeTracker _sponges;
        private DifficultyProfile _difficulty;

        private int _assistanceUsed;
        private bool _defibReady;

        /// <summary>Remaining assistance uses, or -1 for unlimited.</summary>
        public int AssistanceRemaining => _difficulty == null || _difficulty.AssistanceUses < 0
            ? -1
            : Mathf.Max(0, _difficulty.AssistanceUses - _assistanceUsed);

        public void Initialise(PatientController patient, ObjectiveManager objectives, ToolController tools,
            SpongeTracker sponges, DifficultyProfile difficulty, IEnumerable<StaffAIController> team)
        {
            _patient = patient;
            _objectives = objectives;
            _tools = tools;
            _sponges = sponges;
            _difficulty = difficulty;
            _assistanceUsed = 0;

            _staff.Clear();
            if (team != null)
            {
                foreach (StaffAIController member in team)
                {
                    _staff[member.Role] = member;
                }
            }
        }

        private void Update()
        {
            // Assistant-driven continuous help.
            if (_patient == null)
            {
                return;
            }

            if (_staff.TryGetValue(StaffRole.SurgicalAssistant, out StaffAIController assistant) && assistant != null)
            {
                if (assistant.PerformingCompressions)
                {
                    if (_patient.Vitals.InCardiacArrest)
                    {
                        _patient.DeliverCompression();
                    }
                    else
                    {
                        assistant.SetCompressions(false);
                        assistant.Say("Stopping compressions - we have a rhythm.");
                    }
                }

                if (assistant.HoldingSuction)
                {
                    _patient.BloodLoss.Suction(55f * assistant.Competence * Time.deltaTime);
                }
            }

            // Keyboard shortcuts for the command wheel entries.
            foreach (StaffCommandInfo info in Commands)
            {
                if (UnityEngine.Input.GetKeyDown(info.Shortcut))
                {
                    Issue(info.Command);
                }
            }
        }

        public StaffAIController Get(StaffRole role)
        {
            return _staff.TryGetValue(role, out StaffAIController member) ? member : null;
        }

        /// <summary>Issues a command. Returns false when it was refused (out of assistance, etc).</summary>
        public bool Issue(StaffCommand command)
        {
            if (_patient == null)
            {
                return false;
            }

            StaffCommandInfo info = FindInfo(command);
            StaffAIController member = Get(info.Role);

            if (AudioManager.Exists)
            {
                AudioManager.Instance.Play(SoundId.UiClick, 0.4f);
            }

            GameEvents.RaiseCommandIssued(command);

            switch (command)
            {
                case StaffCommand.IncreaseAnesthesia:
                    _patient.Anesthesia.AdjustTarget(+10f);
                    Respond(member, "Deepening the anaesthetic now.");
                    break;

                case StaffCommand.DecreaseAnesthesia:
                    _patient.Anesthesia.AdjustTarget(-10f);
                    Respond(member, "Lightening him up.");
                    break;

                case StaffCommand.GiveBlood:
                    if (_patient.BloodLoss.Transfuse())
                    {
                        Respond(member, $"Unit going in. {_patient.BloodLoss.AvailableUnits} left in the cooler.");
                    }
                    else
                    {
                        Respond(member, "That was the last unit - we are out.");
                        return false;
                    }

                    break;

                case StaffCommand.StartCompressions:
                    if (!_patient.Vitals.InCardiacArrest)
                    {
                        Respond(member, "He still has a rhythm - I am not compressing.");
                        GameEvents.RaiseScoreEvent(ScoreEventType.UnnecessaryAction, 1f,
                            "compressions ordered on a perfusing rhythm");
                        return false;
                    }

                    if (member != null)
                    {
                        member.SetCompressions(true);
                    }

                    Respond(member, "Compressions started.");
                    break;

                case StaffCommand.PrepareDefibrillator:
                    _defibReady = true;
                    if (_tools != null && !_tools.Has(ToolType.Defibrillator))
                    {
                        var list = new List<ToolType>(_tools.Available) { ToolType.Defibrillator };
                        _tools.SetAvailableTools(list);
                    }

                    Respond(member, "Defibrillator is on the field and charging.");
                    break;

                case StaffCommand.Suction:
                    if (member != null)
                    {
                        member.SetSuction(!member.HoldingSuction);
                        Respond(member, member.HoldingSuction ? "Suctioning." : "Suction off.");
                    }

                    break;

                case StaffCommand.HoldRetraction:
                    if (member != null)
                    {
                        member.SetRetraction(true);
                        HoldRetractionOnOpenLayers();
                        Respond(member, "I have the retractor.");
                    }

                    break;

                case StaffCommand.CallAnotherSurgeon:
                    if (!SpendAssistance())
                    {
                        Respond(Get(StaffRole.ConsultantSurgeon), "Nobody is free - you are on your own.");
                        return false;
                    }

                    if (_objectives != null)
                    {
                        _objectives.ForceCompleteCurrent("consultant assistance");
                    }

                    Respond(Get(StaffRole.ConsultantSurgeon), "I have got this part. Keep moving.");
                    break;

                case StaffCommand.RequestImaging:
                    Respond(member, "Imaging back up on the display.");
                    GameEvents.RaiseActionPerformed(SurgicalActionType.ReviewImaging, null, 1f);
                    break;

                case StaffCommand.PrepareEmergencyClosure:
                    PerformEmergencyClosure();
                    Respond(member, "Damage control closure - packing and temporary cover.");
                    break;

                case StaffCommand.CountSponges:
                    if (_sponges != null)
                    {
                        string result = _sponges.PerformCount();
                        GameEvents.RaiseNotification(result, _sponges.CountIsCorrect
                            ? NotificationType.Success
                            : NotificationType.Warning);
                    }

                    break;

                case StaffCommand.GiveAntibiotics:
                    _patient.Medications.Administer("cefazolin", 2f);
                    Respond(member, "Cefazolin two grams in.");
                    break;
            }

            return true;
        }

        public bool DefibrillatorReady => _defibReady;

        private void Respond(StaffAIController member, string line)
        {
            if (member != null)
            {
                member.Say(line, true);
            }
            else
            {
                GameEvents.RaiseStaffSpeech(StaffRole.CirculatingNurse, line);
            }
        }

        private bool SpendAssistance()
        {
            if (_difficulty == null || _difficulty.AssistanceUses < 0)
            {
                return true;
            }

            if (_assistanceUsed >= _difficulty.AssistanceUses)
            {
                return false;
            }

            _assistanceUsed++;
            return true;
        }

        private void HoldRetractionOnOpenLayers()
        {
            if (_patient == null || _patient.Body == null)
            {
                return;
            }

            foreach (AnatomyPart part in _patient.Body.AllParts)
            {
                if (part.blocksAccess && part.isOpen)
                {
                    part.SetRetraction(1f);
                    foreach (AnatomyPart covered in part.Covered)
                    {
                        covered.SetAccessible(true);
                    }
                }
            }
        }

        /// <summary>
        /// Damage control: closes the skin immediately at a scoring cost. A legitimate choice when
        /// the patient is crashing, but never as good as a proper closure.
        /// </summary>
        private void PerformEmergencyClosure()
        {
            if (_patient == null || _patient.Body == null)
            {
                return;
            }

            foreach (AnatomyPart part in _patient.Body.AllParts)
            {
                if (part.layer == AnatomyLayer.Skin && part.isOpen && !part.isRepaired)
                {
                    part.suturesPlaced = part.suturesRequired;
                    part.isRepaired = true;
                    part.state = DamageState.Repaired;
                    part.RefreshVisual();
                    GameEvents.RaiseActionPerformed(SurgicalActionType.Close, part, 0.45f);
                }
            }

            GameEvents.RaiseScoreEvent(ScoreEventType.PrecisionPoor, 0.3f, "emergency closure");
            _patient.Vitals.infectionRisk = Mathf.Min(100f, _patient.Vitals.infectionRisk + 15f);
        }

        private static StaffCommandInfo FindInfo(StaffCommand command)
        {
            foreach (StaffCommandInfo info in Commands)
            {
                if (info.Command == command)
                {
                    return info;
                }
            }

            return Commands[0];
        }
    }
}
