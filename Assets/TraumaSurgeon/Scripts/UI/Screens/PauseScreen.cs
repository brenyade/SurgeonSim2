using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>Escape menu during surgery.</summary>
    public class PauseScreen : UIScreen
    {
        private Text _caseText;

        protected override void Build()
        {
            Image dim = RootBackground;
            dim.color = new Color(0.02f, 0.03f, 0.04f, 0.86f);

            RectTransform panel = UIFactory.CreateRect("Panel", Root);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.sizeDelta = new Vector2(520f, 560f);
            panel.gameObject.AddComponent<Image>().color = UITheme.Panel;

            VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.padding = new RectOffset(28, 28, 28, 28);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;

            UIFactory.CreateHeader(panel, "PAUSED", "");

            _caseText = UIFactory.CreateText("CaseInfo", panel, "", UITheme.FontSmall, UITheme.TextMuted);
            UIFactory.SetSize(_caseText.gameObject, 90f);

            Add(panel, "RESUME", () => GameManager.Instance.SetPaused(false));
            Add(panel, "SETTINGS", () => UIManager.Instance.OpenOverlay(UIManager.Instance.Settings));
            Add(panel, "SAVE / LOAD", () => UIManager.Instance.SaveLoad.OpenForLoad());
            Add(panel, "RESTART CASE", () =>
            {
                GameManager.Instance.SetPaused(false);
                GameManager.Instance.RetryCase();
            });

            Add(panel, "ABANDON OPERATION", () =>
            {
                GameManager.Instance.SetPaused(false);
                if (SurgeryManager.Active != null)
                {
                    SurgeryManager.Active.Abort();
                }
                else
                {
                    GameManager.Instance.GoToMainMenu();
                }
            });

            Add(panel, "QUIT TO MAIN MENU", () =>
            {
                GameManager.Instance.SetPaused(false);
                GameManager.Instance.GoToMainMenu();
            });
        }

        private void Add(Transform parent, string label, System.Action action)
        {
            Button button = UIFactory.CreateButton("Btn_" + label, parent, label, action, UITheme.FontSubheading);
            UIFactory.SetSize(button.gameObject, 48f);
        }

        public override void Refresh()
        {
            SurgeryManager surgery = SurgeryManager.Active;
            if (surgery == null || surgery.Procedure == null)
            {
                _caseText.text = string.Empty;
                return;
            }

            _caseText.text = $"<b>{surgery.Procedure.displayName}</b>\n" +
                             $"{surgery.Patient.Template.name} · {surgery.Profile.DisplayName}\n" +
                             $"Objectives {surgery.Objectives.CompletedCount}/{surgery.Objectives.Steps.Count} · " +
                             $"blood loss {surgery.Patient.BloodLoss.TotalLostMl:0} ml";
        }
    }
}
