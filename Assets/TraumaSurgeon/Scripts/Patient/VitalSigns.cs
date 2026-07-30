using System;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>
    /// The complete physiological snapshot of a patient. Systems mutate it; the HUD reads it.
    /// </summary>
    [Serializable]
    public class VitalSigns
    {
        public float heartRate = 82f;               // bpm
        public float systolic = 120f;               // mmHg
        public float diastolic = 78f;               // mmHg
        public float oxygenSaturation = 98f;        // %
        public float respiratoryRate = 16f;         // breaths/min
        public float temperature = 36.8f;           // Celsius
        public float bloodVolumeMl = 5000f;
        public float maxBloodVolumeMl = 5000f;
        public float painLevel = 20f;               // 0..100
        public float anesthesiaDepth = 0f;          // 0..100 (target window 55-75)
        public float infectionRisk = 5f;            // 0..100
        public float internalBleedRate;             // ml/sec not attributable to a visible bleeder
        public float externalBleedRate;             // ml/sec from the surgical field
        public ConsciousnessLevel consciousness = ConsciousnessLevel.Alert;
        public CardiacRhythm rhythm = CardiacRhythm.NormalSinus;
        public bool isAlive = true;
        public bool isVentilated = true;

        /// <summary>Mean arterial pressure.</summary>
        public float MeanArterialPressure => diastolic + (systolic - diastolic) / 3f;

        /// <summary>Fraction of blood volume remaining, 0..1.</summary>
        public float BloodFraction => Mathf.Clamp01(bloodVolumeMl / Mathf.Max(1f, maxBloodVolumeMl));

        public float BloodLostMl => Mathf.Max(0f, maxBloodVolumeMl - bloodVolumeMl);

        /// <summary>Class I-IV haemorrhagic shock, 0 = none.</summary>
        public int ShockClass
        {
            get
            {
                float lost = 1f - BloodFraction;
                if (lost < 0.15f) return 0;
                if (lost < 0.30f) return 1;
                if (lost < 0.40f) return 2;
                return 3;
            }
        }

        public bool InCardiacArrest =>
            rhythm == CardiacRhythm.Asystole ||
            rhythm == CardiacRhythm.VentricularFibrillation ||
            rhythm == CardiacRhythm.PEA ||
            rhythm == CardiacRhythm.VentricularTachycardia;

        public bool IsShockable =>
            rhythm == CardiacRhythm.VentricularFibrillation ||
            rhythm == CardiacRhythm.VentricularTachycardia;

        /// <summary>0..1 overall stability used for scoring and staff chatter.</summary>
        public float Stability
        {
            get
            {
                if (!isAlive)
                {
                    return 0f;
                }

                float hrScore = 1f - Mathf.Clamp01(Mathf.Abs(heartRate - 78f) / 80f);
                float mapScore = 1f - Mathf.Clamp01(Mathf.Abs(MeanArterialPressure - 88f) / 55f);
                float spo2Score = Mathf.Clamp01((oxygenSaturation - 70f) / 28f);
                float bloodScore = Mathf.Clamp01((BloodFraction - 0.5f) / 0.5f);
                float rhythmScore = InCardiacArrest ? 0f : 1f;
                return Mathf.Clamp01(hrScore * 0.2f + mapScore * 0.25f + spo2Score * 0.25f +
                                     bloodScore * 0.2f + rhythmScore * 0.1f);
            }
        }

        public string BloodPressureText => $"{Mathf.RoundToInt(systolic)}/{Mathf.RoundToInt(diastolic)}";

        public VitalSigns Clone()
        {
            return (VitalSigns)MemberwiseClone();
        }
    }
}
