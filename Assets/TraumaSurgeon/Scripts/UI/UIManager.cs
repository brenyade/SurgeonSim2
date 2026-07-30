using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.InputSystem;
using TraumaSurgeon.Scoring;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Owns the canvases and every screen. Reacts to <see cref="GamePhase"/> changes to show the
    /// right screen, and handles the overlay hotkeys (chart, command wheel, pause).
    /// </summary>
    public class UIManager : MonoSingleton<UIManager>
    {
        public Canvas MainCanvas { get; private set; }
        public Canvas OverlayCanvas { get; private set; }

        public MainMenuScreen MainMenu { get; private set; }
        public CaseSelectScreen CaseSelect { get; private set; }
        public BriefingScreen Briefing { get; private set; }
        public HudScreen Hud { get; private set; }
        public PatientChartScreen Chart { get; private set; }
        public PauseScreen Pause { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public SaveLoadScreen SaveLoad { get; private set; }
        public PostOpScreen PostOp { get; private set; }
        public HospitalScreen Hospital { get; private set; }
        public CommandWheelScreen CommandWheel { get; private set; }
        public NotificationFeed Notifications { get; private set; }

        private readonly List<UIScreen> _screens = new List<UIScreen>();
        private UIScreen _activeScreen;

        protected override void OnSingletonAwake()
        {
            UIFactory.EnsureEventSystem();

            MainCanvas = UIFactory.CreateCanvas("MainCanvas", 100, transform);
            OverlayCanvas = UIFactory.CreateCanvas("OverlayCanvas", 200, transform);

            MainMenu = Create<MainMenuScreen>(MainCanvas.transform);
            CaseSelect = Create<CaseSelectScreen>(MainCanvas.transform);
            Briefing = Create<BriefingScreen>(MainCanvas.transform);
            Hospital = Create<HospitalScreen>(MainCanvas.transform);
            PostOp = Create<PostOpScreen>(MainCanvas.transform);
            Hud = Create<HudScreen>(MainCanvas.transform);

            Chart = Create<PatientChartScreen>(OverlayCanvas.transform);
            CommandWheel = Create<CommandWheelScreen>(OverlayCanvas.transform);
            Pause = Create<PauseScreen>(OverlayCanvas.transform);
            Settings = Create<SettingsScreen>(OverlayCanvas.transform);
            SaveLoad = Create<SaveLoadScreen>(OverlayCanvas.transform);

            Notifications = gameObject.AddComponent<NotificationFeed>();
            Notifications.Initialise(OverlayCanvas.transform);
        }

        private T Create<T>(Transform canvas) where T : UIScreen
        {
            var go = new GameObject(typeof(T).Name + "_Holder");
            go.transform.SetParent(transform, false);
            T screen = go.AddComponent<T>();
            screen.Initialise(canvas);
            _screens.Add(screen);
            return screen;
        }

        private void OnEnable()
        {
            GameEvents.PhaseChanged += OnPhaseChanged;
            GameEvents.SurgeryCompleted += OnSurgeryCompleted;
        }

        private void OnDisable()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
            GameEvents.SurgeryCompleted -= OnSurgeryCompleted;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MainMenu:
                    ShowExclusive(MainMenu);
                    break;
                case GamePhase.CaseSelect:
                    ShowExclusive(CaseSelect);
                    break;
                case GamePhase.Briefing:
                    ShowExclusive(Briefing);
                    break;
                case GamePhase.Surgery:
                    ShowExclusive(Hud);
                    break;
                case GamePhase.PostOp:
                    ShowExclusive(PostOp);
                    break;
                case GamePhase.Hospital:
                    ShowExclusive(Hospital);
                    break;
            }
        }

        private void OnSurgeryCompleted(SurgeryReport report)
        {
            CloseAllOverlays();
        }

        /// <summary>Shows one main screen and hides the others.</summary>
        public void ShowExclusive(UIScreen screen)
        {
            foreach (UIScreen s in _screens)
            {
                if (s == screen)
                {
                    continue;
                }

                if (s.Root != null && s.Root.parent == MainCanvas.transform)
                {
                    s.Hide();
                }
            }

            CloseAllOverlays();
            _activeScreen = screen;
            screen.Show();
        }

        public void CloseAllOverlays()
        {
            Chart.Hide();
            CommandWheel.Hide();
            Pause.Hide();
            Settings.Hide();
            SaveLoad.Hide();
        }

        /// <summary>Opens an overlay on top of whatever is showing.</summary>
        public void OpenOverlay(UIScreen screen)
        {
            screen.Show();
            UpdateInputBlocking();
        }

        public void CloseOverlay(UIScreen screen)
        {
            screen.Hide();
            UpdateInputBlocking();
        }

        public void ToggleOverlay(UIScreen screen)
        {
            if (screen.IsVisible)
            {
                CloseOverlay(screen);
            }
            else
            {
                OpenOverlay(screen);
            }
        }

        private void UpdateInputBlocking()
        {
            if (!GameManager.Exists || !InputManager.Exists)
            {
                return;
            }

            bool overlayBlocking = (Chart.IsVisible && Chart.BlocksGameplay) ||
                                   Pause.IsVisible || Settings.IsVisible || SaveLoad.IsVisible;

            bool inSurgery = GameManager.Instance.Phase == GamePhase.Surgery;
            InputManager.Instance.GameplayInputBlocked = !inSurgery || overlayBlocking ||
                                                        GameManager.Instance.IsPaused;
            GameManager.Instance.SetCursorLocked(inSurgery && !overlayBlocking &&
                                                 !GameManager.Instance.IsPaused);
        }

        private void Update()
        {
            if (!GameManager.Exists || !InputManager.Exists)
            {
                return;
            }

            InputManager input = InputManager.Instance;
            bool inSurgery = GameManager.Instance.Phase == GamePhase.Surgery;

            // Pause overlay follows GameManager's paused flag.
            if (GameManager.Instance.IsPaused && !Pause.IsVisible)
            {
                Pause.Show();
            }
            else if (!GameManager.Instance.IsPaused && Pause.IsVisible && !Settings.IsVisible && !SaveLoad.IsVisible)
            {
                Pause.Hide();
            }

            if (!inSurgery)
            {
                return;
            }

            if (input.PressedRaw(GameAction.PatientChart) && !GameManager.Instance.IsPaused)
            {
                ToggleOverlay(Chart);
            }

            if (input.PressedRaw(GameAction.CommandWheel) && !GameManager.Instance.IsPaused)
            {
                ToggleOverlay(CommandWheel);
            }

            if (input.Pressed(GameAction.RequestAssistance) && SurgeryManager.Active != null)
            {
                SurgeryManager.Active.RequestAssistance();
            }
        }
    }
}
