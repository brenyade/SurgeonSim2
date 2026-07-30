using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Scoring;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>A drug the player can draw up and push.</summary>
    public class MedicationDefinition
    {
        public string Id;
        public string DisplayName;
        public string Unit = "mg";
        public float MinCorrectDose;
        public float MaxCorrectDose;
        public float DefaultDose;
        public string Indication;
        public string OverdoseEffect;

        public bool IsCorrectDose(float dose) => dose >= MinCorrectDose && dose <= MaxCorrectDose;
    }

    /// <summary>
    /// Drug cabinet plus the physiological response model. Doses outside the therapeutic window
    /// are penalised and can create complications, which is the point of the Attending difficulty
    /// where dosing is manual.
    /// </summary>
    public class MedicationSystem : MonoBehaviour
    {
        private PatientController _patient;

        public readonly List<string> AdministeredLog = new List<string>();
        public int CorrectDoses { get; private set; }
        public int IncorrectDoses { get; private set; }

        public bool AntibioticsGiven { get; private set; }
        public bool TranexamicGiven { get; private set; }
        public bool HeparinGiven { get; private set; }

        private static readonly Dictionary<string, MedicationDefinition> Catalog =
            new Dictionary<string, MedicationDefinition>
            {
                ["epinephrine"] = new MedicationDefinition
                {
                    Id = "epinephrine", DisplayName = "Epinephrine", Unit = "mg",
                    MinCorrectDose = 0.8f, MaxCorrectDose = 1.2f, DefaultDose = 1f,
                    Indication = "Cardiac arrest / profound hypotension",
                    OverdoseEffect = "Dangerous tachycardia and arrhythmia"
                },
                ["atropine"] = new MedicationDefinition
                {
                    Id = "atropine", DisplayName = "Atropine", Unit = "mg",
                    MinCorrectDose = 0.4f, MaxCorrectDose = 1f, DefaultDose = 0.5f,
                    Indication = "Symptomatic bradycardia",
                    OverdoseEffect = "Tachycardia, hyperthermia"
                },
                ["amiodarone"] = new MedicationDefinition
                {
                    Id = "amiodarone", DisplayName = "Amiodarone", Unit = "mg",
                    MinCorrectDose = 250f, MaxCorrectDose = 300f, DefaultDose = 300f,
                    Indication = "Refractory VF / pulseless VT",
                    OverdoseEffect = "Hypotension and bradycardia"
                },
                ["norepinephrine"] = new MedicationDefinition
                {
                    Id = "norepinephrine", DisplayName = "Norepinephrine", Unit = "mcg",
                    MinCorrectDose = 4f, MaxCorrectDose = 12f, DefaultDose = 8f,
                    Indication = "Vasoplegic hypotension",
                    OverdoseEffect = "Severe vasoconstriction, ischaemia"
                },
                ["fentanyl"] = new MedicationDefinition
                {
                    Id = "fentanyl", DisplayName = "Fentanyl", Unit = "mcg",
                    MinCorrectDose = 50f, MaxCorrectDose = 100f, DefaultDose = 75f,
                    Indication = "Intraoperative analgesia",
                    OverdoseEffect = "Respiratory depression"
                },
                ["propofol"] = new MedicationDefinition
                {
                    Id = "propofol", DisplayName = "Propofol", Unit = "mg",
                    MinCorrectDose = 20f, MaxCorrectDose = 60f, DefaultDose = 40f,
                    Indication = "Deepen anaesthesia",
                    OverdoseEffect = "Apnoea and hypotension"
                },
                ["tranexamic"] = new MedicationDefinition
                {
                    Id = "tranexamic", DisplayName = "Tranexamic Acid", Unit = "mg",
                    MinCorrectDose = 900f, MaxCorrectDose = 1100f, DefaultDose = 1000f,
                    Indication = "Traumatic haemorrhage",
                    OverdoseEffect = "Thrombotic risk"
                },
                ["cefazolin"] = new MedicationDefinition
                {
                    Id = "cefazolin", DisplayName = "Cefazolin", Unit = "g",
                    MinCorrectDose = 1.8f, MaxCorrectDose = 2.2f, DefaultDose = 2f,
                    Indication = "Surgical prophylaxis",
                    OverdoseEffect = "Allergic reaction risk"
                },
                ["heparin"] = new MedicationDefinition
                {
                    Id = "heparin", DisplayName = "Heparin", Unit = "units/kg",
                    MinCorrectDose = 280f, MaxCorrectDose = 320f, DefaultDose = 300f,
                    Indication = "Anticoagulation before bypass",
                    OverdoseEffect = "Uncontrollable oozing"
                },
                ["epinephrine_allergy"] = new MedicationDefinition
                {
                    Id = "epinephrine_allergy", DisplayName = "Epinephrine (IM)", Unit = "mg",
                    MinCorrectDose = 0.3f, MaxCorrectDose = 0.5f, DefaultDose = 0.5f,
                    Indication = "Anaphylaxis",
                    OverdoseEffect = "Hypertensive crisis"
                }
            };

        public static IEnumerable<MedicationDefinition> All => Catalog.Values;

        public static MedicationDefinition Get(string id)
        {
            return Catalog.TryGetValue(id, out MedicationDefinition def) ? def : null;
        }

        public void Initialise(PatientController patient)
        {
            _patient = patient;
        }

        /// <summary>
        /// Pushes a drug. Returns true when the dose was inside the therapeutic window.
        /// Effects are applied either way - a wrong dose still does something, just the wrong thing.
        /// </summary>
        public bool Administer(string drugId, float dose)
        {
            MedicationDefinition def = Get(drugId);
            if (def == null)
            {
                return false;
            }

            bool correct = def.IsCorrectDose(dose);
            float ratio = def.DefaultDose > 0f ? dose / def.DefaultDose : 1f;

            if (correct)
            {
                CorrectDoses++;
                GameEvents.RaiseScoreEvent(ScoreEventType.MedicationCorrect, 1f,
                    $"{def.DisplayName} {dose:0.##}{def.Unit}");
            }
            else
            {
                IncorrectDoses++;
                GameEvents.RaiseScoreEvent(ScoreEventType.MedicationWrong, 1f,
                    $"{def.DisplayName} {dose:0.##}{def.Unit} outside window");
                GameEvents.RaiseNotification($"Incorrect dose: {def.DisplayName}. {def.OverdoseEffect}",
                    NotificationType.Warning);
            }

            AdministeredLog.Add($"{def.DisplayName} {dose:0.##} {def.Unit}{(correct ? "" : " (out of range)")}");
            ApplyEffect(def, dose, ratio, correct);
            return correct;
        }

        private void ApplyEffect(MedicationDefinition def, float dose, float ratio, bool correct)
        {
            VitalSigns v = _patient.Vitals;

            switch (def.Id)
            {
                case "epinephrine":
                case "epinephrine_allergy":
                    v.heartRate += 26f * ratio;
                    v.systolic += 24f * ratio;
                    v.diastolic += 12f * ratio;
                    if (v.rhythm == CardiacRhythm.Asystole || v.rhythm == CardiacRhythm.PEA)
                    {
                        _patient.RegisterResuscitationDrug();
                    }

                    if (ratio > 2f)
                    {
                        _patient.SetRhythm(CardiacRhythm.VentricularTachycardia);
                    }

                    break;

                case "atropine":
                    v.heartRate += 18f * ratio;
                    if (ratio > 2.5f)
                    {
                        v.temperature += 0.4f;
                    }

                    break;

                case "amiodarone":
                    if (v.rhythm == CardiacRhythm.VentricularFibrillation ||
                        v.rhythm == CardiacRhythm.VentricularTachycardia)
                    {
                        _patient.RegisterAntiarrhythmic();
                    }

                    v.systolic -= 8f * ratio;
                    break;

                case "norepinephrine":
                    v.systolic += 18f * ratio;
                    v.diastolic += 11f * ratio;
                    if (ratio > 2f)
                    {
                        v.heartRate += 12f;
                    }

                    break;

                case "fentanyl":
                    v.painLevel = Mathf.Max(0f, v.painLevel - 32f * ratio);
                    v.respiratoryRate = Mathf.Max(0f, v.respiratoryRate - 2.5f * ratio);
                    v.heartRate -= 6f * ratio;
                    break;

                case "propofol":
                    _patient.Anesthesia.AdjustTarget(14f * ratio);
                    v.systolic -= 7f * ratio;
                    if (ratio > 2f)
                    {
                        v.respiratoryRate = Mathf.Max(0f, v.respiratoryRate - 5f);
                    }

                    break;

                case "tranexamic":
                    TranexamicGiven = true;
                    _patient.ApplyBleedingModifier(0.72f);
                    break;

                case "cefazolin":
                    AntibioticsGiven = true;
                    v.infectionRisk = Mathf.Max(0f, v.infectionRisk - 22f);
                    if (!correct && ratio > 2f)
                    {
                        _patient.MaybeTriggerAllergy();
                    }

                    break;

                case "heparin":
                    HeparinGiven = true;
                    _patient.ApplyBleedingModifier(correct ? 1.25f : 1.9f);
                    break;
            }

            v.heartRate = Mathf.Clamp(v.heartRate, 0f, 220f);
            v.systolic = Mathf.Clamp(v.systolic, 0f, 240f);
            v.diastolic = Mathf.Clamp(v.diastolic, 0f, 160f);
        }
    }
}
