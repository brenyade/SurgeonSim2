using TraumaSurgeon.Career;
using TraumaSurgeon.Core;
using TraumaSurgeon.Save;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>Title screen with the four game modes, settings, save/load and quit.</summary>
    public class MainMenuScreen : UIScreen
    {
        private Text _careerSummary;

        protected override void Build()
        {
            // Left column: title and career summary.
            RectTransform left = UIFactory.CreateRect("Left", Root);
            UIFactory.Anchor(left, new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(110f, 90f), new Vector2(-40f, -120f));

            VerticalLayoutGroup leftGroup = left.gameObject.AddComponent<VerticalLayoutGroup>();
            leftGroup.spacing = 10f;
            leftGroup.childAlignment = TextAnchor.MiddleLeft;
            leftGroup.childForceExpandHeight = false;
            leftGroup.childControlHeight = true;
            leftGroup.childControlWidth = true;

            Text title = UIFactory.CreateText("Title", left, "TRAUMA SURGEON", 72,
                UITheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.SetSize(title.gameObject, 84f);

            Text subtitle = UIFactory.CreateText("Subtitle", left,
                "A first-person operating theatre simulation", UITheme.FontSubheading, UITheme.Accent,
                TextAnchor.MiddleLeft);
            UIFactory.SetSize(subtitle.gameObject, 30f);

            UIFactory.CreateDivider(left);

            _careerSummary = UIFactory.CreateText("CareerSummary", left, "", UITheme.FontBody,
                UITheme.TextMuted, TextAnchor.UpperLeft);
            UIFactory.SetSize(_careerSummary.gameObject, 140f);

            Text controls = UIFactory.CreateText("Controls", left,
                "<b>Controls</b>  WASD move · Mouse look · LMB use tool · RMB alternate · " +
                "E interact · Q return tool · 1-9 tools · Shift steady hand · Tab chart · " +
                "C command wheel · Space assistance · Esc pause",
                UITheme.FontSmall, UITheme.TextMuted, TextAnchor.UpperLeft);
            UIFactory.SetSize(controls.gameObject, 90f);

            // Right column: mode buttons.
            RectTransform right = UIFactory.CreateRect("Right", Root);
            UIFactory.Anchor(right, new Vector2(0.55f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 120f), new Vector2(-140f, -140f));

            VerticalLayoutGroup group = right.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 12f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;

            AddButton(right, "CAREER MODE", "Progress from student to chief of surgery", () =>
            {
                if (SaveManager.Exists && SaveManager.Instance.CurrentSlot < 0)
                {
                    UIManager.Instance.SaveLoad.OpenForNewCareer();
                }
                else
                {
                    GameManager.Instance.GoToHospital();
                }
            });

            AddButton(right, "PLAY NOW", "Jump straight into any unlocked operation",
                () => UIManager.Instance.CaseSelect.OpenFor(GameModeType.PlayNow));

            AddButton(right, "TRAINING", "Practise individual skills - the patient cannot die",
                () => UIManager.Instance.CaseSelect.OpenFor(GameModeType.Training));

            AddButton(right, "CHALLENGES", "Special rules: time limits, power cuts, no help",
                () => UIManager.Instance.CaseSelect.OpenFor(GameModeType.Challenge));

            UIFactory.CreateDivider(right);

            AddButton(right, "SAVE / LOAD", "", () => UIManager.Instance.SaveLoad.OpenForLoad());
            AddButton(right, "SETTINGS", "", () => UIManager.Instance.OpenOverlay(UIManager.Instance.Settings));
            AddButton(right, "QUIT", "", Quit);
        }

        private void AddButton(Transform parent, string label, string tooltip, System.Action action)
        {
            Button button = UIFactory.CreateButton("Btn_" + label, parent, label, action,
                UITheme.FontSubheading);
            UIFactory.SetSize(button.gameObject, string.IsNullOrEmpty(tooltip) ? 48f : 58f);

            if (!string.IsNullOrEmpty(tooltip))
            {
                Text text = button.GetComponentInChildren<Text>();
                text.text = $"{label}\n<size={UITheme.FontSmall}><color=#9AACB6>{tooltip}</color></size>";
            }
        }

        public override void Refresh()
        {
            if (_careerSummary == null)
            {
                return;
            }

            if (!CareerManager.Exists || !SaveManager.Exists || SaveManager.Instance.CurrentSlot < 0)
            {
                _careerSummary.text = "<b>No career loaded.</b>\nStart Career Mode to create one, " +
                                      "or use Play Now for a one-off case.";
                return;
            }

            CareerData data = CareerManager.Instance.Data;
            _careerSummary.text =
                $"<b>{data.surgeonName}</b>  ·  {CareerManager.Instance.RankTitle}\n" +
                $"Experience {data.experience}  ·  Funds ${data.money:N0}\n" +
                $"Reputation {data.reputation:0}/100  ·  Malpractice risk {data.malpracticeRisk:0}%\n" +
                $"Patients treated {data.patientsTreated}  ·  Lost {data.patientsLost}\n" +
                $"Hospital rating {data.hospitalRating:0.0}/5.0";
        }

        private static void Quit()
        {
            if (SettingsManager.Exists)
            {
                SettingsManager.Instance.Save();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
