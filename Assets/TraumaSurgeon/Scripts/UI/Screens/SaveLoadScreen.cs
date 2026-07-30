using TraumaSurgeon.Career;
using TraumaSurgeon.Core;
using TraumaSurgeon.Save;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>Three-slot save/load menu, also used to create a new career.</summary>
    public class SaveLoadScreen : UIScreen
    {
        private RectTransform _slotContainer;
        private Text _title;
        private bool _newCareerMode;

        protected override void Build()
        {
            Image dim = RootBackground;
            dim.color = new Color(0.02f, 0.03f, 0.04f, 0.92f);

            RectTransform panel = UIFactory.CreateRect("Panel", Root);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.sizeDelta = new Vector2(880f, 620f);
            panel.gameObject.AddComponent<Image>().color = UITheme.Panel;

            VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.padding = new RectOffset(28, 28, 24, 24);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;

            _title = UIFactory.CreateText("Title", panel, "SAVE / LOAD", UITheme.FontHeading,
                UITheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetSize(_title.gameObject, 40f);

            Text path = UIFactory.CreateText("Path", panel,
                $"Saves are stored in: {SaveManager.RootFolder}", UITheme.FontSmall, UITheme.TextMuted);
            UIFactory.SetSize(path.gameObject, 22f);

            UIFactory.CreateDivider(panel);

            _slotContainer = UIFactory.CreateRect("Slots", panel);
            UIFactory.SetSize(_slotContainer.gameObject, 380f);
            VerticalLayoutGroup slots = _slotContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            slots.spacing = 10f;
            slots.childForceExpandHeight = false;
            slots.childControlHeight = true;
            slots.childControlWidth = true;

            Button close = UIFactory.CreateButton("Close", panel, "CLOSE",
                () => UIManager.Instance.CloseOverlay(this));
            UIFactory.SetSize(close.gameObject, 46f);
        }

        public void OpenForNewCareer()
        {
            _newCareerMode = true;
            UIManager.Instance.OpenOverlay(this);
        }

        public void OpenForLoad()
        {
            _newCareerMode = false;
            UIManager.Instance.OpenOverlay(this);
        }

        public override void Refresh()
        {
            _title.text = _newCareerMode ? "CHOOSE A CAREER SLOT" : "SAVE / LOAD";
            ClearChildren(_slotContainer);

            for (int i = 0; i < SaveManager.SlotCount; i++)
            {
                int slot = i;
                SaveData existing = SaveManager.Instance.PeekSlot(slot);

                RectTransform row = UIFactory.CreateRect("SlotRow" + i, _slotContainer);
                UIFactory.SetSize(row.gameObject, 108f);
                HorizontalLayoutGroup rowGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowGroup.spacing = 8f;
                rowGroup.childForceExpandHeight = true;
                rowGroup.childControlHeight = true;
                rowGroup.childControlWidth = true;

                string label;
                if (existing != null)
                {
                    CareerData c = existing.career;
                    label = $"<b>Slot {slot + 1}: {c.surgeonName}</b>\n" +
                            $"<size={UITheme.FontSmall}><color=#9AACB6>" +
                            $"{CareerManager.RankTitles[Mathf.Clamp(c.rank, 0, 6)]} · " +
                            $"XP {c.experience} · ${c.money:N0} · " +
                            $"{c.patientsTreated} cases · last played {existing.lastPlayed}</color></size>";
                }
                else
                {
                    label = $"<b>Slot {slot + 1}: empty</b>\n" +
                            $"<size={UITheme.FontSmall}><color=#9AACB6>Start a new career here</color></size>";
                }

                Button main = UIFactory.CreateButton("Slot" + slot, row.transform, label,
                    () => UseSlot(slot, existing != null), UITheme.FontBody);
                UIFactory.SetSize(main.gameObject, 100f, 100f, 480f, 620f);

                Button save = UIFactory.CreateButton("Save" + slot, row.transform, "SAVE HERE", () =>
                {
                    SaveManager.Instance.BindToSlot(slot);
                    SaveManager.Instance.SaveCurrent();
                    Refresh();
                });
                UIFactory.SetSize(save.gameObject, 100f, 100f, 130f, 150f);

                Button delete = UIFactory.CreateButton("Delete" + slot, row.transform, "DELETE", () =>
                {
                    SaveManager.Instance.DeleteSlot(slot);
                    Refresh();
                });
                UIFactory.SetSize(delete.gameObject, 100f, 100f, 110f, 130f);
                delete.GetComponent<Image>().color = new Color(0.35f, 0.14f, 0.14f, 0.95f);
            }
        }

        private void UseSlot(int slot, bool exists)
        {
            if (exists)
            {
                SaveManager.Instance.LoadSlot(slot);
            }
            else
            {
                SaveManager.Instance.CreateNewCareer(slot, "Dr. A. Vance");
            }

            if (CareerManager.Exists)
            {
                CareerManager.Instance.EnsureDefaultStaff();
            }

            SaveManager.Instance.SaveCurrent();
            UIManager.Instance.CloseOverlay(this);

            if (_newCareerMode)
            {
                GameManager.Instance.GoToHospital();
            }
            else
            {
                UIManager.Instance.MainMenu.Refresh();
            }
        }
    }
}
