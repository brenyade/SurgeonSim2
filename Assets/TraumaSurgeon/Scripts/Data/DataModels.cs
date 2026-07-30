using System;
using System.Collections.Generic;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Data
{
    /// <summary>
    /// Plain serializable data records loaded from JSON in <c>Resources/TraumaSurgeonData</c>.
    /// Enum-valued fields are stored as strings and parsed lazily so the JSON stays human editable.
    /// </summary>
    public static class EnumParse
    {
        public static T To<T>(string value, T fallback) where T : struct
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, true, out T parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }

    [Serializable]
    public class ToolData
    {
        public string id;                    // matches ToolType name
        public string displayName;
        public string category;              // Cutting / Grasping / Hemostasis / Closure / Orthopedic / Airway / Support
        public string description;
        public string primaryAction;         // SurgicalActionType name
        public string secondaryAction;       // SurgicalActionType name
        public string misuseConsequence;     // shown in the tool codex and used by the misuse system
        public string colorHex = "#B8C4CC";
        public float length = 0.22f;
        public float precisionRequirement = 0.5f;   // 0 = forgiving, 1 = demanding
        public float tissueDamageOnMisuse = 6f;
        public int hotkey = 0;               // 1..9, 0 = not on the quick bar
        public int unlockLevel = 0;          // CareerRank index required

        public ToolType Type => EnumParse.To(id, ToolType.None);
        public SurgicalActionType Primary => EnumParse.To(primaryAction, SurgicalActionType.None);
        public SurgicalActionType Secondary => EnumParse.To(secondaryAction, SurgicalActionType.None);

        public Color Color
        {
            get
            {
                if (ColorUtility.TryParseHtmlString(colorHex, out Color c))
                {
                    return c;
                }

                return new Color(0.72f, 0.77f, 0.80f);
            }
        }
    }

    [Serializable]
    public class ToolDatabase
    {
        public List<ToolData> tools = new List<ToolData>();
    }

    [Serializable]
    public class ProcedureStepData
    {
        public string id;
        public string title;
        public string description;
        public string action;                // SurgicalActionType name
        public string targetPart;            // AnatomyPart id, or empty for "anywhere"
        public string requiredTool;          // ToolType name, empty = any
        public int repetitions = 1;          // how many successful actions are needed
        public bool optional;
        public string hint;
        public float parScoreSeconds = 45f;

        public SurgicalActionType Action => EnumParse.To(action, SurgicalActionType.None);
        public ToolType RequiredTool => EnumParse.To(requiredTool, ToolType.None);
    }

    [Serializable]
    public class PatientTemplateData
    {
        public string name = "Unknown Patient";
        public int age = 40;
        public string sex = "Unspecified";
        public float weightKg = 78f;
        public string bloodType = "O+";
        public string allergies = "None known";
        public string history = "No significant history.";
        public string presentation = "Presented to the emergency department.";
        public string imagingCaption = "No imaging on file.";
        public string imagingType = "XRay";      // XRay / CT / Ultrasound
        public float startHeartRate = 88f;
        public float startSystolic = 122f;
        public float startDiastolic = 78f;
        public float startSpO2 = 97f;
        public float startRespRate = 16f;
        public float startTemperature = 36.8f;
        public float startBloodVolumeMl = 5000f;
        public float startPain = 30f;
        public float internalBleedRate;          // ml/sec at surgery start
        public float externalBleedRate;
        public float infectionRisk = 5f;
    }

    [Serializable]
    public class InjuryData
    {
        public string partId;
        public string damageState = "Lacerated";
        public float severity = 50f;
        public float bleedRate = 2f;
        public bool requiresRemoval;
        public bool requiresRepair = true;

        public DamageState State => EnumParse.To(damageState, DamageState.Lacerated);
    }

    [Serializable]
    public class ProcedureData
    {
        public string id;
        public string displayName;
        public string shortName;
        public string category = "General";
        public string bodyRegion = "Abdomen";     // Abdomen / Chest / Head / Limb
        public string description;
        public string tutorial;
        public int unlockLevel;                   // CareerRank index
        public int parTimeSeconds = 480;
        public float payout = 4000f;
        public float reputation = 5f;
        public int experience = 250;
        public string[] requiredTools = new string[0];
        public string[] possibleComplications = new string[0];
        public string[] failureConditions = new string[0];
        public PatientTemplateData patient = new PatientTemplateData();
        public InjuryData[] injuries = new InjuryData[0];
        public ProcedureStepData[] steps = new ProcedureStepData[0];
    }

    [Serializable]
    public class ProcedureDatabase
    {
        public List<ProcedureData> procedures = new List<ProcedureData>();
    }

    [Serializable]
    public class ComplicationData
    {
        public string id;                     // ComplicationType name
        public string displayName;
        public string announcement;
        public string resolutionHint;
        public string resolveAction;          // SurgicalActionType that clears it
        public string resolveTool;            // ToolType that clears it (optional)
        public float timeToCriticalSeconds = 60f;
        public float severity = 1f;
        public float baseChancePerMinute = 6f;
        public string[] triggerTags = new string[0];   // e.g. "highBloodLoss", "longSurgery"

        public ComplicationType Type => EnumParse.To(id, ComplicationType.SuddenHemorrhage);
        public SurgicalActionType ResolveAction => EnumParse.To(resolveAction, SurgicalActionType.None);
        public ToolType ResolveTool => EnumParse.To(resolveTool, ToolType.None);
    }

    [Serializable]
    public class ComplicationDatabase
    {
        public List<ComplicationData> complications = new List<ComplicationData>();
    }

    [Serializable]
    public class UpgradeData
    {
        public string id;
        public string displayName;
        public string category = "Equipment";   // Equipment / Staff / Research / Room
        public string description;
        public float cost = 5000f;
        public int requiredLevel;
        public string effect;                   // human readable
        public string effectKey;                // machine readable, see HospitalManager
        public float effectValue = 0.1f;
    }

    [Serializable]
    public class UpgradeDatabase
    {
        public List<UpgradeData> upgrades = new List<UpgradeData>();
    }

    [Serializable]
    public class TrainingStationData
    {
        public string id;
        public string displayName;
        public string description;
        public string action;                // SurgicalActionType practiced
        public string tool;                  // ToolType used
        public int targetRepetitions = 8;
        public float timeLimitSeconds = 120f;

        public SurgicalActionType Action => EnumParse.To(action, SurgicalActionType.None);
        public ToolType Tool => EnumParse.To(tool, ToolType.None);
    }

    [Serializable]
    public class TrainingDatabase
    {
        public List<TrainingStationData> stations = new List<TrainingStationData>();
    }

    [Serializable]
    public class ChallengeData
    {
        public string id;
        public string displayName;
        public string description;
        public string procedureId;
        public string difficulty = "Attending";
        public float timeLimitSeconds;         // 0 = none
        public bool powerFailure;
        public bool limitedBlood;
        public bool noAssistance;
        public bool faultyEquipment;
        public int rewardExperience = 400;
        public float rewardMoney = 3000f;

        public DifficultyLevel Difficulty => EnumParse.To(difficulty, DifficultyLevel.Attending);
    }

    [Serializable]
    public class ChallengeDatabase
    {
        public List<ChallengeData> challenges = new List<ChallengeData>();
    }
}
