using System.Collections.Generic;
using TraumaSurgeon.Career;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Modes;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Case, training station and challenge picker, plus difficulty selection.
    /// Reused for Play Now, Training and Challenge modes.
    /// </summary>
    public class CaseSelectScreen : UIScreen
    {
        private GameModeType _mode = GameModeType.PlayNow;
        private DifficultyLevel _difficulty = DifficultyLevel.Resident;

        private RectTransform _listContent;
        private Text _headerText;
        private Text _difficultyDescription;
        private readonly List<Button> _difficultyButtons = new List<Button>();

        protected override void Build()
        {
            RectTransform header = UIFactory.CreateRect("Header", Root);
            UIFactory.Anchor(header, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(90f, -110f), new Vector2(-90f, -40f));

            _headerText = UIFactory.CreateText("HeaderText", header, "SELECT CASE", UITheme.FontTitle,
                UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)_headerText.transform);

            // Case list.
            RectTransform listRoot = UIFactory.CreateRect("ListRoot", Root);
            UIFactory.Anchor(listRoot, new Vector2(0f, 0f), new Vector2(0.62f, 1f),
                new Vector2(90f, 100f), new Vector2(-20f, -130f));
            _listContent = UIFactory.CreateScrollView("CaseList", listRoot, out _);
            UIFactory.Stretch((RectTransform)_listContent.parent.parent);

            // Difficulty panel.
            RectTransform side = UIFactory.CreateRect("Side", Root);
            UIFactory.Anchor(side, new Vector2(0.64f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 100f), new Vector2(-90f, -130f));

            Image sideBg = side.gameObject.AddComponent<Image>();
            sideBg.color = UITheme.Panel;

            VerticalLayoutGroup sideGroup = side.gameObject.AddComponent<VerticalLayoutGroup>();
            sideGroup.spacing = 8f;
            sideGroup.padding = new RectOffset(20, 20, 20, 20);
            sideGroup.childForceExpandHeight = false;
            sideGroup.childControlHeight = true;
            sideGroup.childControlWidth = true;

            UIFactory.CreateHeader(side, "DIFFICULTY", "Sets guidance, deterioration and scoring");

            _difficultyButtons.Clear();
            foreach (DifficultyProfile profile in DifficultyProfile.All)
            {
                DifficultyLevel level = profile.Level;
                Button button = UIFactory.CreateButton("Diff_" + level, side, profile.DisplayName,
                    () => SetDifficulty(level), UITheme.FontSubheading);
                UIFactory.SetSize(button.gameObject, 42f);
                _difficultyButtons.Add(button);
            }

            _difficultyDescription = UIFactory.CreateText("DiffDesc", side, "", UITheme.FontSmall,
                UITheme.TextMuted);
            UIFactory.SetSize(_difficultyDescription.gameObject, 110f);

            // Footer.
            Button back = UIFactory.CreateButton("Back", Root, "BACK TO MENU",
                () => GameManager.Instance.GoToMainMenu(), UITheme.FontBody);
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(90f, 36f), new Vector2(90f, 36f));
            ((RectTransform)back.transform).sizeDelta = new Vector2(240f, 48f);
            ((RectTransform)back.transform).pivot = Vector2.zero;
        }

        /// <summary>Opens the screen in one of the selectable modes.</summary>
        public void OpenFor(GameModeType mode)
        {
            _mode = mode;
            GameManager.Instance.SetPhase(GamePhase.CaseSelect);
        }

        private void SetDifficulty(DifficultyLevel level)
        {
            _difficulty = level;
            RefreshDifficulty();
        }

        private void RefreshDifficulty()
        {
            DifficultyProfile profile = DifficultyProfile.Get(_difficulty);
            _difficultyDescription.text =
                $"<b>{profile.DisplayName}</b>\n{profile.Description}\n\n" +
                $"Deterioration x{profile.DeteriorationRate:0.00}  ·  " +
                $"Complications x{profile.ComplicationRate:0.00}\n" +
                $"Score x{profile.ScoreMultiplier:0.00}  ·  " +
                $"Assistance {(profile.AssistanceUses < 0 ? "unlimited" : profile.AssistanceUses.ToString())}";

            for (int i = 0; i < _difficultyButtons.Count; i++)
            {
                Image image = _difficultyButtons[i].GetComponent<Image>();
                image.color = (DifficultyLevel)i == _difficulty ? UITheme.AccentDim : UITheme.ButtonNormal;
            }
        }

        public override void Refresh()
        {
            RefreshDifficulty();
            ClearChildren(_listContent);

            switch (_mode)
            {
                case GameModeType.Training:
                    _headerText.text = "TRAINING STATIONS";
                    BuildTrainingList();
                    break;
                case GameModeType.Challenge:
                    _headerText.text = "CHALLENGES";
                    BuildChallengeList();
                    break;
                default:
                    _headerText.text = "SELECT CASE";
                    BuildProcedureList();
                    break;
            }
        }

        /// <summary>
        /// Shown instead of an empty panel when a list has nothing in it, so a data or layout
        /// failure reports itself rather than looking like a blank screen.
        /// </summary>
        private void ShowEmptyNotice(string message)
        {
            Text notice = UIFactory.CreateText("Empty", _listContent, message, UITheme.FontBody,
                UITheme.Warning);
            UIFactory.SetSize(notice.gameObject, 80f);
        }

        private void BuildProcedureList()
        {
            if (DataLibrary.Procedures.Count == 0)
            {
                ShowEmptyNotice(
                    "No procedures loaded.\n\nExpected JSON at " +
                    "Assets/TraumaSurgeon/Resources/TraumaSurgeonData/procedures.json\n" +
                    "Run Trauma Surgeon → Validate Data Files and check the console.");
                Debug.LogError("[CaseSelect] DataLibrary returned zero procedures.");
                return;
            }

            foreach (ProcedureData procedure in DataLibrary.Procedures)
            {
                bool unlocked = !CareerManager.Exists || CareerManager.Instance.IsProcedureUnlocked(procedure);
                ProcedureData captured = procedure;

                string bestText = string.Empty;
                if (CareerManager.Exists)
                {
                    BestScoreEntryLookup(procedure.id, out bestText);
                }

                string label =
                    $"<b>{procedure.displayName}</b>   <size={UITheme.FontSmall}>" +
                    $"<color=#9AACB6>{procedure.bodyRegion} · par {procedure.parTimeSeconds / 60} min · " +
                    $"{procedure.steps.Length} steps{bestText}</color></size>\n" +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>{procedure.description}</color></size>";

                if (!unlocked)
                {
                    label = $"<b>{procedure.displayName}</b>  <color=#F05C5C>LOCKED</color>\n" +
                            $"<size={UITheme.FontSmall}><color=#9AACB6>Requires " +
                            $"{CareerManager.RankTitles[Mathf.Clamp(procedure.unlockLevel, 0, 6)]}</color></size>";
                }

                Button button = UIFactory.CreateButton("Case_" + procedure.id, _listContent, label, () =>
                {
                    if (unlocked)
                    {
                        GameManager.Instance.SelectCase(captured, _difficulty, _mode);
                    }
                }, UITheme.FontSubheading);

                UIFactory.SetSize(button.gameObject, 86f);
                if (!unlocked)
                {
                    button.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
                }
            }
        }

        private static void BestScoreEntryLookup(string procedureId, out string suffix)
        {
            suffix = string.Empty;
            var best = CareerManager.Instance.Data.bestScores.Find(b => b.procedureId == procedureId);
            if (best != null)
            {
                suffix = $" · best {best.rank} ({best.score})";
            }
        }

        private void BuildTrainingList()
        {
            if (DataLibrary.TrainingStations.Count == 0)
            {
                ShowEmptyNotice("No training stations loaded. Check training.json.");
                return;
            }

            foreach (TrainingStationData station in DataLibrary.TrainingStations)
            {
                TrainingStationData captured = station;
                bool complete = CareerManager.Exists && CareerManager.Instance.IsTrainingComplete(station.id);

                string label =
                    $"<b>{station.displayName}</b>{(complete ? "  <color=#5AD48C>COMPLETE</color>" : "")}\n" +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>{station.description} · " +
                    $"{station.targetRepetitions} reps · {station.timeLimitSeconds:0}s</color></size>";

                Button button = UIFactory.CreateButton("Train_" + station.id, _listContent, label,
                    () => TrainingManager.StartStation(captured), UITheme.FontSubheading);
                UIFactory.SetSize(button.gameObject, 76f);
            }
        }

        private void BuildChallengeList()
        {
            if (DataLibrary.Challenges.Count == 0)
            {
                ShowEmptyNotice("No challenges loaded. Check challenges.json.");
                return;
            }

            foreach (ChallengeData challenge in DataLibrary.Challenges)
            {
                ChallengeData captured = challenge;
                ProcedureData procedure = DataLibrary.GetProcedure(challenge.procedureId);
                bool complete = CareerManager.Exists &&
                                CareerManager.Instance.Data.completedChallenges.Contains(challenge.id);

                var rules = new List<string>();
                if (challenge.timeLimitSeconds > 0f) rules.Add($"{challenge.timeLimitSeconds:0}s limit");
                if (challenge.powerFailure) rules.Add("power failure");
                if (challenge.limitedBlood) rules.Add("limited blood");
                if (challenge.noAssistance) rules.Add("no assistance");
                if (challenge.faultyEquipment) rules.Add("faulty equipment");

                string label =
                    $"<b>{challenge.displayName}</b>{(complete ? "  <color=#5AD48C>CLEARED</color>" : "")}\n" +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>{challenge.description}\n" +
                    $"{(procedure != null ? procedure.displayName : challenge.procedureId)} · " +
                    $"{string.Join(" · ", rules)}</color></size>";

                Button button = UIFactory.CreateButton("Chal_" + challenge.id, _listContent, label, () =>
                {
                    if (procedure != null)
                    {
                        GameManager.Instance.SelectCase(procedure, captured.Difficulty,
                            GameModeType.Challenge, captured);
                    }
                }, UITheme.FontSubheading);

                UIFactory.SetSize(button.gameObject, 92f);
            }
        }
    }
}
