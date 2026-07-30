using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Scoring;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>
    /// The patient on the table: owns the vitals, the anatomy and the physiological subsystems,
    /// and exposes the high level verbs the surgery systems call (arrest, CPR, defibrillate...).
    /// </summary>
    public class PatientController : MonoBehaviour
    {
        public VitalSigns Vitals { get; private set; } = new VitalSigns();
        public BodyAnatomy Body { get; private set; }
        public VitalSignsSystem VitalsSystem { get; private set; }
        public BloodLossSystem BloodLoss { get; private set; }
        public AnesthesiaSystem Anesthesia { get; private set; }
        public MedicationSystem Medications { get; private set; }
        public OrganDamageSystem OrganDamage { get; private set; }

        public PatientTemplateData Template { get; private set; }

        /// <summary>0..1 collapse of the lung from air in the pleural space.</summary>
        public float PneumothoraxSeverity { get; private set; }

        /// <summary>Sponges/instruments currently inside the patient.</summary>
        public int RetainedItemCount { get; set; }

        public bool IsPrepped { get; set; }
        public bool IsScrubbedIn { get; set; }
        public bool IsClosed { get; private set; }

        /// <summary>Seconds of chest compressions delivered while in arrest.</summary>
        public float CompressionSeconds { get; private set; }
        public int DefibrillationCount { get; private set; }
        public bool ImmortalMode { get; set; }

        private float _bleedingModifier = 1f;
        private float _difficultyScale = 1f;
        private bool _compressingThisFrame;
        private float _arrestTimer;
        private bool _resusDrugGiven;
        private bool _antiarrhythmicGiven;
        private bool _initialised;

        public bool IsCavityOpen
        {
            get
            {
                if (Body == null)
                {
                    return false;
                }

                foreach (AnatomyPart part in Body.AllParts)
                {
                    if (part.blocksAccess && part.isOpen && part.layer == AnatomyLayer.Muscle)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Creates the anatomy and subsystems. Call once when a case is set up.</summary>
        public void Initialise(PatientTemplateData template, DifficultyProfile difficulty)
        {
            Template = template ?? new PatientTemplateData();
            _difficultyScale = difficulty != null ? difficulty.DeteriorationRate : 1f;
            ImmortalMode = difficulty != null && difficulty.PatientImmortal;

            Vitals = new VitalSigns
            {
                heartRate = Template.startHeartRate,
                systolic = Template.startSystolic,
                diastolic = Template.startDiastolic,
                oxygenSaturation = Template.startSpO2,
                respiratoryRate = Template.startRespRate,
                temperature = Template.startTemperature,
                bloodVolumeMl = Template.startBloodVolumeMl,
                maxBloodVolumeMl = Template.startBloodVolumeMl,
                painLevel = Template.startPain,
                internalBleedRate = Template.internalBleedRate,
                externalBleedRate = Template.externalBleedRate,
                infectionRisk = Template.infectionRisk,
                consciousness = ConsciousnessLevel.Alert,
                rhythm = CardiacRhythm.NormalSinus,
                isAlive = true,
                isVentilated = false
            };

            Body = AnatomyBuilder.Build(transform, Vector3.zero);

            VitalsSystem = gameObject.AddComponent<VitalSignsSystem>();
            BloodLoss = gameObject.AddComponent<BloodLossSystem>();
            Anesthesia = gameObject.AddComponent<AnesthesiaSystem>();
            Medications = gameObject.AddComponent<MedicationSystem>();
            OrganDamage = gameObject.AddComponent<OrganDamageSystem>();

            VitalsSystem.Initialise(this);
            BloodLoss.Initialise(this);
            Anesthesia.Initialise(this);
            Medications.Initialise(this);
            OrganDamage.Initialise(this);

            _initialised = true;
        }

        private void Update()
        {
            if (!_initialised)
            {
                return;
            }

            float dt = Time.deltaTime;
            BloodLoss.Tick(dt * _bleedingModifier, _difficultyScale);
            Anesthesia.Tick(dt, _difficultyScale);
            OrganDamage.Tick(dt, _difficultyScale);
            VitalsSystem.Tick(dt, _difficultyScale);

            TickArrest(dt);
            CheckDeath();

            _compressingThisFrame = false;
        }

        // ---- Arrest / resuscitation ------------------------------------------

        private void TickArrest(float dt)
        {
            if (!Vitals.InCardiacArrest)
            {
                _arrestTimer = 0f;
                return;
            }

            _arrestTimer += dt;

            if (_compressingThisFrame)
            {
                // Compressions maintain some perfusion and buy time.
                CompressionSeconds += dt;
                Vitals.systolic = Mathf.Min(80f, Vitals.systolic + 34f * dt);
                Vitals.diastolic = Mathf.Min(40f, Vitals.diastolic + 18f * dt);
                Vitals.oxygenSaturation = Mathf.Min(88f, Vitals.oxygenSaturation + 5f * dt);
                _arrestTimer -= dt * 0.75f;
            }

            // Untreated arrest becomes unsurvivable.
            if (_arrestTimer > 55f && !ImmortalMode)
            {
                Die("Prolonged cardiac arrest");
            }
        }

        public void SetRhythm(CardiacRhythm rhythm)
        {
            if (Vitals.rhythm == rhythm)
            {
                return;
            }

            Vitals.rhythm = rhythm;
            GameEvents.RaiseRhythmChanged(rhythm);
        }

        public void TriggerCardiacArrest(string cause)
        {
            if (Vitals.InCardiacArrest || !Vitals.isAlive)
            {
                return;
            }

            SetRhythm(Random.value < 0.55f ? CardiacRhythm.VentricularFibrillation : CardiacRhythm.Asystole);
            _resusDrugGiven = false;
            _antiarrhythmicGiven = false;
            GameEvents.RaiseComplicationStarted(ComplicationType.CardiacArrest, $"Cardiac arrest - {cause}");
            GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist, "We've lost the pulse! Starting compressions!");
        }

        public void TriggerRespiratoryArrest(string cause)
        {
            Vitals.respiratoryRate = 0f;
            Vitals.isVentilated = false;
            GameEvents.RaiseComplicationStarted(ComplicationType.RespiratoryArrest, $"Respiratory arrest - {cause}");
        }

        /// <summary>Called every frame compressions are being delivered.</summary>
        public void DeliverCompression()
        {
            _compressingThisFrame = true;
        }

        public void RegisterResuscitationDrug() => _resusDrugGiven = true;

        public void RegisterAntiarrhythmic() => _antiarrhythmicGiven = true;

        /// <summary>
        /// Attempts defibrillation. Returns true when the rhythm converts.
        /// Shocking a non-shockable rhythm is a scored error.
        /// </summary>
        public bool Defibrillate(float energyJoules = 200f)
        {
            DefibrillationCount++;

            if (!Vitals.IsShockable)
            {
                GameEvents.RaiseScoreEvent(ScoreEventType.UnnecessaryAction, 1f,
                    "Defibrillated a non-shockable rhythm");
                GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist, "That rhythm isn't shockable!");
                Vitals.heartRate = Mathf.Max(0f, Vitals.heartRate - 10f);
                return false;
            }

            float chance = 0.42f;
            chance += CompressionSeconds > 12f ? 0.2f : 0f;
            chance += _antiarrhythmicGiven ? 0.2f : 0f;
            chance += _resusDrugGiven ? 0.1f : 0f;
            chance += Mathf.Clamp01(Vitals.BloodFraction - 0.6f) * 0.4f;
            chance += Mathf.Clamp01((energyJoules - 120f) / 200f) * 0.1f;

            if (Random.value < chance)
            {
                SetRhythm(CardiacRhythm.NormalSinus);
                Vitals.heartRate = 96f;
                Vitals.systolic = Mathf.Max(Vitals.systolic, 82f);
                Vitals.diastolic = Mathf.Max(Vitals.diastolic, 48f);
                GameEvents.RaiseComplicationResolved(ComplicationType.CardiacArrest);
                GameEvents.RaiseComplicationResolved(ComplicationType.VentricularFibrillation);
                GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist, "We have a rhythm! Sinus at 96.");
                return true;
            }

            GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist, "No change. Resume compressions.");
            return false;
        }

        // ---- Injury helpers ---------------------------------------------------

        public void AddPneumothorax(float amount)
        {
            PneumothoraxSeverity = Mathf.Clamp01(PneumothoraxSeverity + amount);
            if (PneumothoraxSeverity > 0.6f)
            {
                GameEvents.RaiseComplicationStarted(ComplicationType.TensionPneumothorax,
                    "Tension pneumothorax developing");
            }
        }

        /// <summary>Chest tube decompression.</summary>
        public void RelievePneumothorax(float amount)
        {
            bool wasSevere = PneumothoraxSeverity > 0.4f;
            PneumothoraxSeverity = Mathf.Clamp01(PneumothoraxSeverity - amount);
            if (wasSevere && PneumothoraxSeverity <= 0.05f)
            {
                GameEvents.RaiseComplicationResolved(ComplicationType.TensionPneumothorax);
                GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist, "Good rush of air - sats are climbing.");
            }
        }

        public void ApplyBleedingModifier(float multiplier)
        {
            _bleedingModifier = Mathf.Clamp(_bleedingModifier * multiplier, 0.2f, 4f);
        }

        public void MaybeTriggerAllergy()
        {
            if (Random.value < 0.5f)
            {
                GameEvents.RaiseComplicationStarted(ComplicationType.AllergicReaction,
                    "Anaphylaxis - blood pressure dropping");
                Vitals.systolic -= 35f;
                Vitals.diastolic -= 20f;
                Vitals.oxygenSaturation -= 10f;
            }
        }

        public void MarkClosed()
        {
            IsClosed = true;
        }

        /// <summary>Adds bleeding not tied to a specific visible bleeder.</summary>
        public void AddInternalBleeding(float mlPerSecond)
        {
            Vitals.internalBleedRate = Mathf.Max(0f, Vitals.internalBleedRate + mlPerSecond);
        }

        // ---- Death ------------------------------------------------------------

        private void CheckDeath()
        {
            if (!Vitals.isAlive || ImmortalMode)
            {
                return;
            }

            if (Vitals.bloodVolumeMl <= Vitals.maxBloodVolumeMl * 0.32f)
            {
                Die("Exsanguination");
            }
            else if (Vitals.oxygenSaturation < 42f)
            {
                Die("Profound hypoxia");
            }
        }

        public void Die(string cause)
        {
            if (!Vitals.isAlive)
            {
                return;
            }

            Vitals.isAlive = false;
            SetRhythm(CardiacRhythm.Asystole);
            Vitals.heartRate = 0f;
            Vitals.systolic = 0f;
            Vitals.diastolic = 0f;
            GameEvents.RaiseScoreEvent(ScoreEventType.PatientDeath, 1f, cause);
            GameEvents.RaiseNotification($"Patient death: {cause}", NotificationType.Critical);
            GameEvents.RaisePatientDied();
        }

        // ---- Chart helpers ----------------------------------------------------

        public List<string> BuildProblemList()
        {
            var list = new List<string>();
            if (Body == null)
            {
                return list;
            }

            foreach (AnatomyPart part in Body.AllParts)
            {
                if (part.state != DamageState.Healthy && part.state != DamageState.Repaired && !part.isRemoved)
                {
                    list.Add(part.StatusLine());
                }
            }

            return list;
        }
    }
}
