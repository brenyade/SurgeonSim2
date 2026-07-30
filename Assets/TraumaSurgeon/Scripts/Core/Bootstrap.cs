using TraumaSurgeon.Audio;
using TraumaSurgeon.Career;
using TraumaSurgeon.Data;
using TraumaSurgeon.InputSystem;
using TraumaSurgeon.Save;
using TraumaSurgeon.UI;
using UnityEngine;

namespace TraumaSurgeon.Core
{
    /// <summary>
    /// Single entry point. Sitting in the Bootstrap scene, it creates every persistent manager in
    /// a deterministic order and then hands control to the main menu.
    ///
    /// The whole game is constructed at runtime, so the only authored scene is the near-empty
    /// Bootstrap scene that hosts this component.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Bootstrap : MonoBehaviour
    {
        [Tooltip("Skip the menu and drop straight into a case (leave empty for normal boot).")]
        public string debugProcedureId = "";

        [Tooltip("Difficulty used by the debug quick start.")]
        public DifficultyLevel debugDifficulty = DifficultyLevel.Resident;

        private static bool _created;

        /// <summary>
        /// Safety net: if the game is started from an empty scene without the Bootstrap object,
        /// create one anyway so the project always runs.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (_created || FindAnyObjectByType<Bootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();
        }

        private void Awake()
        {
            if (_created)
            {
                Destroy(gameObject);
                return;
            }

            _created = true;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 120;

            CreateManagers();
            DataLibrary.EnsureLoaded();
            ValidateData();
        }

        private void Start()
        {
            AudioManager.Instance.StartLoop(SoundId.Ambience, 0.35f);

            if (!string.IsNullOrEmpty(debugProcedureId))
            {
                SaveManager.Instance.EnsureCareer();
                GameManager.Instance.QuickPlay(debugProcedureId, debugDifficulty);
                return;
            }

            GameManager.Instance.SetPhase(GamePhase.MainMenu);
        }

        /// <summary>
        /// Order matters: input before settings (settings pushes bindings into it), save before
        /// career (career reads the active slot), UI last (it queries everything).
        /// </summary>
        private void CreateManagers()
        {
            var managers = new GameObject("Managers");
            managers.transform.SetParent(transform, false);

            managers.AddComponent<InputManager>();
            managers.AddComponent<SaveManager>();
            managers.AddComponent<SettingsManager>();
            managers.AddComponent<AudioManager>();
            managers.AddComponent<CareerManager>();
            managers.AddComponent<HospitalManager>();
            managers.AddComponent<GameManager>();
            managers.AddComponent<UIManager>();
        }

        /// <summary>Logs data problems at boot so a bad JSON edit is obvious immediately.</summary>
        private static void ValidateData()
        {
            // Always report the counts. If a list in the UI comes up empty, this line in the
            // console immediately separates "data did not load" from "the UI did not draw it".
            Debug.Log($"[Trauma Surgeon] Content loaded - {DataLibrary.Procedures.Count} procedures, " +
                      $"{DataLibrary.Tools.Count} tools, {DataLibrary.Complications.Count} complications, " +
                      $"{DataLibrary.TrainingStations.Count} training stations, " +
                      $"{DataLibrary.Challenges.Count} challenges, {DataLibrary.Upgrades.Count} upgrades.");

            if (DataLibrary.Procedures.Count == 0)
            {
                Debug.LogError("[Bootstrap] No procedures loaded. Check " +
                               "Assets/TraumaSurgeon/Resources/TraumaSurgeonData/procedures.json");
            }

            if (DataLibrary.Tools.Count == 0)
            {
                Debug.LogError("[Bootstrap] No tools loaded. Check " +
                               "Assets/TraumaSurgeon/Resources/TraumaSurgeonData/tools.json");
            }

            foreach (ProcedureData procedure in DataLibrary.Procedures)
            {
                if (procedure.steps == null || procedure.steps.Length == 0)
                {
                    Debug.LogWarning($"[Bootstrap] Procedure '{procedure.id}' has no steps.");
                }

                foreach (string tool in procedure.requiredTools)
                {
                    if (!System.Enum.TryParse(tool, true, out ToolType _))
                    {
                        Debug.LogWarning($"[Bootstrap] Procedure '{procedure.id}' lists unknown tool '{tool}'.");
                    }
                }
            }
        }
    }
}
