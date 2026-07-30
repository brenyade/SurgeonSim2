using System;
using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Save;
using TraumaSurgeon.Scoring;
using UnityEngine;

namespace TraumaSurgeon.Career
{
    /// <summary>
    /// Career progression: experience, rank, money, reputation, malpractice risk, unlocks and the
    /// case history. Reads and writes the <see cref="CareerData"/> block of the active save slot.
    /// </summary>
    public class CareerManager : MonoSingleton<CareerManager>
    {
        /// <summary>Experience required to reach each <see cref="CareerRank"/>.</summary>
        public static readonly int[] RankThresholds = { 0, 800, 2200, 4500, 8000, 13000, 20000 };

        public static readonly string[] RankTitles =
        {
            "Medical Student", "Junior Resident", "Senior Resident", "Surgical Fellow",
            "Attending Surgeon", "Trauma Surgery Director", "Chief of Surgery"
        };

        public CareerData Data
        {
            get
            {
                SaveData save = SaveManager.Exists ? SaveManager.Instance.EnsureCareer() : null;
                return save != null ? save.career : _fallback;
            }
        }

        private readonly CareerData _fallback = new CareerData();

        public CareerRank Rank => (CareerRank)Mathf.Clamp(Data.rank, 0, RankThresholds.Length - 1);

        public string RankTitle => RankTitles[Mathf.Clamp(Data.rank, 0, RankTitles.Length - 1)];

        public int ExperienceToNextRank
        {
            get
            {
                int next = Data.rank + 1;
                if (next >= RankThresholds.Length)
                {
                    return 0;
                }

                return Mathf.Max(0, RankThresholds[next] - Data.experience);
            }
        }

        public float RankProgress
        {
            get
            {
                int current = RankThresholds[Mathf.Clamp(Data.rank, 0, RankThresholds.Length - 1)];
                int next = Data.rank + 1 < RankThresholds.Length ? RankThresholds[Data.rank + 1] : current + 1;
                return Mathf.Clamp01((float)(Data.experience - current) / Mathf.Max(1, next - current));
            }
        }

        protected override void OnSingletonAwake()
        {
            EnsureDefaultStaff();
        }

        /// <summary>Populates the starting team the first time a career is played.</summary>
        public void EnsureDefaultStaff()
        {
            CareerData data = Data;
            if (data.staff.Count > 0)
            {
                return;
            }

            data.staff.Add(new StaffRecord { role = StaffRole.Anesthesiologist.ToString(), name = "Dr. Okafor", skill = 4, salary = 2400f });
            data.staff.Add(new StaffRecord { role = StaffRole.ScrubNurse.ToString(), name = "Nurse Bell", skill = 4, salary = 1400f });
            data.staff.Add(new StaffRecord { role = StaffRole.CirculatingNurse.ToString(), name = "Nurse Ramos", skill = 3, salary = 1300f });
            data.staff.Add(new StaffRecord { role = StaffRole.SurgicalAssistant.ToString(), name = "Dr. Lindqvist", skill = 3, salary = 1900f });
            data.staff.Add(new StaffRecord { role = StaffRole.ConsultantSurgeon.ToString(), name = "Dr. Marchetti", skill = 6, salary = 3200f });
        }

        // ---- Unlocks ----------------------------------------------------------

        public bool IsProcedureUnlocked(ProcedureData procedure)
        {
            if (procedure == null)
            {
                return false;
            }

            if (Data.unlockedProcedures.Contains(procedure.id))
            {
                return true;
            }

            return Data.rank >= procedure.unlockLevel;
        }

        public List<ProcedureData> UnlockedProcedures()
        {
            var list = new List<ProcedureData>();
            foreach (ProcedureData procedure in DataLibrary.Procedures)
            {
                if (IsProcedureUnlocked(procedure))
                {
                    list.Add(procedure);
                }
            }

            return list;
        }

        public bool IsToolUnlocked(ToolType type)
        {
            ToolData data = DataLibrary.GetTool(type);
            if (data == null)
            {
                return true;
            }

            return Data.rank >= data.unlockLevel || Data.unlockedTools.Contains(type.ToString());
        }

        public bool IsTrainingComplete(string stationId) => Data.completedTraining.Contains(stationId);

        public void MarkTrainingComplete(string stationId)
        {
            if (!Data.completedTraining.Contains(stationId))
            {
                Data.completedTraining.Add(stationId);
                AddExperience(120);
                GameEvents.RaiseNotification("Training module completed.", NotificationType.Success);
                Save();
            }
        }

        public void MarkChallengeComplete(string challengeId, int experience, float money)
        {
            if (Data.completedChallenges.Contains(challengeId))
            {
                return;
            }

            Data.completedChallenges.Add(challengeId);
            AddExperience(experience);
            Data.money += money;
            Save();
        }

        // ---- Progression ------------------------------------------------------

        public void AddExperience(int amount)
        {
            Data.experience += Mathf.Max(0, amount);
            CheckPromotion();
        }

        private void CheckPromotion()
        {
            CareerData data = Data;
            while (data.rank + 1 < RankThresholds.Length && data.experience >= RankThresholds[data.rank + 1])
            {
                data.rank++;
                GameEvents.RaiseNotification($"Promoted to {RankTitles[data.rank]}!", NotificationType.Success);
                UnlockForRank(data.rank);
            }
        }

        private void UnlockForRank(int rank)
        {
            foreach (ProcedureData procedure in DataLibrary.Procedures)
            {
                if (procedure.unlockLevel == rank && !Data.unlockedProcedures.Contains(procedure.id))
                {
                    Data.unlockedProcedures.Add(procedure.id);
                    GameEvents.RaiseNotification($"New procedure unlocked: {procedure.displayName}",
                        NotificationType.Success);
                }
            }

            foreach (ToolData tool in DataLibrary.Tools)
            {
                if (tool.unlockLevel == rank && !Data.unlockedTools.Contains(tool.id))
                {
                    Data.unlockedTools.Add(tool.id);
                }
            }
        }

        /// <summary>Applies the outcome of one operation to the career record.</summary>
        public void RecordSurgery(SurgeryReport report, ProcedureData procedure, DifficultyLevel difficulty)
        {
            CareerData data = Data;
            DifficultyProfile profile = DifficultyProfile.Get(difficulty);

            data.patientsTreated++;
            if (!report.patientSurvived)
            {
                data.patientsLost++;
            }

            float performance = report.Percentage;

            // Money and reputation scale with performance and difficulty.
            float payout = procedure != null ? procedure.payout : 3000f;
            float earned = payout * profile.RewardMultiplier * Mathf.Lerp(0.35f, 1.25f, performance);
            if (!report.patientSurvived)
            {
                earned *= 0.25f;
            }

            data.money += Mathf.Round(earned);

            float reputationDelta = (procedure != null ? procedure.reputation : 4f) *
                                    profile.RewardMultiplier * (performance - 0.55f) * 2f;
            if (!report.patientSurvived)
            {
                reputationDelta -= 12f;
            }

            data.reputation = Mathf.Clamp(data.reputation + reputationDelta, 0f, 100f);

            // Malpractice risk rises with errors and deaths, decays with clean cases.
            float malpractice = 0f;
            malpractice += report.retainedItems * 9f;
            malpractice += report.incorrectDoses * 3f;
            malpractice += report.wrongToolUses * 1.2f;
            malpractice += report.patientSurvived ? 0f : 14f;
            malpractice -= report.Rank == SurgicalRank.S || report.Rank == SurgicalRank.A ? 5f : 0f;
            data.malpracticeRisk = Mathf.Clamp(data.malpracticeRisk + malpractice, 0f, 100f);

            // Hospital rating tracks reputation and outcomes.
            float survivalRate = data.patientsTreated > 0
                ? 1f - (float)data.patientsLost / data.patientsTreated
                : 1f;
            data.hospitalRating = Mathf.Clamp(survivalRate * 3.2f + data.reputation / 100f * 1.8f, 0f, 5f);

            int experience = Mathf.RoundToInt(
                (procedure != null ? procedure.experience : 200) * profile.RewardMultiplier *
                Mathf.Lerp(0.4f, 1.4f, performance));
            AddExperience(experience);

            // Best score table.
            BestScoreEntry best = data.bestScores.Find(b => b.procedureId == report.procedureId);
            if (best == null)
            {
                data.bestScores.Add(new BestScoreEntry
                {
                    procedureId = report.procedureId, score = report.totalScore, rank = report.rank
                });
            }
            else if (report.totalScore > best.score)
            {
                best.score = report.totalScore;
                best.rank = report.rank;
            }

            // Case history (kept to the most recent 40 entries).
            data.caseHistory.Insert(0, new CaseRecord
            {
                procedureId = report.procedureId,
                patientName = report.patientName,
                rank = report.rank,
                score = report.totalScore,
                survived = report.patientSurvived,
                durationSeconds = report.durationSeconds,
                bloodLossMl = report.bloodLossMl,
                complications = report.complicationsTriggered,
                notes = report.summary,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });

            while (data.caseHistory.Count > 40)
            {
                data.caseHistory.RemoveAt(data.caseHistory.Count - 1);
            }

            GenerateIncidentReports(report);

            data.shiftNumber++;
            Save();
        }

        private void GenerateIncidentReports(SurgeryReport report)
        {
            CareerData data = Data;

            if (report.retainedItems > 0)
            {
                data.incidentReports.Insert(0, new IncidentReport
                {
                    title = "Retained foreign object",
                    body = $"{report.retainedItems} item(s) were left inside {report.patientName}. " +
                           "A never-event review has been opened.",
                    severity = "Severe"
                });
            }

            if (!report.patientSurvived)
            {
                data.incidentReports.Insert(0, new IncidentReport
                {
                    title = "Intraoperative mortality",
                    body = $"{report.patientName} died during {report.procedureName}. " +
                           "The case will be presented at morbidity and mortality.",
                    severity = "Severe"
                });
            }

            if (report.incorrectDoses > 1)
            {
                data.incidentReports.Insert(0, new IncidentReport
                {
                    title = "Medication administration error",
                    body = $"{report.incorrectDoses} doses were given outside the therapeutic window.",
                    severity = "Moderate"
                });
            }

            while (data.incidentReports.Count > 20)
            {
                data.incidentReports.RemoveAt(data.incidentReports.Count - 1);
            }
        }

        // ---- Staff ------------------------------------------------------------

        public int GetStaffSkill(StaffRole role)
        {
            StaffRecord record = Data.staff.Find(s => s.role == role.ToString());
            return record != null ? record.skill : 4;
        }

        public StaffRecord GetStaff(StaffRole role)
        {
            return Data.staff.Find(s => s.role == role.ToString());
        }

        public bool TrainStaff(StaffRole role, float cost)
        {
            StaffRecord record = GetStaff(role);
            if (record == null || Data.money < cost || record.skill >= 10)
            {
                return false;
            }

            Data.money -= cost;
            record.skill++;
            record.trainingLevel++;
            record.salary *= 1.08f;
            Save();
            return true;
        }

        public bool HireStaff(StaffRole role, string name, int skill, float signingCost, float salary)
        {
            if (Data.money < signingCost)
            {
                return false;
            }

            Data.money -= signingCost;
            StaffRecord record = GetStaff(role);
            if (record == null)
            {
                Data.staff.Add(new StaffRecord { role = role.ToString(), name = name, skill = skill, salary = salary });
            }
            else
            {
                record.name = name;
                record.skill = skill;
                record.salary = salary;
            }

            Save();
            return true;
        }

        // ---- Persistence ------------------------------------------------------

        public void Save()
        {
            if (SaveManager.Exists && SaveManager.Instance.CurrentSlot >= 0)
            {
                SaveManager.Instance.SaveCurrent();
            }
        }
    }
}
