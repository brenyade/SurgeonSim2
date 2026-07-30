using System.Collections.Generic;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Data
{
    /// <summary>
    /// Loads and caches every JSON database from <c>Resources/TraumaSurgeonData</c>.
    /// Fails soft: a missing or malformed file logs a warning and yields an empty database
    /// so the game still boots.
    /// </summary>
    public static class DataLibrary
    {
        public const string ResourceFolder = "TraumaSurgeonData/";

        private static ToolDatabase _tools;
        private static ProcedureDatabase _procedures;
        private static ComplicationDatabase _complications;
        private static UpgradeDatabase _upgrades;
        private static TrainingDatabase _training;
        private static ChallengeDatabase _challenges;

        private static readonly Dictionary<ToolType, ToolData> ToolLookup = new Dictionary<ToolType, ToolData>();
        private static readonly Dictionary<string, ProcedureData> ProcedureLookup = new Dictionary<string, ProcedureData>();
        private static readonly Dictionary<ComplicationType, ComplicationData> ComplicationLookup =
            new Dictionary<ComplicationType, ComplicationData>();

        public static List<ToolData> Tools
        {
            get
            {
                EnsureLoaded();
                return _tools.tools;
            }
        }

        public static List<ProcedureData> Procedures
        {
            get
            {
                EnsureLoaded();
                return _procedures.procedures;
            }
        }

        public static List<ComplicationData> Complications
        {
            get
            {
                EnsureLoaded();
                return _complications.complications;
            }
        }

        public static List<UpgradeData> Upgrades
        {
            get
            {
                EnsureLoaded();
                return _upgrades.upgrades;
            }
        }

        public static List<TrainingStationData> TrainingStations
        {
            get
            {
                EnsureLoaded();
                return _training.stations;
            }
        }

        public static List<ChallengeData> Challenges
        {
            get
            {
                EnsureLoaded();
                return _challenges.challenges;
            }
        }

        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            _tools = Load<ToolDatabase>("tools");
            _procedures = Load<ProcedureDatabase>("procedures");
            _complications = Load<ComplicationDatabase>("complications");
            _upgrades = Load<UpgradeDatabase>("upgrades");
            _training = Load<TrainingDatabase>("training");
            _challenges = Load<ChallengeDatabase>("challenges");

            BuildLookups();
        }

        /// <summary>Force a reload (used by the editor tooling after editing JSON).</summary>
        public static void Reload()
        {
            _loaded = false;
            ToolLookup.Clear();
            ProcedureLookup.Clear();
            ComplicationLookup.Clear();
            EnsureLoaded();
        }

        private static T Load<T>(string fileName) where T : new()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourceFolder + fileName);
            if (asset == null)
            {
                Debug.LogWarning($"[DataLibrary] Missing data file: Resources/{ResourceFolder}{fileName}.json");
                return new T();
            }

            try
            {
                T parsed = JsonUtility.FromJson<T>(asset.text);
                return parsed != null ? parsed : new T();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataLibrary] Failed to parse {fileName}.json: {e.Message}");
                return new T();
            }
        }

        private static void BuildLookups()
        {
            ToolLookup.Clear();
            foreach (ToolData tool in _tools.tools)
            {
                ToolType type = tool.Type;
                if (type != ToolType.None)
                {
                    ToolLookup[type] = tool;
                }
            }

            ProcedureLookup.Clear();
            foreach (ProcedureData procedure in _procedures.procedures)
            {
                if (!string.IsNullOrEmpty(procedure.id))
                {
                    ProcedureLookup[procedure.id] = procedure;
                }
            }

            ComplicationLookup.Clear();
            foreach (ComplicationData complication in _complications.complications)
            {
                ComplicationLookup[complication.Type] = complication;
            }
        }

        public static ToolData GetTool(ToolType type)
        {
            EnsureLoaded();
            return ToolLookup.TryGetValue(type, out ToolData data) ? data : null;
        }

        public static ProcedureData GetProcedure(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return ProcedureLookup.TryGetValue(id, out ProcedureData data) ? data : null;
        }

        public static ComplicationData GetComplication(ComplicationType type)
        {
            EnsureLoaded();
            return ComplicationLookup.TryGetValue(type, out ComplicationData data) ? data : null;
        }

        public static TrainingStationData GetTrainingStation(string id)
        {
            EnsureLoaded();
            foreach (TrainingStationData station in _training.stations)
            {
                if (station.id == id)
                {
                    return station;
                }
            }

            return null;
        }

        public static ChallengeData GetChallenge(string id)
        {
            EnsureLoaded();
            foreach (ChallengeData challenge in _challenges.challenges)
            {
                if (challenge.id == id)
                {
                    return challenge;
                }
            }

            return null;
        }

        public static UpgradeData GetUpgrade(string id)
        {
            EnsureLoaded();
            foreach (UpgradeData upgrade in _upgrades.upgrades)
            {
                if (upgrade.id == id)
                {
                    return upgrade;
                }
            }

            return null;
        }
    }
}
