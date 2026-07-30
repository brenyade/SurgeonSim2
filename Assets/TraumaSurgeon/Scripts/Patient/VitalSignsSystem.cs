using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>
    /// Drives the numbers on the monitor. Each tick it computes target values from the patient's
    /// physiological state (blood volume, pain, anaesthesia, organ function, rhythm) and eases the
    /// displayed vitals toward them so the monitor moves believably instead of snapping.
    /// </summary>
    public class VitalSignsSystem : MonoBehaviour
    {
        private PatientController _patient;
        private float _emitTimer;

        public void Initialise(PatientController patient)
        {
            _patient = patient;
        }

        public void Tick(float dt, float difficultyScale)
        {
            VitalSigns v = _patient.Vitals;
            if (!v.isAlive)
            {
                return;
            }

            float bloodFraction = v.BloodFraction;
            float lungFunction = _patient.Body != null ? _patient.Body.LungFunction : 1f;
            float heartFunction = _patient.Body != null ? _patient.Body.HeartFunction : 1f;
            float painFactor = v.painLevel / 100f;

            // ---- Cardiac rhythm gates everything ------------------------------
            if (v.rhythm == CardiacRhythm.Asystole)
            {
                v.heartRate = Mathf.MoveTowards(v.heartRate, 0f, 60f * dt);
                v.systolic = Mathf.MoveTowards(v.systolic, 0f, 45f * dt);
                v.diastolic = Mathf.MoveTowards(v.diastolic, 0f, 30f * dt);
                v.oxygenSaturation = Mathf.MoveTowards(v.oxygenSaturation, 40f, 6f * dt * difficultyScale);
                return;
            }

            if (v.rhythm == CardiacRhythm.VentricularFibrillation)
            {
                v.heartRate = 220f + Mathf.Sin(Time.time * 12f) * 20f;
                v.systolic = Mathf.MoveTowards(v.systolic, 15f, 40f * dt);
                v.diastolic = Mathf.MoveTowards(v.diastolic, 8f, 25f * dt);
                v.oxygenSaturation = Mathf.MoveTowards(v.oxygenSaturation, 45f, 5f * dt * difficultyScale);
                return;
            }

            if (v.rhythm == CardiacRhythm.PEA)
            {
                v.systolic = Mathf.MoveTowards(v.systolic, 20f, 30f * dt);
                v.diastolic = Mathf.MoveTowards(v.diastolic, 12f, 20f * dt);
                v.oxygenSaturation = Mathf.MoveTowards(v.oxygenSaturation, 50f, 4f * dt * difficultyScale);
                return;
            }

            // ---- Heart rate ---------------------------------------------------
            // Baseline, plus compensatory tachycardia for blood loss and pain.
            float hrTarget = 72f;
            hrTarget += (1f - bloodFraction) * 190f;              // haemorrhage drives tachycardia
            hrTarget += painFactor * 38f;
            hrTarget += (1f - lungFunction) * 22f;
            hrTarget -= Mathf.Clamp((v.anesthesiaDepth - 65f) / 35f, 0f, 1f) * 14f;
            hrTarget = Mathf.Clamp(hrTarget, 28f, 200f);
            v.heartRate = Mathf.MoveTowards(v.heartRate, hrTarget, 12f * dt * difficultyScale);

            // ---- Blood pressure -----------------------------------------------
            // MAP collapses once compensation fails (roughly below 70% volume).
            float perfusion = Mathf.Clamp01((bloodFraction - 0.42f) / 0.5f);
            float sysTarget = 55f + perfusion * 75f;
            sysTarget *= Mathf.Lerp(0.55f, 1f, heartFunction);
            sysTarget += painFactor * 12f;
            sysTarget -= Mathf.Clamp((v.anesthesiaDepth - 70f) / 30f, 0f, 1f) * 20f;
            sysTarget = Mathf.Clamp(sysTarget, 0f, 210f);

            float diaTarget = Mathf.Clamp(sysTarget * 0.64f, 0f, 140f);
            v.systolic = Mathf.MoveTowards(v.systolic, sysTarget, 9f * dt * difficultyScale);
            v.diastolic = Mathf.MoveTowards(v.diastolic, diaTarget, 7f * dt * difficultyScale);

            // ---- Oxygen saturation ---------------------------------------------
            float spo2Target = 99f;
            spo2Target -= (1f - lungFunction) * 45f;
            spo2Target -= Mathf.Clamp01(1f - bloodFraction / 0.7f) * 22f;
            if (v.respiratoryRate < 6f && !v.isVentilated)
            {
                spo2Target -= 35f;
            }

            if (_patient.PneumothoraxSeverity > 0f)
            {
                spo2Target -= _patient.PneumothoraxSeverity * 40f;
            }

            spo2Target = Mathf.Clamp(spo2Target, 20f, 100f);
            float spo2Speed = spo2Target < v.oxygenSaturation ? 3.2f * difficultyScale : 4.5f;
            v.oxygenSaturation = Mathf.MoveTowards(v.oxygenSaturation, spo2Target, spo2Speed * dt);

            // ---- Respiration ----------------------------------------------------
            if (v.isVentilated)
            {
                float rrTarget = 14f - Mathf.Clamp((v.anesthesiaDepth - 70f) / 30f, 0f, 1f) * 6f;
                v.respiratoryRate = Mathf.MoveTowards(v.respiratoryRate, rrTarget, 2f * dt);
            }
            else
            {
                float rrTarget = 16f + painFactor * 10f + (1f - bloodFraction) * 12f;
                rrTarget -= Mathf.Clamp((v.anesthesiaDepth - 55f) / 45f, 0f, 1f) * 16f;
                v.respiratoryRate = Mathf.MoveTowards(v.respiratoryRate, Mathf.Max(0f, rrTarget), 2.5f * dt);
            }

            // ---- Temperature ----------------------------------------------------
            // An open body cavity loses heat; infection pushes the other way.
            float tempTarget = 36.8f;
            if (_patient.IsCavityOpen)
            {
                tempTarget -= 1.6f;
            }

            tempTarget -= (1f - bloodFraction) * 1.2f;
            tempTarget += Mathf.Clamp01(v.infectionRisk / 100f) * 1.4f;
            v.temperature = Mathf.MoveTowards(v.temperature, tempTarget, 0.035f * dt * difficultyScale);

            // ---- Derived rhythm transitions --------------------------------------
            if (v.heartRate > 150f && v.rhythm == CardiacRhythm.NormalSinus)
            {
                _patient.SetRhythm(CardiacRhythm.Tachycardia);
            }
            else if (v.heartRate < 48f && v.rhythm == CardiacRhythm.NormalSinus)
            {
                _patient.SetRhythm(CardiacRhythm.Bradycardia);
            }
            else if (v.heartRate >= 52f && v.heartRate <= 140f &&
                     (v.rhythm == CardiacRhythm.Tachycardia || v.rhythm == CardiacRhythm.Bradycardia))
            {
                _patient.SetRhythm(CardiacRhythm.NormalSinus);
            }

            // ---- Emit for UI ------------------------------------------------------
            _emitTimer += dt;
            if (_emitTimer >= 0.2f)
            {
                _emitTimer = 0f;
                GameEvents.RaiseVitalsUpdated(v);
            }
        }
    }
}
