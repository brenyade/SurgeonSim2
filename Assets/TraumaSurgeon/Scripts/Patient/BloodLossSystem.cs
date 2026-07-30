using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>
    /// Aggregates every bleeding source into a volume loss per second and applies transfusions.
    /// Also tracks how much blood has pooled in the surgical field for the suction system.
    /// </summary>
    public class BloodLossSystem : MonoBehaviour
    {
        private PatientController _patient;

        /// <summary>Blood sitting in the surgical field, in millilitres. Suction removes it.</summary>
        public float PooledBloodMl { get; private set; }

        /// <summary>Total blood lost across the whole operation (for the report).</summary>
        public float TotalLostMl { get; private set; }

        /// <summary>Units of packed red cells transfused so far.</summary>
        public int UnitsTransfused { get; private set; }

        /// <summary>Blood bank supply for this case (units). Challenge modes lower this.</summary>
        public int AvailableUnits = 8;

        /// <summary>Current combined loss rate in ml/sec.</summary>
        public float CurrentLossRate { get; private set; }

        public void Initialise(PatientController patient)
        {
            _patient = patient;
        }

        /// <summary>Called by the patient tick.</summary>
        public void Tick(float dt, float difficultyScale)
        {
            if (_patient == null || !_patient.Vitals.isAlive)
            {
                return;
            }

            VitalSigns v = _patient.Vitals;
            float anatomical = _patient.Body != null ? _patient.Body.TotalBleedRate : 0f;
            float rate = anatomical + v.internalBleedRate + v.externalBleedRate;

            // Blood pressure modulates flow: a hypotensive patient bleeds more slowly.
            float pressureScale = Mathf.Clamp(v.MeanArterialPressure / 90f, 0.25f, 1.6f);
            rate *= pressureScale * difficultyScale;

            CurrentLossRate = rate;

            if (rate > 0f)
            {
                float lost = rate * dt;
                v.bloodVolumeMl = Mathf.Max(0f, v.bloodVolumeMl - lost);
                TotalLostMl += lost;
                PooledBloodMl += lost * 0.75f;
            }

            // Pooled blood slowly soaks into drapes/sponges.
            PooledBloodMl = Mathf.Max(0f, PooledBloodMl - 1.5f * dt);
        }

        /// <summary>Removes pooled blood. Returns how much was actually cleared.</summary>
        public float Suction(float amountMl)
        {
            float removed = Mathf.Min(PooledBloodMl, amountMl);
            PooledBloodMl -= removed;
            return removed;
        }

        /// <summary>Adds blood to the field (organ injury, dropped clamp).</summary>
        public void AddPooledBlood(float amountMl)
        {
            PooledBloodMl += amountMl;
        }

        /// <summary>Transfuse one unit (approx. 300 ml). Returns false when the bank is empty.</summary>
        public bool Transfuse(float volumeMl = 300f)
        {
            if (AvailableUnits <= 0)
            {
                GameEvents.RaiseNotification("Blood bank is empty.", NotificationType.Critical);
                return false;
            }

            AvailableUnits--;
            UnitsTransfused++;
            VitalSigns v = _patient.Vitals;
            v.bloodVolumeMl = Mathf.Min(v.maxBloodVolumeMl, v.bloodVolumeMl + volumeMl);
            v.temperature -= 0.06f;   // cold products cool the patient
            return true;
        }

        /// <summary>Crystalloid: restores volume but dilutes oxygen carrying capacity.</summary>
        public void InfuseFluid(float volumeMl)
        {
            VitalSigns v = _patient.Vitals;
            v.bloodVolumeMl = Mathf.Min(v.maxBloodVolumeMl, v.bloodVolumeMl + volumeMl * 0.6f);
            v.oxygenSaturation = Mathf.Max(60f, v.oxygenSaturation - 0.6f);
            v.temperature -= 0.05f;
        }

        /// <summary>Field visibility 0..1 - heavy pooling makes precise work harder.</summary>
        public float FieldClarity => Mathf.Clamp01(1f - PooledBloodMl / 450f);
    }
}
