using TraumaSurgeon.Core;
using TraumaSurgeon.Staff;
using TraumaSurgeon.Surgery;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Radial command wheel (hold C). Each spoke issues an order to the team; every entry also has
    /// a function-key shortcut handled by <see cref="StaffCommandSystem"/>.
    /// </summary>
    public class CommandWheelScreen : UIScreen
    {
        protected override bool Transparent => true;
        public override bool BlocksGameplay => false;

        private Text _centreText;

        protected override void Build()
        {
            Image dim = RootBackground;
            dim.color = new Color(0.02f, 0.03f, 0.04f, 0.55f);

            RectTransform wheel = UIFactory.CreateRect("Wheel", Root);
            UIFactory.Anchor(wheel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            wheel.sizeDelta = new Vector2(900f, 900f);

            int count = StaffCommandSystem.Commands.Length;
            float radius = 300f;

            for (int i = 0; i < count; i++)
            {
                StaffCommandInfo info = StaffCommandSystem.Commands[i];
                float angle = (float)i / count * Mathf.PI * 2f - Mathf.PI * 0.5f;
                Vector2 position = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;

                Button button = UIFactory.CreateButton("Cmd_" + info.Command, wheel,
                    $"{info.Label}\n<size={UITheme.FontSmall}><color=#9AACB6>{info.Shortcut}</color></size>",
                    () => Issue(info.Command), UITheme.FontSmall);

                RectTransform rect = (RectTransform)button.transform;
                UIFactory.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(210f, 62f);
            }

            RectTransform centre = UIFactory.CreateRect("Centre", wheel);
            UIFactory.Anchor(centre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            centre.sizeDelta = new Vector2(330f, 200f);
            centre.gameObject.AddComponent<Image>().color = UITheme.Panel;

            _centreText = UIFactory.CreateText("CentreText", centre, "", UITheme.FontBody,
                UITheme.TextPrimary, TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)_centreText.transform, 12f);
        }

        private void Issue(StaffCommand command)
        {
            SurgeryManager surgery = SurgeryManager.Active;
            if (surgery != null && surgery.Commands != null)
            {
                surgery.Commands.Issue(command);
            }

            Refresh();
        }

        public override void Show()
        {
            base.Show();
            if (GameManager.Exists)
            {
                GameManager.Instance.SetCursorLocked(false);
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (GameManager.Exists && GameManager.Instance.Phase == GamePhase.Surgery &&
                !GameManager.Instance.IsPaused)
            {
                GameManager.Instance.SetCursorLocked(true);
            }
        }

        public override void Refresh()
        {
            SurgeryManager surgery = SurgeryManager.Active;
            if (surgery == null || surgery.Commands == null)
            {
                _centreText.text = "No team available.";
                return;
            }

            int assistance = surgery.Commands.AssistanceRemaining;
            _centreText.text =
                "<b>COMMAND WHEEL</b>\n" +
                "<size=13><color=#9AACB6>Click an order or press its F-key.\n" +
                "C closes the wheel.</color></size>\n\n" +
                $"Assistance left: {(assistance < 0 ? "unlimited" : assistance.ToString())}\n" +
                $"Blood units: {surgery.Patient.BloodLoss.AvailableUnits}\n" +
                $"Sponges inside: {surgery.Sponges.SpongesInside}";
        }
    }
}
