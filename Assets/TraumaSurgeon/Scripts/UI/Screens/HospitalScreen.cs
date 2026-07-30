using System.Text;
using TraumaSurgeon.Career;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Save;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// The between-operations hub: schedule, upgrades, staff, research, outcomes and incidents.
    /// Tabs keep it compact - management is deliberately secondary to the surgery itself.
    /// </summary>
    public class HospitalScreen : UIScreen
    {
        private enum Tab
        {
            Schedule,
            Equipment,
            Staff,
            Research,
            Outcomes,
            Incidents
        }

        private Tab _tab = Tab.Schedule;
        private RectTransform _content;
        private Text _statusBar;

        protected override void Build()
        {
            Text title = UIFactory.CreateText("Title", Root, "ST. AUGUSTINE TRAUMA CENTRE", UITheme.FontTitle,
                UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(90f, -104f), new Vector2(-90f, -40f));

            _statusBar = UIFactory.CreateText("Status", Root, "", UITheme.FontBody, UITheme.TextMuted,
                TextAnchor.MiddleLeft);
            UIFactory.Anchor((RectTransform)_statusBar.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(92f, -140f), new Vector2(-90f, -104f));

            // Tabs.
            RectTransform tabRow = UIFactory.CreateRect("Tabs", Root);
            UIFactory.Anchor(tabRow, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(90f, -196f), new Vector2(-90f, -148f));

            HorizontalLayoutGroup tabGroup = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabGroup.spacing = 6f;
            tabGroup.childForceExpandWidth = true;
            tabGroup.childControlWidth = true;
            tabGroup.childControlHeight = true;

            foreach (Tab tab in System.Enum.GetValues(typeof(Tab)))
            {
                Tab captured = tab;
                Button button = UIFactory.CreateButton("Tab_" + tab, tabRow, tab.ToString().ToUpper(), () =>
                {
                    _tab = captured;
                    Refresh();
                });
                UIFactory.SetSize(button.gameObject, 44f);
            }

            RectTransform contentRoot = UIFactory.CreateRect("ContentRoot", Root);
            UIFactory.Anchor(contentRoot, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(90f, 100f), new Vector2(-90f, -204f));
            contentRoot.gameObject.AddComponent<Image>().color = UITheme.Panel;
            _content = UIFactory.CreateScrollView("Content", contentRoot, out _);
            UIFactory.Stretch((RectTransform)_content.parent.parent);

            Button menu = UIFactory.CreateButton("Menu", Root, "MAIN MENU",
                () => GameManager.Instance.GoToMainMenu());
            RectTransform menuRect = (RectTransform)menu.transform;
            UIFactory.Anchor(menuRect, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(90f, 30f), new Vector2(90f, 30f));
            menuRect.pivot = Vector2.zero;
            menuRect.sizeDelta = new Vector2(240f, 52f);

            Button save = UIFactory.CreateButton("Save", Root, "SAVE PROGRESS", () =>
            {
                if (SaveManager.Instance.CurrentSlot < 0)
                {
                    UIManager.Instance.SaveLoad.OpenForLoad();
                }
                else
                {
                    SaveManager.Instance.SaveCurrent();
                }
            });
            RectTransform saveRect = (RectTransform)save.transform;
            UIFactory.Anchor(saveRect, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-90f, 30f), new Vector2(-90f, 30f));
            saveRect.pivot = new Vector2(1f, 0f);
            saveRect.sizeDelta = new Vector2(280f, 52f);
        }

        public override void Refresh()
        {
            if (!CareerManager.Exists || !HospitalManager.Exists)
            {
                return;
            }

            CareerData data = CareerManager.Instance.Data;
            _statusBar.text =
                $"<b>{data.surgeonName}</b> · {CareerManager.Instance.RankTitle} · " +
                $"XP {data.experience} (next rank in {CareerManager.Instance.ExperienceToNextRank}) · " +
                $"<color=#5AD48C>${data.money:N0}</color> · Reputation {data.reputation:0} · " +
                $"Malpractice {data.malpracticeRisk:0}% · Rating {data.hospitalRating:0.0}/5 · Shift {data.shiftNumber}";

            ClearChildren(_content);

            switch (_tab)
            {
                case Tab.Schedule: BuildSchedule(); break;
                case Tab.Equipment: BuildUpgrades("Equipment"); break;
                case Tab.Staff: BuildStaff(); break;
                case Tab.Research: BuildUpgrades("Research"); break;
                case Tab.Outcomes: BuildOutcomes(); break;
                case Tab.Incidents: BuildIncidents(); break;
            }
        }

        private void BuildSchedule()
        {
            UIFactory.CreateHeader(_content, "SURGICAL SCHEDULE",
                "Accept a case to take it to theatre, or decline it at a small reputation cost.");

            if (HospitalManager.Instance.Schedule.Count == 0)
            {
                HospitalManager.Instance.GenerateSchedule();
            }

            foreach (ScheduledCase entry in new System.Collections.Generic.List<ScheduledCase>(
                         HospitalManager.Instance.Schedule))
            {
                ScheduledCase captured = entry;

                RectTransform row = UIFactory.CreateRect("Case", _content);
                UIFactory.SetSize(row.gameObject, 96f);
                row.gameObject.AddComponent<Image>().color = UITheme.PanelSoft;

                HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                group.spacing = 8f;
                group.padding = new RectOffset(10, 10, 8, 8);
                group.childForceExpandHeight = true;
                group.childControlHeight = true;
                group.childControlWidth = true;

                string risk = entry.HighRisk
                    ? "<color=#F05C5C>HIGH RISK</color>"
                    : "<color=#5AD48C>ROUTINE</color>";

                Text info = UIFactory.CreateText("Info", row,
                    $"<b>{entry.Procedure.displayName}</b>  {risk}\n" +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>{entry.PatientName} · " +
                    $"{DifficultyProfile.Get(entry.Difficulty).DisplayName} · " +
                    $"payout ${entry.Procedure.payout * entry.PayoutMultiplier:N0}\n{entry.Note}</color></size>",
                    UITheme.FontBody, UITheme.TextPrimary, TextAnchor.MiddleLeft);
                UIFactory.SetSize(info.gameObject, 80f, 80f, 600f, 900f);

                Button accept = UIFactory.CreateButton("Accept", row, "ACCEPT", () =>
                {
                    captured.Procedure.patient.name = captured.PatientName;
                    GameManager.Instance.SelectCase(captured.Procedure, captured.Difficulty,
                        GameModeType.Career, null, captured);
                });
                UIFactory.SetSize(accept.gameObject, 80f, 80f, 150f, 170f);

                Button decline = UIFactory.CreateButton("Decline", row, "DECLINE", () =>
                {
                    HospitalManager.Instance.RejectCase(captured);
                    Refresh();
                });
                UIFactory.SetSize(decline.gameObject, 80f, 80f, 140f, 160f);
            }

            Button regenerate = UIFactory.CreateButton("NewShift", _content, "START NEXT SHIFT (new case list)",
                () =>
                {
                    HospitalManager.Instance.GenerateSchedule();
                    Refresh();
                });
            UIFactory.SetSize(regenerate.gameObject, 46f);
        }

        private void BuildUpgrades(string category)
        {
            UIFactory.CreateHeader(_content, category.ToUpper(),
                category == "Research"
                    ? "Research unlocks passive bonuses that apply to every future operation."
                    : "Better equipment reduces malfunctions and gives you more room to work.");

            foreach (UpgradeData upgrade in HospitalManager.Instance.UpgradesByCategory(category))
            {
                UpgradeData captured = upgrade;
                bool owned = HospitalManager.Instance.IsPurchased(upgrade.id);
                bool affordable = HospitalManager.Instance.CanAfford(upgrade);

                RectTransform row = UIFactory.CreateRect("Upgrade", _content);
                UIFactory.SetSize(row.gameObject, 84f);
                row.gameObject.AddComponent<Image>().color = UITheme.PanelSoft;

                HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                group.spacing = 8f;
                group.padding = new RectOffset(10, 10, 8, 8);
                group.childForceExpandHeight = true;
                group.childControlHeight = true;
                group.childControlWidth = true;

                Text info = UIFactory.CreateText("Info", row,
                    $"<b>{upgrade.displayName}</b>{(owned ? "  <color=#5AD48C>OWNED</color>" : "")}\n" +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>{upgrade.description}\n" +
                    $"Effect: {upgrade.effect}</color></size>",
                    UITheme.FontBody, UITheme.TextPrimary, TextAnchor.MiddleLeft);
                UIFactory.SetSize(info.gameObject, 68f, 68f, 620f, 900f);

                if (!owned)
                {
                    Button buy = UIFactory.CreateButton("Buy", row, $"${upgrade.cost:N0}", () =>
                    {
                        if (HospitalManager.Instance.Purchase(captured.id))
                        {
                            Refresh();
                        }
                    });
                    UIFactory.SetSize(buy.gameObject, 68f, 68f, 170f, 190f);
                    if (!affordable)
                    {
                        buy.GetComponent<Image>().color = new Color(0.22f, 0.14f, 0.14f, 0.9f);
                    }
                }
            }
        }

        private void BuildStaff()
        {
            UIFactory.CreateHeader(_content, "OPERATING ROOM TEAM",
                "Higher skill means faster, more reliable responses to your commands.");

            foreach (StaffRole role in System.Enum.GetValues(typeof(StaffRole)))
            {
                StaffRole captured = role;
                StaffRecord record = CareerManager.Instance.GetStaff(role);
                if (record == null)
                {
                    continue;
                }

                float trainCost = 1500f + record.skill * 800f;

                RectTransform row = UIFactory.CreateRect("Staff", _content);
                UIFactory.SetSize(row.gameObject, 76f);
                row.gameObject.AddComponent<Image>().color = UITheme.PanelSoft;

                HorizontalLayoutGroup group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                group.spacing = 8f;
                group.padding = new RectOffset(10, 10, 8, 8);
                group.childForceExpandHeight = true;
                group.childControlHeight = true;
                group.childControlWidth = true;

                Text info = UIFactory.CreateText("Info", row,
                    $"<b>{record.name}</b> — {role}\n" +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>Skill {record.skill}/10 · " +
                    $"salary ${record.salary:N0}/shift · trainings {record.trainingLevel}</color></size>",
                    UITheme.FontBody, UITheme.TextPrimary, TextAnchor.MiddleLeft);
                UIFactory.SetSize(info.gameObject, 60f, 60f, 560f, 900f);

                Button train = UIFactory.CreateButton("Train", row, $"TRAIN\n${trainCost:N0}", () =>
                {
                    if (CareerManager.Instance.TrainStaff(captured, trainCost))
                    {
                        Refresh();
                    }
                    else
                    {
                        GameEvents.RaiseNotification("Cannot train: insufficient funds or maximum skill.",
                            NotificationType.Warning);
                    }
                }, UITheme.FontSmall);
                UIFactory.SetSize(train.gameObject, 60f, 60f, 160f, 180f);

                Button hire = UIFactory.CreateButton("Hire", row, "HIRE SPECIALIST\n$9,000", () =>
                {
                    if (CareerManager.Instance.HireStaff(captured, "Dr. " + RandomName(), 8, 9000f, 3400f))
                    {
                        Refresh();
                    }
                    else
                    {
                        GameEvents.RaiseNotification("Not enough funds to hire.", NotificationType.Warning);
                    }
                }, UITheme.FontSmall);
                UIFactory.SetSize(hire.gameObject, 60f, 60f, 190f, 210f);
            }
        }

        private static string RandomName()
        {
            string[] names = { "Aoki", "Bergman", "Castellanos", "Duarte", "Eze", "Fournier", "Grigoryan" };
            return names[Random.Range(0, names.Length)];
        }

        private void BuildOutcomes()
        {
            UIFactory.CreateHeader(_content, "PATIENT OUTCOMES", "Your last 40 cases.");

            CareerData data = CareerManager.Instance.Data;
            if (data.caseHistory.Count == 0)
            {
                UIFactory.CreateText("Empty", _content, "No completed cases yet.", UITheme.FontBody,
                    UITheme.TextMuted);
                return;
            }

            var sb = new StringBuilder();
            foreach (CaseRecord record in data.caseHistory)
            {
                ProcedureData procedure = DataLibrary.GetProcedure(record.procedureId);
                string name = procedure != null ? procedure.displayName : record.procedureId;
                string color = ColorUtility.ToHtmlStringRGB(UITheme.RankColor(record.rank));
                sb.AppendLine(
                    $"<color=#{color}><b>{record.rank}</b></color>  {name} — {record.patientName}  " +
                    $"<size={UITheme.FontSmall}><color=#9AACB6>{record.score} pts · " +
                    $"{(record.survived ? "survived" : "died")} · {record.bloodLossMl:0} ml · " +
                    $"{record.complications} complications · {record.timestamp}</color></size>");
            }

            Text text = UIFactory.CreateText("History", _content, sb.ToString(), UITheme.FontBody,
                UITheme.TextPrimary);
            UIFactory.SetSize(text.gameObject, 40f, data.caseHistory.Count * 26f + 40f);
        }

        private void BuildIncidents()
        {
            UIFactory.CreateHeader(_content, "INCIDENT REPORTS",
                "Filed automatically by the quality and safety committee.");

            CareerData data = CareerManager.Instance.Data;
            if (data.incidentReports.Count == 0)
            {
                UIFactory.CreateText("Empty", _content, "No incidents on file. Keep it that way.",
                    UITheme.FontBody, UITheme.Success);
                return;
            }

            foreach (IncidentReport incident in data.incidentReports)
            {
                RectTransform row = UIFactory.CreateRect("Incident", _content);
                UIFactory.SetSize(row.gameObject, 72f);
                row.gameObject.AddComponent<Image>().color = UITheme.PanelSoft;

                VerticalLayoutGroup group = row.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = 2f;
                group.padding = new RectOffset(12, 12, 8, 8);
                group.childForceExpandHeight = false;
                group.childControlHeight = true;
                group.childControlWidth = true;

                Color severity = incident.severity == "Severe" ? UITheme.Critical
                    : incident.severity == "Moderate" ? UITheme.Warning : UITheme.TextMuted;

                Text head = UIFactory.CreateText("Head", row,
                    $"<b>{incident.title}</b>  <color=#{ColorUtility.ToHtmlStringRGB(severity)}>" +
                    $"{incident.severity}</color>", UITheme.FontBody, UITheme.TextPrimary);
                UIFactory.SetSize(head.gameObject, 22f);

                Text body = UIFactory.CreateText("Body", row, incident.body, UITheme.FontSmall,
                    UITheme.TextMuted);
                UIFactory.SetSize(body.gameObject, 36f);
            }
        }
    }
}
