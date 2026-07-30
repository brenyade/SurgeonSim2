using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Save;
using UnityEngine;

namespace TraumaSurgeon.Career
{
    /// <summary>One case sitting on the surgical schedule.</summary>
    public class ScheduledCase
    {
        public ProcedureData Procedure;
        public DifficultyLevel Difficulty;
        public string PatientName;
        public bool HighRisk;
        public float PayoutMultiplier = 1f;
        public string Note;
    }

    /// <summary>
    /// The between-operations layer: upgrades, equipment, staff, research and the surgical
    /// schedule. Deliberately lightweight - it feeds bonuses into surgery rather than being a
    /// game of its own.
    /// </summary>
    public class HospitalManager : MonoSingleton<HospitalManager>
    {
        private readonly List<ScheduledCase> _schedule = new List<ScheduledCase>();

        public IReadOnlyList<ScheduledCase> Schedule => _schedule;

        private CareerData Data => CareerManager.Exists ? CareerManager.Instance.Data : null;

        // ---- Upgrades ---------------------------------------------------------

        public bool IsPurchased(string upgradeId)
        {
            CareerData data = Data;
            return data != null && data.purchasedUpgrades.Contains(upgradeId);
        }

        public bool CanAfford(UpgradeData upgrade)
        {
            CareerData data = Data;
            return data != null && upgrade != null && data.money >= upgrade.cost &&
                   data.rank >= upgrade.requiredLevel;
        }

        public bool Purchase(string upgradeId)
        {
            UpgradeData upgrade = DataLibrary.GetUpgrade(upgradeId);
            CareerData data = Data;
            if (upgrade == null || data == null || IsPurchased(upgradeId) || !CanAfford(upgrade))
            {
                return false;
            }

            data.money -= upgrade.cost;
            data.purchasedUpgrades.Add(upgradeId);
            GameEvents.RaiseNotification($"Purchased: {upgrade.displayName}", NotificationType.Success);
            CareerManager.Instance.Save();
            return true;
        }

        /// <summary>Summed effect value of every purchased upgrade with the given key.</summary>
        public float EffectValue(string effectKey)
        {
            CareerData data = Data;
            if (data == null)
            {
                return 0f;
            }

            float total = 0f;
            foreach (string id in data.purchasedUpgrades)
            {
                UpgradeData upgrade = DataLibrary.GetUpgrade(id);
                if (upgrade != null && upgrade.effectKey == effectKey)
                {
                    total += upgrade.effectValue;
                }
            }

            return total;
        }

        /// <summary>Equipment reliability bonus applied to the complication system.</summary>
        public float EquipmentReliability => Mathf.Clamp01(0.9f + EffectValue("equipment_reliability"));

        /// <summary>Extra units of blood available in theatre.</summary>
        public int ExtraBloodUnits => Mathf.RoundToInt(EffectValue("blood_supply"));

        /// <summary>Research bonus applied to experience earned.</summary>
        public float ResearchExperienceBonus => EffectValue("research_xp");

        /// <summary>Reduces how fast the patient deteriorates (better monitoring / equipment).</summary>
        public float DeteriorationReduction => Mathf.Clamp(EffectValue("deterioration"), 0f, 0.4f);

        public List<UpgradeData> UpgradesByCategory(string category)
        {
            var list = new List<UpgradeData>();
            foreach (UpgradeData upgrade in DataLibrary.Upgrades)
            {
                if (upgrade.category == category)
                {
                    list.Add(upgrade);
                }
            }

            return list;
        }

        // ---- Schedule ---------------------------------------------------------

        /// <summary>Generates the next set of offered cases based on rank.</summary>
        public void GenerateSchedule(int count = 4)
        {
            _schedule.Clear();
            if (!CareerManager.Exists)
            {
                return;
            }

            List<ProcedureData> unlocked = CareerManager.Instance.UnlockedProcedures();
            if (unlocked.Count == 0)
            {
                return;
            }

            string[] firstNames = { "Marcus", "Ana", "Priya", "Tomas", "Dorothy", "Kwame", "Lena", "Hiroshi" };
            string[] lastNames = { "Delgado", "Whitfield", "Nakamura", "Osei", "Brennan", "Kaur", "Novak", "Ferreira" };

            for (int i = 0; i < count; i++)
            {
                ProcedureData procedure = unlocked[Random.Range(0, unlocked.Count)];
                bool highRisk = Random.value < 0.3f;

                var entry = new ScheduledCase
                {
                    Procedure = procedure,
                    Difficulty = PickDifficulty(highRisk),
                    PatientName = $"{firstNames[Random.Range(0, firstNames.Length)]} " +
                                  $"{lastNames[Random.Range(0, lastNames.Length)]}",
                    HighRisk = highRisk,
                    PayoutMultiplier = highRisk ? Random.Range(1.5f, 2.2f) : Random.Range(0.9f, 1.2f),
                    Note = highRisk
                        ? "High risk: unstable on arrival, expect complications."
                        : "Routine referral, patient is stable."
                };

                _schedule.Add(entry);
            }
        }

        private DifficultyLevel PickDifficulty(bool highRisk)
        {
            int rank = Data != null ? Data.rank : 0;
            if (highRisk)
            {
                return rank >= 4 ? DifficultyLevel.ImpossibleShift : DifficultyLevel.Attending;
            }

            if (rank <= 1)
            {
                return DifficultyLevel.Student;
            }

            return rank <= 3 ? DifficultyLevel.Resident : DifficultyLevel.Attending;
        }

        /// <summary>Declining a case costs a little reputation but avoids the risk.</summary>
        public void RejectCase(ScheduledCase entry)
        {
            if (entry == null || Data == null)
            {
                return;
            }

            _schedule.Remove(entry);
            Data.reputation = Mathf.Clamp(Data.reputation - (entry.HighRisk ? 1f : 3f), 0f, 100f);
            GameEvents.RaiseNotification("Case declined and reassigned.", NotificationType.Info);
            CareerManager.Instance.Save();
        }

        public void RemoveCase(ScheduledCase entry)
        {
            _schedule.Remove(entry);
        }

        // ---- Research ---------------------------------------------------------

        public List<UpgradeData> ResearchProjects => UpgradesByCategory("Research");

        public bool ResearchComplete(string id) => IsPurchased(id);
    }
}
