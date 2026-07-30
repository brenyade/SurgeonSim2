using TraumaSurgeon.Audio;
using TraumaSurgeon.Career;
using TraumaSurgeon.Data;
using TraumaSurgeon.Environment;
using TraumaSurgeon.InputSystem;
using TraumaSurgeon.Player;
using TraumaSurgeon.Save;
using TraumaSurgeon.Scoring;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Core
{
    /// <summary>
    /// Top level application flow: which phase we are in, starting and ending cases, and owning
    /// the persistent objects (operating room, player rig). Everything is constructed at runtime
    /// so the game runs from a single near-empty scene.
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public GameModeType Mode { get; private set; } = GameModeType.Career;
        public DifficultyLevel DifficultyLevel { get; private set; } = DifficultyLevel.Resident;
        public DifficultyProfile Difficulty { get; private set; } = DifficultyProfile.Get(DifficultyLevel.Resident);

        public OperatingRoomBuilder Room { get; private set; }
        public PlayerRigBuilder.Rig PlayerRig { get; private set; }
        public SurgeryManager CurrentSurgery { get; private set; }
        public SurgeryReport LastReport { get; private set; }
        public ProcedureData PendingProcedure { get; private set; }
        public ChallengeData PendingChallenge { get; private set; }
        public ScheduledCase PendingScheduledCase { get; private set; }

        public bool IsPaused { get; private set; }

        protected override void OnSingletonAwake()
        {
            DataLibrary.EnsureLoaded();
        }

        private void OnEnable()
        {
            GameEvents.SurgeryCompleted += OnSurgeryCompleted;
        }

        private void OnDisable()
        {
            GameEvents.SurgeryCompleted -= OnSurgeryCompleted;
        }

        // ---- Phase control ----------------------------------------------------

        public void SetPhase(GamePhase phase)
        {
            Phase = phase;
            GameEvents.RaisePhaseChanged(phase);

            bool gameplay = phase == GamePhase.Surgery;
            SetCursorLocked(gameplay && !IsPaused);

            if (InputManager.Exists)
            {
                InputManager.Instance.GameplayInputBlocked = !gameplay || IsPaused;
            }
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void GoToMainMenu()
        {
            EndCurrentSurgery(true);
            DestroyWorld();
            SetPhase(GamePhase.MainMenu);
        }

        public void GoToHospital()
        {
            EndCurrentSurgery(true);
            DestroyWorld();
            if (HospitalManager.Exists && HospitalManager.Instance.Schedule.Count == 0)
            {
                HospitalManager.Instance.GenerateSchedule();
            }

            SetPhase(GamePhase.Hospital);
        }

        // ---- Case flow --------------------------------------------------------

        /// <summary>Selects a procedure and moves to the briefing (chart/imaging) screen.</summary>
        public void SelectCase(ProcedureData procedure, DifficultyLevel difficulty,
            GameModeType mode, ChallengeData challenge = null, ScheduledCase scheduled = null)
        {
            PendingProcedure = procedure;
            PendingChallenge = challenge;
            PendingScheduledCase = scheduled;
            DifficultyLevel = difficulty;
            Difficulty = DifficultyProfile.Get(difficulty);
            Mode = mode;
            SetPhase(GamePhase.Briefing);
        }

        /// <summary>Builds the theatre and starts the operation.</summary>
        public void BeginSurgery()
        {
            if (PendingProcedure == null)
            {
                Debug.LogError("[GameManager] BeginSurgery called with no procedure selected.");
                return;
            }

            EnsureWorld();

            CurrentSurgery = SurgeryManager.Create(transform);
            CurrentSurgery.SetupCase(PendingProcedure, DifficultyLevel, Mode, Room, PlayerRig, PendingChallenge);

            // Career upgrades feed into the case.
            if (HospitalManager.Exists && Mode == GameModeType.Career)
            {
                CurrentSurgery.Complications.EquipmentReliability = HospitalManager.Instance.EquipmentReliability;
                CurrentSurgery.Patient.BloodLoss.AvailableUnits += HospitalManager.Instance.ExtraBloodUnits;
            }

            if (PlayerRig != null && Room != null)
            {
                PlayerRig.Movement.Warp(Room.transform.TransformPoint(Room.SurgeonSpawn), 270f);
            }

            IsPaused = false;
            Time.timeScale = 1f;
            SetPhase(GamePhase.Surgery);
        }

        private void EnsureWorld()
        {
            if (Room == null)
            {
                Room = OperatingRoomBuilder.Build(null);
            }

            if (PlayerRig == null)
            {
                Vector3 spawn = Room.transform.TransformPoint(Room.SurgeonSpawn);
                PlayerRig = PlayerRigBuilder.Build(spawn, 270f);
            }

            PlayerRig.Root.SetActive(true);
        }

        private void DestroyWorld()
        {
            if (CurrentSurgery != null)
            {
                CurrentSurgery.Teardown();
                CurrentSurgery = null;
            }

            if (Room != null)
            {
                Destroy(Room.gameObject);
                Room = null;
            }

            if (PlayerRig != null)
            {
                Destroy(PlayerRig.Root);
                PlayerRig = null;
            }
        }

        private void EndCurrentSurgery(bool teardown)
        {
            if (CurrentSurgery == null)
            {
                return;
            }

            if (teardown)
            {
                CurrentSurgery.Teardown();
                CurrentSurgery = null;
            }
        }

        private void OnSurgeryCompleted(SurgeryReport report)
        {
            LastReport = report;

            if (Mode == GameModeType.Career && CareerManager.Exists)
            {
                CareerManager.Instance.RecordSurgery(report, PendingProcedure, DifficultyLevel);
                if (PendingScheduledCase != null && HospitalManager.Exists)
                {
                    HospitalManager.Instance.RemoveCase(PendingScheduledCase);
                    PendingScheduledCase = null;
                }
            }
            else if (Mode == GameModeType.Challenge && CareerManager.Exists && PendingChallenge != null)
            {
                if (report.patientSurvived)
                {
                    CareerManager.Instance.MarkChallengeComplete(PendingChallenge.id,
                        PendingChallenge.rewardExperience, PendingChallenge.rewardMoney);
                }
            }

            SetPhase(GamePhase.PostOp);
        }

        /// <summary>Called from the post-op report screen.</summary>
        public void DismissReport()
        {
            DestroyWorld();

            if (Mode == GameModeType.Career)
            {
                GoToHospital();
            }
            else
            {
                GoToMainMenu();
            }
        }

        /// <summary>Restarts the same case from the top.</summary>
        public void RetryCase()
        {
            DestroyWorld();
            BeginSurgery();
        }

        // ---- Pause ------------------------------------------------------------

        public void SetPaused(bool paused)
        {
            if (Phase != GamePhase.Surgery && !paused)
            {
                IsPaused = false;
                Time.timeScale = 1f;
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            SetCursorLocked(!paused && Phase == GamePhase.Surgery);

            if (InputManager.Exists)
            {
                InputManager.Instance.GameplayInputBlocked = paused || Phase != GamePhase.Surgery;
            }
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        // ---- Quick start helpers ---------------------------------------------

        /// <summary>Play Now: jump straight into a procedure by id.</summary>
        public void QuickPlay(string procedureId, DifficultyLevel difficulty)
        {
            ProcedureData procedure = DataLibrary.GetProcedure(procedureId);
            if (procedure == null)
            {
                Debug.LogError($"[GameManager] Unknown procedure id: {procedureId}");
                return;
            }

            SelectCase(procedure, difficulty, GameModeType.PlayNow);
        }

        private void Update()
        {
            // Escape toggles pause during surgery, or backs out of menus.
            if (InputManager.Exists && InputManager.Instance.PressedRaw(GameAction.Pause) &&
                Phase == GamePhase.Surgery)
            {
                TogglePause();
                if (AudioManager.Exists)
                {
                    AudioManager.Instance.PlayUI(SoundId.UiClick);
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (SettingsManager.Exists)
            {
                SettingsManager.Instance.Save();
            }

            if (SaveManager.Exists && SaveManager.Instance.CurrentSlot >= 0)
            {
                SaveManager.Instance.SaveCurrent();
            }
        }
    }
}
