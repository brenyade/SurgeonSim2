using System.Collections;
using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Career;
using TraumaSurgeon.Complications;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Environment;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Player;
using TraumaSurgeon.Scoring;
using TraumaSurgeon.Staff;
using TraumaSurgeon.Tools;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>
    /// Owns one operation from set-up to post-op report. Builds the patient, spawns the team,
    /// wires the objective/complication/scoring systems together and decides when the case ends.
    /// </summary>
    public class SurgeryManager : MonoBehaviour
    {
        public static SurgeryManager Active { get; private set; }

        public ProcedureData Procedure { get; private set; }
        public DifficultyLevel Difficulty { get; private set; }
        public DifficultyProfile Profile { get; private set; }
        public GameModeType Mode { get; private set; }
        public ChallengeData Challenge { get; private set; }

        public PatientController Patient { get; private set; }
        public ObjectiveManager Objectives { get; private set; }
        public ComplicationManager Complications { get; private set; }
        public ScoringManager Scoring { get; private set; }
        public SpongeTracker Sponges { get; private set; }
        public StaffCommandSystem Commands { get; private set; }
        public LaparoscopySystem Laparoscopy { get; private set; }
        public OperatingRoomBuilder Room { get; private set; }
        public PlayerRigBuilder.Rig PlayerRig { get; private set; }

        public bool IsRunning { get; private set; }
        public bool IsComplete { get; private set; }

        /// <summary>Remaining seconds for timed challenges, or -1 when untimed.</summary>
        public float TimeRemaining { get; private set; } = -1f;

        private readonly List<StaffAIController> _team = new List<StaffAIController>();
        private float _endDelay;
        private bool _endQueued;
        private bool _closureReported;

        public IReadOnlyList<StaffAIController> Team => _team;

        // ---- Setup ------------------------------------------------------------

        public static SurgeryManager Create(Transform parent)
        {
            var go = new GameObject("SurgeryManager");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go.AddComponent<SurgeryManager>();
        }

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            SurgeryServices.Clear();
            if (AudioManager.Exists)
            {
                AudioManager.Instance.DetachMonitor();
            }
        }

        /// <summary>Builds and starts a case.</summary>
        public void SetupCase(ProcedureData procedure, DifficultyLevel difficulty, GameModeType mode,
            OperatingRoomBuilder room, PlayerRigBuilder.Rig rig, ChallengeData challenge = null)
        {
            Procedure = procedure;
            Difficulty = difficulty;
            Profile = DifficultyProfile.Get(difficulty);
            Mode = mode;
            Challenge = challenge;
            Room = room;
            PlayerRig = rig;
            IsComplete = false;
            _endQueued = false;
            _closureReported = false;

            BuildPatient();
            BuildSubsystems();
            BuildTeam();
            BuildStations();
            ConfigureTools();
            ConfigureChallenge();
            ApplyPresentingInjuries();
            ApplyDifficultyGuidance();

            SurgeryServices.Patient = Patient;
            SurgeryServices.Sponges = Sponges;
            SurgeryServices.Laparoscopy = Laparoscopy;

            if (AudioManager.Exists)
            {
                AudioManager.Instance.AttachMonitor(Patient);
                AudioManager.Instance.StartLoop(SoundId.Ambience, 0.5f);
            }

            Scoring.BeginSurgery();
            IsRunning = true;

            GameEvents.RaiseNotification($"Case started: {procedure.displayName}", NotificationType.Info);
            GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist,
                $"Patient is {procedure.patient.name}, {procedure.patient.age} years old. Ready when you are.");
        }

        private void BuildPatient()
        {
            var go = new GameObject("Patient");
            go.transform.SetParent(Room != null ? Room.transform : transform, false);
            if (Room != null && Room.PatientAnchor != null)
            {
                go.transform.position = Room.PatientAnchor.position;
                go.transform.rotation = Room.PatientAnchor.rotation;
            }

            Patient = go.AddComponent<PatientController>();
            Patient.Initialise(Procedure.patient, Profile);
        }

        private void BuildSubsystems()
        {
            Objectives = gameObject.AddComponent<ObjectiveManager>();
            Complications = gameObject.AddComponent<ComplicationManager>();
            Scoring = gameObject.AddComponent<ScoringManager>();
            Sponges = gameObject.AddComponent<SpongeTracker>();
            Commands = gameObject.AddComponent<StaffCommandSystem>();
            Laparoscopy = gameObject.AddComponent<LaparoscopySystem>();

            Sponges.Patient = Patient;
            Objectives.Initialise(Procedure, PlayerRig != null ? PlayerRig.Tools : null);
            Complications.Initialise(Patient, Profile, Procedure.possibleComplications);
        }

        private void BuildTeam()
        {
            _team.Clear();
            if (Room == null)
            {
                return;
            }

            var roles = new[]
            {
                StaffRole.Anesthesiologist, StaffRole.ScrubNurse, StaffRole.CirculatingNurse,
                StaffRole.SurgicalAssistant, StaffRole.ConsultantSurgeon
            };

            var names = new[] { "Dr. Okafor", "Nurse Bell", "Nurse Ramos", "Dr. Lindqvist", "Dr. Marchetti" };

            for (int i = 0; i < roles.Length && i < Room.StaffStations.Count; i++)
            {
                int skill = CareerManager.Exists ? CareerManager.Instance.GetStaffSkill(roles[i]) : 4;
                StaffAIController member = StaffAIController.Spawn(roles[i], names[i], skill,
                    Room.StaffStations[i], Room.transform);
                _team.Add(member);
            }

            Commands.Initialise(Patient, Objectives, PlayerRig != null ? PlayerRig.Tools : null,
                Sponges, Profile, _team);
        }

        /// <summary>
        /// Creates the interactable stations around the theatre: the scrub sink, the patient prep
        /// zone and the imaging display. These drive the non-instrument steps of a procedure.
        /// </summary>
        private void BuildStations()
        {
            if (Room == null)
            {
                return;
            }

            Room.AddInteractable("Station_ScrubSink", new Vector3(3.2f, 1.2f, 2.6f),
                new Vector3(1f, 0.6f, 0.9f), "Scrub in",
                _ =>
                {
                    if (Patient.IsScrubbedIn)
                    {
                        GameEvents.RaiseNotification("Already scrubbed and gowned.", NotificationType.Info);
                        return;
                    }

                    GameEvents.RaiseActionPerformed(SurgicalActionType.ScrubIn, null, 1f);
                    GameEvents.RaiseNotification("Scrubbed, gowned and gloved.", NotificationType.Success);
                    GameEvents.RaiseStaffSpeech(StaffRole.ScrubNurse, "Gown and gloves are on. Table is ready.");
                });

            // Placed at the foot of the table so it never sits between the surgeon and the field.
            Room.AddInteractable("Station_Prep", new Vector3(0f, 1.12f, -1.25f),
                new Vector3(0.6f, 0.4f, 0.5f), "Prep and drape the patient",
                _ =>
                {
                    if (Patient.IsPrepped)
                    {
                        GameEvents.RaiseNotification("Patient is already prepped.", NotificationType.Info);
                        return;
                    }

                    if (!Patient.IsScrubbedIn)
                    {
                        GameEvents.RaiseNotification("Scrub in at the sink first.", NotificationType.Warning);
                        return;
                    }

                    GameEvents.RaiseActionPerformed(SurgicalActionType.PrepPatient, null, 1f);
                    GameEvents.RaiseNotification("Skin prepped, drapes on, anaesthesia induced.",
                        NotificationType.Success);
                });

            Room.AddInteractable("Station_Imaging", new Vector3(-2.4f, 1.7f, 2.35f),
                new Vector3(1.2f, 0.9f, 0.4f), "Review imaging",
                _ =>
                {
                    GameEvents.RaiseActionPerformed(SurgicalActionType.ReviewImaging, null, 1f);
                    GameEvents.RaiseNotification("Imaging reviewed - see the patient chart (Tab) for details.",
                        NotificationType.Info);
                });
        }

        private void ConfigureTools()
        {
            if (PlayerRig == null || PlayerRig.Tools == null)
            {
                return;
            }

            var tools = new List<ToolType>();
            foreach (string id in Procedure.requiredTools)
            {
                if (System.Enum.TryParse(id, true, out ToolType type))
                {
                    tools.Add(type);
                }
            }

            // Always-available basics.
            foreach (ToolType basic in new[] { ToolType.Sponge, ToolType.Suction, ToolType.Syringe })
            {
                if (!tools.Contains(basic))
                {
                    tools.Add(basic);
                }
            }

            PlayerRig.Tools.SetAvailableTools(tools);
        }

        private void ConfigureChallenge()
        {
            TimeRemaining = -1f;

            if (Challenge == null)
            {
                return;
            }

            if (Challenge.timeLimitSeconds > 0f)
            {
                TimeRemaining = Challenge.timeLimitSeconds;
            }

            if (Challenge.powerFailure && Room != null)
            {
                Room.SetLightsEnabled(false);
                GameEvents.RaiseNotification("Power failure - running on emergency lighting.",
                    NotificationType.Critical);
            }

            if (Challenge.limitedBlood)
            {
                Patient.BloodLoss.AvailableUnits = 2;
            }

            if (Challenge.faultyEquipment)
            {
                Complications.EquipmentReliability = 0.5f;
            }

            if (Challenge.noAssistance)
            {
                GameEvents.RaiseNotification("No assistance available for this challenge.",
                    NotificationType.Warning);
            }
        }

        private void ApplyPresentingInjuries()
        {
            if (Procedure.injuries == null)
            {
                return;
            }

            foreach (InjuryData injury in Procedure.injuries)
            {
                AnatomyPart part = Patient.Body.Get(injury.partId);
                if (part == null)
                {
                    Debug.LogWarning($"[SurgeryManager] Unknown anatomy id in {Procedure.id}: {injury.partId}");
                    continue;
                }

                // The hematoma is hidden by default - reveal it for neuro cases.
                if (part.partId == AnatomyIds.Hematoma && part.MeshRoot != null)
                {
                    part.MeshRoot.gameObject.SetActive(true);
                }

                Patient.OrganDamage.ApplyPresentingInjury(part, injury.State, injury.severity,
                    injury.bleedRate, injury.requiresRepair, injury.requiresRemoval);
            }

            // Gunshot cases carry a retained bullet.
            if (Procedure.id == "gunshot_abdomen")
            {
                AnatomyPart bowel = Patient.Body.Get(AnatomyIds.Intestines);
                GameObject bullet = ForeignObjectSystem.Spawn(bowel, AnatomyIds.Bullet,
                    new Vector3(0.03f, 0.02f, 0.02f));
                Patient.Body.RegisterForeignObject(bullet);
            }
        }

        private void ApplyDifficultyGuidance()
        {
            if (Patient.Body == null)
            {
                return;
            }

            Patient.Body.SetGuidesVisible(Profile.HighlightAnatomy);
            if (!Profile.HighlightAnatomy)
            {
                Patient.Body.ClearHighlights();
            }
        }

        // ---- Runtime ----------------------------------------------------------

        private void OnEnable()
        {
            GameEvents.ActionPerformed += OnActionPerformed;
            GameEvents.PatientDied += OnPatientDied;
        }

        private void OnDisable()
        {
            GameEvents.ActionPerformed -= OnActionPerformed;
            GameEvents.PatientDied -= OnPatientDied;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            UpdateHighlighting();
            UpdateTimeLimit();
            UpdateCompletionCheck();

            if (_endQueued)
            {
                _endDelay -= Time.deltaTime;
                if (_endDelay <= 0f)
                {
                    _endQueued = false;
                    FinishSurgery();
                }
            }
        }

        /// <summary>Highlights the structure the current objective points at (guided difficulties).</summary>
        private void UpdateHighlighting()
        {
            if (!Profile.HighlightAnatomy || Patient.Body == null || Objectives == null)
            {
                return;
            }

            string targetId = Objectives.CurrentTargetPart();
            foreach (AnatomyPart part in Patient.Body.AllParts)
            {
                part.SetHighlight(!string.IsNullOrEmpty(targetId) && part.partId == targetId && !part.isRemoved);
            }
        }

        private void UpdateTimeLimit()
        {
            if (TimeRemaining < 0f)
            {
                return;
            }

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                GameEvents.RaiseNotification("Time limit reached.", NotificationType.Critical);
                QueueEnd(1f);
            }
        }

        private void UpdateCompletionCheck()
        {
            if (Objectives == null || !Objectives.AllComplete || IsComplete)
            {
                return;
            }

            if (!_closureReported)
            {
                _closureReported = true;
                Sponges.FinaliseAtClosure();
                Patient.MarkClosed();
                GameEvents.RaiseNotification("Procedure complete.", NotificationType.Success);
                if (AudioManager.Exists)
                {
                    AudioManager.Instance.Play(SoundId.SuccessChime, 0.7f);
                }

                QueueEnd(2.5f);
            }
        }

        private void OnActionPerformed(SurgicalActionType action, AnatomyPart target, float quality)
        {
            if (Complications != null && PlayerRig != null && PlayerRig.Tools != null)
            {
                Complications.NotifyAction(action, PlayerRig.Tools.EquippedType);
            }

            switch (action)
            {
                case SurgicalActionType.ScrubIn:
                    Patient.IsScrubbedIn = true;
                    break;
                case SurgicalActionType.PrepPatient:
                    Patient.IsPrepped = true;
                    if (!Patient.Anesthesia.IsInduced)
                    {
                        Patient.Anesthesia.Induce();
                    }

                    break;
            }
        }

        private void OnPatientDied()
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.Play(SoundId.FailureBuzz, 0.8f);
            }

            QueueEnd(4f);
        }

        public void QueueEnd(float delaySeconds)
        {
            if (_endQueued || IsComplete)
            {
                return;
            }

            _endQueued = true;
            _endDelay = delaySeconds;
        }

        /// <summary>Aborts the case immediately (pause menu "abandon operation").</summary>
        public void Abort()
        {
            QueueEnd(0f);
        }

        private void FinishSurgery()
        {
            if (IsComplete)
            {
                return;
            }

            IsComplete = true;
            IsRunning = false;

            Objectives.Stop();
            Complications.Stop();
            Scoring.EndSurgery();

            SurgeryReport report = Scoring.BuildReport(
                Procedure.id,
                Procedure.displayName,
                Patient,
                Objectives.Steps.Count,
                Procedure.parTimeSeconds,
                Difficulty);

            report.complicationsTriggered = Complications.TotalTriggered;
            report.complicationsResolved = Complications.TotalResolved;

            if (AudioManager.Exists)
            {
                AudioManager.Instance.DetachMonitor();
                AudioManager.Instance.StopLoop(SoundId.Ambience);
            }

            GameEvents.RaiseSurgeryCompleted(report);
        }

        /// <summary>Cleans up all case-scoped objects.</summary>
        public void Teardown()
        {
            IsRunning = false;
            SurgeryServices.Clear();

            foreach (StaffAIController member in _team)
            {
                if (member != null)
                {
                    Destroy(member.gameObject);
                }
            }

            _team.Clear();

            if (Patient != null)
            {
                Destroy(Patient.gameObject);
            }

            Destroy(gameObject);
        }

        /// <summary>Space bar assistance: the consultant nudges you toward the next step.</summary>
        public void RequestAssistance()
        {
            if (Commands == null)
            {
                return;
            }

            if (Challenge != null && Challenge.noAssistance)
            {
                GameEvents.RaiseNotification("This challenge forbids asking for help.", NotificationType.Warning);
                return;
            }

            ObjectiveState current = Objectives != null ? Objectives.Current : null;
            if (current == null)
            {
                GameEvents.RaiseStaffSpeech(StaffRole.ConsultantSurgeon, "Nothing left to do - close him up.");
                return;
            }

            int remaining = Commands.AssistanceRemaining;
            if (remaining == 0)
            {
                GameEvents.RaiseStaffSpeech(StaffRole.ConsultantSurgeon, "I have already helped all I can.");
                return;
            }

            string hint = string.IsNullOrEmpty(current.Data.hint) ? current.Data.description : current.Data.hint;
            GameEvents.RaiseStaffSpeech(StaffRole.ConsultantSurgeon, hint);

            if (Patient.Body != null && !string.IsNullOrEmpty(current.Data.targetPart))
            {
                AnatomyPart part = Patient.Body.Get(current.Data.targetPart);
                if (part != null)
                {
                    StartCoroutine(FlashHighlight(part));
                }
            }
        }

        private IEnumerator FlashHighlight(AnatomyPart part)
        {
            for (int i = 0; i < 6; i++)
            {
                part.SetHighlight(i % 2 == 0);
                yield return new WaitForSeconds(0.25f);
            }

            part.SetHighlight(Profile.HighlightAnatomy);
        }
    }
}
