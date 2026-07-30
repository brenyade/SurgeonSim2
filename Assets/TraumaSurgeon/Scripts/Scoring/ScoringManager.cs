using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Patient;
using UnityEngine;

namespace TraumaSurgeon.Scoring
{
    /// <summary>
    /// Listens to gameplay score events for the whole operation and turns them into a ranked
    /// <see cref="SurgeryReport"/>. Lives for the duration of one surgery.
    /// </summary>
    public class ScoringManager : MonoBehaviour
    {
        public float ElapsedSeconds { get; private set; }

        private readonly List<float> _accuracySamples = new List<float>();
        private readonly List<string> _notes = new List<string>();

        private int _stepsCompleted;
        private int _wrongTool;
        private int _unnecessary;
        private int _complicationsTriggered;
        private int _complicationsResolved;
        private float _tissueDamage;
        private int _correctDoses;
        private int _incorrectDoses;
        private bool _patientDied;
        private bool _running;

        public void BeginSurgery()
        {
            ElapsedSeconds = 0f;
            _accuracySamples.Clear();
            _notes.Clear();
            _stepsCompleted = 0;
            _wrongTool = 0;
            _unnecessary = 0;
            _complicationsTriggered = 0;
            _complicationsResolved = 0;
            _tissueDamage = 0f;
            _correctDoses = 0;
            _incorrectDoses = 0;
            _patientDied = false;
            _running = true;
        }

        public void EndSurgery()
        {
            _running = false;
        }

        private void OnEnable()
        {
            GameEvents.ScoreEvent += OnScoreEvent;
            GameEvents.ComplicationStarted += OnComplicationStarted;
            GameEvents.ComplicationResolved += OnComplicationResolved;
        }

        private void OnDisable()
        {
            GameEvents.ScoreEvent -= OnScoreEvent;
            GameEvents.ComplicationStarted -= OnComplicationStarted;
            GameEvents.ComplicationResolved -= OnComplicationResolved;
        }

        private void Update()
        {
            if (_running)
            {
                ElapsedSeconds += Time.deltaTime;
            }
        }

        private void OnComplicationStarted(ComplicationType type, string message)
        {
            _complicationsTriggered++;
            _notes.Add($"Complication: {message}");
        }

        private void OnComplicationResolved(ComplicationType type)
        {
            _complicationsResolved++;
        }

        private void OnScoreEvent(ScoreEventType type, float amount, string reason)
        {
            switch (type)
            {
                case ScoreEventType.StepCompleted:
                    _stepsCompleted++;
                    break;
                case ScoreEventType.PrecisionGood:
                case ScoreEventType.PrecisionPoor:
                    _accuracySamples.Add(Mathf.Clamp01(amount));
                    break;
                case ScoreEventType.WrongTool:
                    _wrongTool++;
                    _notes.Add($"Wrong instrument: {reason}");
                    break;
                case ScoreEventType.UnnecessaryAction:
                    _unnecessary++;
                    break;
                case ScoreEventType.TissueDamage:
                    _tissueDamage += amount;
                    break;
                case ScoreEventType.MedicationCorrect:
                    _correctDoses++;
                    break;
                case ScoreEventType.MedicationWrong:
                    _incorrectDoses++;
                    _notes.Add($"Medication error: {reason}");
                    break;
                case ScoreEventType.PatientDeath:
                    _patientDied = true;
                    _notes.Add($"Patient died: {reason}");
                    break;
                case ScoreEventType.RetainedItem:
                    _notes.Add($"Retained item: {reason}");
                    break;
            }
        }

        public float AverageAccuracy
        {
            get
            {
                if (_accuracySamples.Count == 0)
                {
                    return 1f;
                }

                float total = 0f;
                foreach (float f in _accuracySamples)
                {
                    total += f;
                }

                return total / _accuracySamples.Count;
            }
        }

        /// <summary>Builds the final report. Called once by SurgeryManager when the case ends.</summary>
        public SurgeryReport BuildReport(
            string procedureId,
            string procedureName,
            PatientController patient,
            int stepsTotal,
            float parTimeSeconds,
            DifficultyLevel difficulty)
        {
            DifficultyProfile profile = DifficultyProfile.Get(difficulty);

            var report = new SurgeryReport
            {
                procedureId = procedureId,
                procedureName = procedureName,
                patientName = patient != null && patient.Template != null ? patient.Template.name : "Unknown",
                difficulty = profile.DisplayName,
                patientSurvived = !_patientDied && (patient == null || patient.Vitals.isAlive),
                finalStability = patient != null ? patient.Vitals.Stability : 0f,
                bloodLossMl = patient != null ? patient.BloodLoss.TotalLostMl : 0f,
                durationSeconds = ElapsedSeconds,
                parTimeSeconds = parTimeSeconds,
                stepsCompleted = _stepsCompleted,
                stepsTotal = Mathf.Max(1, stepsTotal),
                accuracyAverage = AverageAccuracy,
                tissueDamage = _tissueDamage,
                wrongToolUses = _wrongTool,
                unnecessaryActions = _unnecessary,
                complicationsTriggered = _complicationsTriggered,
                complicationsResolved = _complicationsResolved,
                correctDoses = _correctDoses,
                incorrectDoses = _incorrectDoses,
                retainedItems = patient != null ? patient.RetainedItemCount : 0,
                infectionRisk = patient != null ? patient.Vitals.infectionRisk : 0f,
                unitsTransfused = patient != null ? patient.BloodLoss.UnitsTransfused : 0
            };

            report.notes.AddRange(_notes);
            ScoreReport(report, profile);
            return report;
        }

        /// <summary>Fills the breakdown lines, total and rank.</summary>
        private static void ScoreReport(SurgeryReport r, DifficultyProfile profile)
        {
            r.lines.Clear();

            // Survival (300)
            int survival = r.patientSurvived ? 300 : 0;
            r.lines.Add(new ScoreLine("Patient survival", survival, 300,
                r.patientSurvived ? "Patient left theatre alive" : "Patient died on the table"));

            // Final stability (120)
            int stability = Mathf.RoundToInt(Mathf.Clamp01(r.finalStability) * 120f);
            r.lines.Add(new ScoreLine("Final stability", stability, 120,
                $"{Mathf.RoundToInt(r.finalStability * 100f)}% stable"));

            // Procedure completion (150)
            float completion = (float)r.stepsCompleted / Mathf.Max(1, r.stepsTotal);
            int completionPoints = Mathf.RoundToInt(Mathf.Clamp01(completion) * 150f);
            r.lines.Add(new ScoreLine("Procedure completion", completionPoints, 150,
                $"{r.stepsCompleted}/{r.stepsTotal} steps"));

            // Blood loss (120) - 500 ml is excellent, 3000 ml is a disaster.
            float bloodScore = Mathf.Clamp01(1f - Mathf.Max(0f, r.bloodLossMl - 400f) / 2600f);
            int bloodPoints = Mathf.RoundToInt(bloodScore * 120f);
            r.lines.Add(new ScoreLine("Blood loss", bloodPoints, 120, $"{Mathf.RoundToInt(r.bloodLossMl)} ml"));

            // Surgical accuracy (110)
            int accuracyPoints = Mathf.RoundToInt(Mathf.Clamp01(r.accuracyAverage) * 110f);
            r.lines.Add(new ScoreLine("Surgical accuracy", accuracyPoints, 110,
                $"{Mathf.RoundToInt(r.accuracyAverage * 100f)}% on target"));

            // Tissue damage (90)
            float damageScore = Mathf.Clamp01(1f - r.tissueDamage / 220f);
            int damagePoints = Mathf.RoundToInt(damageScore * 90f);
            r.lines.Add(new ScoreLine("Tissue preservation", damagePoints, 90,
                $"{Mathf.RoundToInt(r.tissueDamage)} collateral damage"));

            // Instrument discipline (60)
            int toolPoints = Mathf.Max(0, 60 - r.wrongToolUses * 12);
            r.lines.Add(new ScoreLine("Correct tool use", toolPoints, 60, $"{r.wrongToolUses} wrong-instrument uses"));

            // Economy of motion (40)
            int economyPoints = Mathf.Max(0, 40 - r.unnecessaryActions * 4);
            r.lines.Add(new ScoreLine("Economy of motion", economyPoints, 40,
                $"{r.unnecessaryActions} unnecessary actions"));

            // Time (60)
            float timeRatio = r.parTimeSeconds <= 0f ? 1f : r.durationSeconds / r.parTimeSeconds;
            float timeScore = Mathf.Clamp01(1.4f - timeRatio);
            int timePoints = Mathf.RoundToInt(timeScore * 60f);
            r.lines.Add(new ScoreLine("Operative time", timePoints, 60,
                $"{r.DurationText} (par {Mathf.RoundToInt(r.parTimeSeconds / 60f)} min)"));

            // Infection prevention (50)
            int infectionPoints = Mathf.RoundToInt(Mathf.Clamp01(1f - r.infectionRisk / 100f) * 50f);
            r.lines.Add(new ScoreLine("Infection prevention", infectionPoints, 50,
                $"{Mathf.RoundToInt(r.infectionRisk)}% risk"));

            // Complication management (60)
            int compPoints;
            if (r.complicationsTriggered == 0)
            {
                compPoints = 60;
            }
            else
            {
                compPoints = Mathf.RoundToInt(
                    Mathf.Clamp01((float)r.complicationsResolved / r.complicationsTriggered) * 60f);
            }

            r.lines.Add(new ScoreLine("Complication management", compPoints, 60,
                $"{r.complicationsResolved}/{r.complicationsTriggered} resolved"));

            // Medication accuracy (40)
            int medTotal = r.correctDoses + r.incorrectDoses;
            int medPoints = medTotal == 0 ? 40 : Mathf.RoundToInt((float)r.correctDoses / medTotal * 40f);
            r.lines.Add(new ScoreLine("Medication accuracy", medPoints, 40,
                medTotal == 0 ? "No drugs required" : $"{r.correctDoses}/{medTotal} correct"));

            // Retained items (0 or heavy penalty)
            int retainedPenalty = r.retainedItems * -80;
            if (r.retainedItems > 0)
            {
                r.lines.Add(new ScoreLine("Retained items", retainedPenalty, 0,
                    $"{r.retainedItems} item(s) left inside the patient"));
            }
            else
            {
                r.lines.Add(new ScoreLine("Instrument count", 40, 40, "Count correct"));
            }

            int total = 0;
            int max = 0;
            foreach (ScoreLine line in r.lines)
            {
                total += line.points;
                max += line.maxPoints;
            }

            total = Mathf.RoundToInt(total * profile.ScoreMultiplier);
            max = Mathf.RoundToInt(max * profile.ScoreMultiplier);

            r.totalScore = Mathf.Max(0, total);
            r.maxScore = Mathf.Max(1, max);
            r.rank = DetermineRank(r).ToString();
            r.summary = BuildSummary(r);
        }

        private static SurgicalRank DetermineRank(SurgeryReport r)
        {
            if (!r.patientSurvived || r.retainedItems > 1)
            {
                return SurgicalRank.F;
            }

            float pct = r.Percentage;
            if (r.retainedItems > 0 || r.incorrectDoses > 2)
            {
                pct -= 0.12f;
            }

            if (pct >= 0.93f) return SurgicalRank.S;
            if (pct >= 0.84f) return SurgicalRank.A;
            if (pct >= 0.72f) return SurgicalRank.B;
            if (pct >= 0.58f) return SurgicalRank.C;
            return SurgicalRank.D;
        }

        private static string BuildSummary(SurgeryReport r)
        {
            if (!r.patientSurvived)
            {
                return "The patient did not survive the operation. Review the timeline below.";
            }

            switch (r.Rank)
            {
                case SurgicalRank.S:
                    return "Exceptional. Textbook technique, minimal blood loss, no avoidable harm.";
                case SurgicalRank.A:
                    return "Excellent work. Clean field, efficient closure, patient stable.";
                case SurgicalRank.B:
                    return "Successful operation with a few rough edges.";
                case SurgicalRank.C:
                    return "The patient will recover, but the recovery will be complicated.";
                default:
                    return "Major errors. This case will be discussed at morbidity and mortality.";
            }
        }
    }
}
