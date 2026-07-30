using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>
    /// Models anaesthetic depth. Too light and the patient hurts, moves and becomes tachycardic;
    /// too deep and respiration and blood pressure collapse.
    /// </summary>
    public class AnesthesiaSystem : MonoBehaviour
    {
        public const float TargetMin = 55f;
        public const float TargetMax = 75f;

        private PatientController _patient;

        /// <summary>Anaesthetist's dial, 0..100. The depth chases this value.</summary>
        public float TargetDepth { get; private set; }

        /// <summary>Seconds the patient spent outside the safe window (used in scoring).</summary>
        public float TimeTooLight { get; private set; }
        public float TimeTooDeep { get; private set; }

        public bool IsInduced { get; private set; }

        public void Initialise(PatientController patient)
        {
            _patient = patient;
        }

        /// <summary>Puts the patient under at the start of the operation.</summary>
        public void Induce()
        {
            IsInduced = true;
            TargetDepth = 65f;
            _patient.Vitals.consciousness = ConsciousnessLevel.Anesthetized;
            _patient.Vitals.isVentilated = true;   // intubated and on the ventilator
            GameEvents.RaiseStaffSpeech(StaffRole.Anesthesiologist, "Patient is induced. Airway secured.");
        }

        public void AdjustTarget(float delta)
        {
            TargetDepth = Mathf.Clamp(TargetDepth + delta, 0f, 100f);
        }

        public void SetTarget(float value)
        {
            TargetDepth = Mathf.Clamp(value, 0f, 100f);
        }

        public void Tick(float dt, float difficultyScale)
        {
            VitalSigns v = _patient.Vitals;
            if (!v.isAlive)
            {
                return;
            }

            // Depth chases the target; agents wash out when the vaporiser is turned down.
            float rate = TargetDepth > v.anesthesiaDepth ? 6f : 4f;
            v.anesthesiaDepth = Mathf.MoveTowards(v.anesthesiaDepth, TargetDepth, rate * dt);

            if (!IsInduced)
            {
                return;
            }

            if (v.anesthesiaDepth < TargetMin)
            {
                TimeTooLight += dt;
                float lightness = (TargetMin - v.anesthesiaDepth) / TargetMin;
                v.painLevel = Mathf.Min(100f, v.painLevel + lightness * 14f * dt * difficultyScale);
                v.heartRate += lightness * 9f * dt * difficultyScale;
                v.systolic += lightness * 6f * dt * difficultyScale;

                if (v.anesthesiaDepth < 25f)
                {
                    v.consciousness = ConsciousnessLevel.Drowsy;
                }
                else
                {
                    v.consciousness = ConsciousnessLevel.Sedated;
                }
            }
            else if (v.anesthesiaDepth > TargetMax)
            {
                TimeTooDeep += dt;
                float excess = (v.anesthesiaDepth - TargetMax) / (100f - TargetMax);
                v.respiratoryRate = Mathf.Max(0f, v.respiratoryRate - excess * 5f * dt * difficultyScale);
                v.systolic -= excess * 9f * dt * difficultyScale;
                v.diastolic -= excess * 6f * dt * difficultyScale;
                v.heartRate -= excess * 4f * dt * difficultyScale;
                v.consciousness = ConsciousnessLevel.Anesthetized;

                if (v.anesthesiaDepth > 92f && v.respiratoryRate < 4f && !v.isVentilated)
                {
                    _patient.TriggerRespiratoryArrest("Anaesthetic overdose");
                }
            }
            else
            {
                v.consciousness = ConsciousnessLevel.Anesthetized;
                v.painLevel = Mathf.Max(0f, v.painLevel - 6f * dt);
            }
        }

        public bool IsTooLight => IsInduced && _patient.Vitals.anesthesiaDepth < TargetMin - 3f;
        public bool IsTooDeep => IsInduced && _patient.Vitals.anesthesiaDepth > TargetMax + 3f;

        public string StatusText
        {
            get
            {
                if (!IsInduced)
                {
                    return "Awake";
                }

                if (IsTooLight)
                {
                    return "Too light";
                }

                if (IsTooDeep)
                {
                    return "Too deep";
                }

                return "Stable plane";
            }
        }
    }
}
