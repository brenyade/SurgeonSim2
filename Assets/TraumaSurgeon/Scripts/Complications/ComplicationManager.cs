using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Scoring;
using UnityEngine;

namespace TraumaSurgeon.Complications
{
    /// <summary>A complication currently in play.</summary>
    public class ComplicationInstance
    {
        public ComplicationData Data;
        public float ElapsedSeconds;
        public bool Resolved;
        public bool EscalatedToCritical;

        public float Urgency => Data.timeToCriticalSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(ElapsedSeconds / Data.timeToCriticalSeconds);
    }

    /// <summary>
    /// Decides when things go wrong. Complications are driven by game state - blood loss, time,
    /// anaesthetic depth, organ damage, equipment condition - with only a small random component,
    /// so a bad outcome is always traceable to something the player did or failed to do.
    /// </summary>
    public class ComplicationManager : MonoBehaviour
    {
        private PatientController _patient;
        private DifficultyProfile _difficulty;
        private readonly List<ComplicationInstance> _active = new List<ComplicationInstance>();
        private readonly HashSet<ComplicationType> _allowed = new HashSet<ComplicationType>();
        private readonly HashSet<ComplicationType> _fired = new HashSet<ComplicationType>();

        private float _checkTimer;
        private float _elapsed;
        private bool _running;

        public IReadOnlyList<ComplicationInstance> Active => _active;
        public int TotalTriggered { get; private set; }
        public int TotalResolved { get; private set; }

        /// <summary>Equipment reliability 0..1. Upgrades raise it, challenge modes lower it.</summary>
        public float EquipmentReliability = 1f;

        public void Initialise(PatientController patient, DifficultyProfile difficulty,
            IEnumerable<string> allowedIds)
        {
            _patient = patient;
            _difficulty = difficulty ?? DifficultyProfile.Get(DifficultyLevel.Resident);
            _allowed.Clear();
            _fired.Clear();
            _active.Clear();
            _elapsed = 0f;
            TotalTriggered = 0;
            TotalResolved = 0;

            if (allowedIds != null)
            {
                foreach (string id in allowedIds)
                {
                    if (System.Enum.TryParse(id, true, out ComplicationType type))
                    {
                        _allowed.Add(type);
                    }
                }
            }

            _running = true;
        }

        public void Stop()
        {
            _running = false;
        }

        private void OnEnable()
        {
            GameEvents.ComplicationStarted += OnExternalComplication;
            GameEvents.ComplicationResolved += OnResolved;
        }

        private void OnDisable()
        {
            GameEvents.ComplicationStarted -= OnExternalComplication;
            GameEvents.ComplicationResolved -= OnResolved;
        }

        private void Update()
        {
            if (!_running || _patient == null || !_patient.Vitals.isAlive)
            {
                return;
            }

            float dt = Time.deltaTime;
            _elapsed += dt;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ComplicationInstance instance = _active[i];
                instance.ElapsedSeconds += dt;
                ApplyOngoingEffect(instance, dt);

                if (!instance.EscalatedToCritical && instance.Urgency >= 1f)
                {
                    instance.EscalatedToCritical = true;
                    Escalate(instance);
                }
            }

            _checkTimer -= dt;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 3f;
                EvaluateTriggers();
            }
        }

        // ---- Trigger evaluation ------------------------------------------------

        private void EvaluateTriggers()
        {
            VitalSigns v = _patient.Vitals;
            float rate = _difficulty.ComplicationRate;

            foreach (ComplicationData data in DataLibrary.Complications)
            {
                ComplicationType type = data.Type;
                if (_fired.Contains(type))
                {
                    continue;
                }

                if (_allowed.Count > 0 && !_allowed.Contains(type))
                {
                    continue;
                }

                float chance = 0f;

                foreach (string tag in data.triggerTags)
                {
                    chance += EvaluateTag(tag, v);
                }

                if (chance <= 0f)
                {
                    continue;
                }

                // baseChancePerMinute is per minute; we check every 3 seconds.
                float perCheck = data.baseChancePerMinute / 100f / 20f;
                float total = perCheck * chance * rate;

                if (Random.value < total)
                {
                    Trigger(type, data.announcement);
                }
            }
        }

        /// <summary>
        /// Converts a trigger tag into a multiplier. Returning 0 means "conditions absent",
        /// which is what keeps complications tied to game state rather than a random timer.
        /// </summary>
        private float EvaluateTag(string tag, VitalSigns v)
        {
            switch (tag)
            {
                case "always":
                    return 0.5f;
                case "highBloodLoss":
                    return v.BloodFraction < 0.75f ? (1f - v.BloodFraction) * 3f : 0f;
                case "activeBleeding":
                    return _patient.Body != null && _patient.Body.UncontrolledBleederCount > 0
                        ? _patient.Body.UncontrolledBleederCount * 0.6f
                        : 0f;
                case "longSurgery":
                    return _elapsed > 420f ? (_elapsed - 420f) / 240f : 0f;
                case "lowOxygen":
                    return v.oxygenSaturation < 93f ? (93f - v.oxygenSaturation) / 12f : 0f;
                case "lowPressure":
                    return v.MeanArterialPressure < 65f ? (65f - v.MeanArterialPressure) / 20f : 0f;
                case "deepAnesthesia":
                    return v.anesthesiaDepth > 82f ? (v.anesthesiaDepth - 82f) / 10f : 0f;
                case "lightAnesthesia":
                    return v.anesthesiaDepth < 48f && _patient.Anesthesia.IsInduced
                        ? (48f - v.anesthesiaDepth) / 20f
                        : 0f;
                case "tachycardia":
                    return v.heartRate > 135f ? (v.heartRate - 135f) / 30f : 0f;
                case "organDamage":
                    return _patient.OrganDamage != null && _patient.OrganDamage.AccidentalDamageTotal > 20f
                        ? _patient.OrganDamage.AccidentalDamageTotal / 60f
                        : 0f;
                case "openCavity":
                    return _patient.IsCavityOpen ? 0.7f : 0f;
                case "faultyEquipment":
                    return EquipmentReliability < 1f ? (1f - EquipmentReliability) * 4f : 0f;
                case "hypothermia":
                    return v.temperature < 35f ? (35f - v.temperature) * 2f : 0f;
                case "retainedItem":
                    return _patient.RetainedItemCount > 0 ? _patient.RetainedItemCount * 1.5f : 0f;
                case "lungInjury":
                    return _patient.PneumothoraxSeverity > 0.2f ? _patient.PneumothoraxSeverity * 2.5f : 0f;
                default:
                    return 0f;
            }
        }

        // ---- Firing and resolution ---------------------------------------------

        public void Trigger(ComplicationType type, string message = null)
        {
            if (_fired.Contains(type))
            {
                return;
            }

            ComplicationData data = DataLibrary.GetComplication(type);
            if (data == null)
            {
                return;
            }

            _fired.Add(type);
            var instance = new ComplicationInstance { Data = data };
            _active.Add(instance);
            TotalTriggered++;

            ApplyOnsetEffect(instance);

            GameEvents.RaiseComplicationStarted(type,
                string.IsNullOrEmpty(message) ? data.announcement : message);

            if (AudioManager.Exists)
            {
                AudioManager.Instance.Play(SoundId.Warning, 0.6f);
            }
        }

        /// <summary>Called when something else in the game raises a complication directly.</summary>
        private void OnExternalComplication(ComplicationType type, string message)
        {
            if (_fired.Contains(type))
            {
                return;
            }

            ComplicationData data = DataLibrary.GetComplication(type);
            if (data == null)
            {
                return;
            }

            _fired.Add(type);
            _active.Add(new ComplicationInstance { Data = data });
            TotalTriggered++;
        }

        private void OnResolved(ComplicationType type)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Data.Type == type && !_active[i].Resolved)
                {
                    _active[i].Resolved = true;
                    _active.RemoveAt(i);
                    TotalResolved++;
                    _fired.Remove(type);
                    GameEvents.RaiseNotification($"Resolved: {type}", NotificationType.Success);
                }
            }
        }

        private void ApplyOnsetEffect(ComplicationInstance instance)
        {
            VitalSigns v = _patient.Vitals;

            switch (instance.Data.Type)
            {
                case ComplicationType.SuddenHemorrhage:
                    SpawnMajorBleeder();
                    break;
                case ComplicationType.CardiacArrest:
                    _patient.TriggerCardiacArrest("intraoperative arrest");
                    break;
                case ComplicationType.VentricularFibrillation:
                    _patient.SetRhythm(CardiacRhythm.VentricularFibrillation);
                    break;
                case ComplicationType.RespiratoryArrest:
                    _patient.TriggerRespiratoryArrest("intraoperative apnoea");
                    break;
                case ComplicationType.TensionPneumothorax:
                    _patient.AddPneumothorax(0.65f);
                    break;
                case ComplicationType.AllergicReaction:
                    v.systolic -= 30f;
                    v.oxygenSaturation -= 8f;
                    v.heartRate += 25f;
                    break;
                case ComplicationType.EquipmentMalfunction:
                    EquipmentReliability = Mathf.Min(EquipmentReliability, 0.55f);
                    break;
                case ComplicationType.AnesthesiaOverdose:
                    _patient.Anesthesia.AdjustTarget(28f);
                    break;
                case ComplicationType.BloodPressureCollapse:
                    v.systolic *= 0.6f;
                    v.diastolic *= 0.6f;
                    break;
                case ComplicationType.SurgicalFire:
                    ApplyFireDamage();
                    break;
                case ComplicationType.InfectionRisk:
                    v.infectionRisk = Mathf.Min(100f, v.infectionRisk + 25f);
                    break;
            }
        }

        private void ApplyOngoingEffect(ComplicationInstance instance, float dt)
        {
            VitalSigns v = _patient.Vitals;

            switch (instance.Data.Type)
            {
                case ComplicationType.SuddenHemorrhage:
                    _patient.AddInternalBleeding(0.6f * dt);
                    break;
                case ComplicationType.TensionPneumothorax:
                    _patient.AddPneumothorax(0.02f * dt);
                    break;
                case ComplicationType.AllergicReaction:
                    v.systolic -= 1.5f * dt;
                    v.oxygenSaturation -= 0.5f * dt;
                    break;
                case ComplicationType.InfectionRisk:
                    v.infectionRisk = Mathf.Min(100f, v.infectionRisk + 0.6f * dt);
                    break;
            }
        }

        private void Escalate(ComplicationInstance instance)
        {
            GameEvents.RaiseNotification($"{instance.Data.displayName} is now critical!",
                NotificationType.Critical);
            GameEvents.RaiseScoreEvent(ScoreEventType.ComplicationIgnored, 1f, instance.Data.displayName);

            switch (instance.Data.Type)
            {
                case ComplicationType.SuddenHemorrhage:
                case ComplicationType.BloodPressureCollapse:
                    _patient.TriggerCardiacArrest("uncontrolled haemorrhage");
                    break;
                case ComplicationType.TensionPneumothorax:
                    _patient.AddPneumothorax(0.4f);
                    _patient.TriggerCardiacArrest("tension pneumothorax");
                    break;
                case ComplicationType.RespiratoryArrest:
                    _patient.Vitals.oxygenSaturation -= 20f;
                    break;
                case ComplicationType.AllergicReaction:
                    _patient.TriggerCardiacArrest("anaphylactic shock");
                    break;
            }
        }

        private void SpawnMajorBleeder()
        {
            if (_patient.Body == null)
            {
                return;
            }

            // Pick an exposed structure and open a brisk bleeder on it.
            var candidates = new List<AnatomyPart>();
            foreach (AnatomyPart part in _patient.Body.AllParts)
            {
                if (!part.isRemoved && (part.layer == AnatomyLayer.Organ || part.layer == AnatomyLayer.Vessel))
                {
                    candidates.Add(part);
                }
            }

            if (candidates.Count == 0)
            {
                _patient.AddInternalBleeding(3f);
                return;
            }

            AnatomyPart target = candidates[Random.Range(0, candidates.Count)];
            target.SpawnBleeder(Random.insideUnitSphere * 0.03f, Random.Range(3f, 7f),
                target.layer == AnatomyLayer.Vessel);
        }

        private void ApplyFireDamage()
        {
            if (_patient.Body == null)
            {
                return;
            }

            foreach (AnatomyPart part in _patient.Body.AllParts)
            {
                if (part.layer == AnatomyLayer.Skin && part.isOpen)
                {
                    _patient.OrganDamage.ReportInjury(part, 12f, "surgical fire burn", true);
                    break;
                }
            }
        }

        /// <summary>
        /// Called by the surgery manager when the player performs an action, so complications can
        /// be cleared by the correct treatment.
        /// </summary>
        public void NotifyAction(SurgicalActionType action, ToolType tool)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ComplicationData data = _active[i].Data;
                bool actionMatches = data.ResolveAction == SurgicalActionType.None || data.ResolveAction == action;
                bool toolMatches = data.ResolveTool == ToolType.None || data.ResolveTool == tool;

                if (actionMatches && toolMatches && (data.ResolveAction != SurgicalActionType.None ||
                                                     data.ResolveTool != ToolType.None))
                {
                    GameEvents.RaiseComplicationResolved(data.Type);
                }
            }
        }

        /// <summary>Checks whether a specific complication is currently active.</summary>
        public bool IsActive(ComplicationType type)
        {
            foreach (ComplicationInstance instance in _active)
            {
                if (instance.Data.Type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
