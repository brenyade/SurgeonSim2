namespace TraumaSurgeon.Core
{
    /// <summary>
    /// Tuning bundle for a difficulty level. Pure data + a static table, so any system can ask
    /// <c>DifficultyProfile.Get(level)</c> without a scene dependency.
    /// </summary>
    public class DifficultyProfile
    {
        public DifficultyLevel Level;
        public string DisplayName;
        public string Description;

        /// <summary>Multiplier on how fast vitals deteriorate (bleeding, hypoxia, etc).</summary>
        public float DeteriorationRate = 1f;

        /// <summary>Multiplier on the chance of random/triggered complications firing.</summary>
        public float ComplicationRate = 1f;

        /// <summary>Guided incision paths and highlighted anatomy are shown.</summary>
        public bool HighlightAnatomy = true;

        /// <summary>Objective list shows explicit hints.</summary>
        public bool ShowHints = true;

        /// <summary>Number of times the player may press Space for assistance. -1 = unlimited.</summary>
        public int AssistanceUses = -1;

        /// <summary>Correct tool is auto-equipped when a step begins.</summary>
        public bool AutoToolSelect = false;

        /// <summary>Medication doses must be dialled in manually instead of being pre-set.</summary>
        public bool ManualMedicationDoses = false;

        /// <summary>Patient cannot die (training wheels).</summary>
        public bool PatientImmortal = false;

        /// <summary>Hand tremor multiplier.</summary>
        public float TremorScale = 0.5f;

        /// <summary>Final score multiplier.</summary>
        public float ScoreMultiplier = 0.8f;

        /// <summary>Multiplier on career money/reputation rewards.</summary>
        public float RewardMultiplier = 0.6f;

        private static readonly DifficultyProfile[] Profiles =
        {
            new DifficultyProfile
            {
                Level = DifficultyLevel.Student,
                DisplayName = "Student",
                Description = "Guided incisions, highlighted anatomy, unlimited assistance, slow deterioration.",
                DeteriorationRate = 0.45f,
                ComplicationRate = 0.25f,
                HighlightAnatomy = true,
                ShowHints = true,
                AssistanceUses = -1,
                AutoToolSelect = true,
                ManualMedicationDoses = false,
                PatientImmortal = true,
                TremorScale = 0.25f,
                ScoreMultiplier = 0.7f,
                RewardMultiplier = 0.5f
            },
            new DifficultyProfile
            {
                Level = DifficultyLevel.Resident,
                DisplayName = "Resident",
                Description = "Limited guidance, normal deterioration, moderate complications.",
                DeteriorationRate = 1f,
                ComplicationRate = 0.7f,
                HighlightAnatomy = true,
                ShowHints = true,
                AssistanceUses = 6,
                AutoToolSelect = false,
                ManualMedicationDoses = false,
                PatientImmortal = false,
                TremorScale = 0.5f,
                ScoreMultiplier = 1f,
                RewardMultiplier = 1f
            },
            new DifficultyProfile
            {
                Level = DifficultyLevel.Attending,
                DisplayName = "Attending",
                Description = "No highlights, faster deterioration, realistic complications, manual dosing.",
                DeteriorationRate = 1.5f,
                ComplicationRate = 1.1f,
                HighlightAnatomy = false,
                ShowHints = false,
                AssistanceUses = 3,
                AutoToolSelect = false,
                ManualMedicationDoses = true,
                PatientImmortal = false,
                TremorScale = 0.85f,
                ScoreMultiplier = 1.35f,
                RewardMultiplier = 1.5f
            },
            new DifficultyProfile
            {
                Level = DifficultyLevel.ImpossibleShift,
                DisplayName = "Impossible Shift",
                Description = "Simultaneous injuries, equipment failures, limited blood, minimal help.",
                DeteriorationRate = 2.2f,
                ComplicationRate = 1.8f,
                HighlightAnatomy = false,
                ShowHints = false,
                AssistanceUses = 1,
                AutoToolSelect = false,
                ManualMedicationDoses = true,
                PatientImmortal = false,
                TremorScale = 1.2f,
                ScoreMultiplier = 1.8f,
                RewardMultiplier = 2.2f
            }
        };

        public static DifficultyProfile Get(DifficultyLevel level)
        {
            int index = (int)level;
            if (index < 0 || index >= Profiles.Length)
            {
                return Profiles[1];
            }

            return Profiles[index];
        }

        public static DifficultyProfile[] All => Profiles;
    }
}
