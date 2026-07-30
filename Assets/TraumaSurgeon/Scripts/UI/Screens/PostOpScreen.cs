using System.Text;
using TraumaSurgeon.Core;
using TraumaSurgeon.Scoring;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Post-operation report: rank, scored breakdown, raw metrics and the incident notes.
    /// </summary>
    public class PostOpScreen : UIScreen
    {
        private Text _rankText;
        private Text _summaryText;
        private Text _headlineText;
        private RectTransform _breakdown;
        private Text _notesText;
        private Text _metricsText;

        protected override void Build()
        {
            Text title = UIFactory.CreateText("Title", Root, "POST-OPERATION REPORT", UITheme.FontTitle,
                UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(90f, -110f), new Vector2(-90f, -40f));

            // Rank block.
            RectTransform rankPanel = UIFactory.CreateRect("RankPanel", Root);
            UIFactory.Anchor(rankPanel, new Vector2(0f, 0f), new Vector2(0.26f, 1f),
                new Vector2(90f, 110f), new Vector2(-10f, -130f));
            rankPanel.gameObject.AddComponent<Image>().color = UITheme.Panel;

            VerticalLayoutGroup rankGroup = rankPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            rankGroup.spacing = 8f;
            rankGroup.padding = new RectOffset(18, 18, 24, 18);
            rankGroup.childForceExpandHeight = false;
            rankGroup.childControlHeight = true;
            rankGroup.childControlWidth = true;

            _rankText = UIFactory.CreateText("Rank", rankPanel, "B", 150, UITheme.Accent,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.SetSize(_rankText.gameObject, 170f);

            _headlineText = UIFactory.CreateText("Headline", rankPanel, "", UITheme.FontSubheading,
                UITheme.TextPrimary, TextAnchor.MiddleCenter);
            UIFactory.SetSize(_headlineText.gameObject, 70f);

            _summaryText = UIFactory.CreateText("Summary", rankPanel, "", UITheme.FontSmall,
                UITheme.TextMuted, TextAnchor.UpperCenter);
            UIFactory.SetSize(_summaryText.gameObject, 110f);

            UIFactory.CreateDivider(rankPanel);

            _metricsText = UIFactory.CreateText("Metrics", rankPanel, "", UITheme.FontSmall,
                UITheme.TextPrimary);
            UIFactory.SetSize(_metricsText.gameObject, 260f);

            // Score breakdown.
            RectTransform breakdownPanel = UIFactory.CreateRect("BreakdownPanel", Root);
            UIFactory.Anchor(breakdownPanel, new Vector2(0.26f, 0f), new Vector2(0.68f, 1f),
                new Vector2(10f, 110f), new Vector2(-10f, -130f));
            breakdownPanel.gameObject.AddComponent<Image>().color = UITheme.Panel;
            _breakdown = UIFactory.CreateScrollView("BreakdownScroll", breakdownPanel, out _);
            UIFactory.Stretch((RectTransform)_breakdown.parent.parent);

            // Notes.
            RectTransform notesPanel = UIFactory.CreateRect("NotesPanel", Root);
            UIFactory.Anchor(notesPanel, new Vector2(0.68f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, 110f), new Vector2(-90f, -130f));
            notesPanel.gameObject.AddComponent<Image>().color = UITheme.Panel;

            VerticalLayoutGroup notesGroup = notesPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            notesGroup.spacing = 6f;
            notesGroup.padding = new RectOffset(18, 18, 18, 18);
            notesGroup.childForceExpandHeight = false;
            notesGroup.childControlHeight = true;
            notesGroup.childControlWidth = true;

            UIFactory.CreateHeader(notesPanel, "OPERATIVE NOTES", "");
            _notesText = UIFactory.CreateText("Notes", notesPanel, "", UITheme.FontSmall, UITheme.TextPrimary);
            UIFactory.SetSize(_notesText.gameObject, 480f);

            // Buttons.
            Button retry = UIFactory.CreateButton("Retry", Root, "RETRY CASE",
                () => GameManager.Instance.RetryCase(), UITheme.FontSubheading);
            RectTransform retryRect = (RectTransform)retry.transform;
            UIFactory.Anchor(retryRect, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(90f, 34f), new Vector2(90f, 34f));
            retryRect.pivot = Vector2.zero;
            retryRect.sizeDelta = new Vector2(280f, 56f);

            Button continueButton = UIFactory.CreateButton("Continue", Root, "CONTINUE",
                () => GameManager.Instance.DismissReport(), UITheme.FontSubheading);
            RectTransform continueRect = (RectTransform)continueButton.transform;
            UIFactory.Anchor(continueRect, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-90f, 34f), new Vector2(-90f, 34f));
            continueRect.pivot = new Vector2(1f, 0f);
            continueRect.sizeDelta = new Vector2(320f, 56f);
        }

        public override void Refresh()
        {
            SurgeryReport report = GameManager.Exists ? GameManager.Instance.LastReport : null;
            if (report == null)
            {
                return;
            }

            _rankText.text = report.rank;
            _rankText.color = UITheme.RankColor(report.rank);

            _headlineText.text = $"{report.totalScore} / {report.maxScore}\n" +
                                 $"<size={UITheme.FontSmall}>{report.Percentage * 100f:0}%</size>";
            _summaryText.text = $"<b>{report.procedureName}</b>\n{report.patientName}\n" +
                                $"{report.difficulty}\n\n{report.summary}";

            var metrics = new StringBuilder();
            metrics.AppendLine("<b>METRICS</b>");
            metrics.AppendLine($"Outcome        {(report.patientSurvived ? "Survived" : "Died")}");
            metrics.AppendLine($"Stability      {report.finalStability * 100f:0}%");
            metrics.AppendLine($"Blood loss     {report.bloodLossMl:0} ml");
            metrics.AppendLine($"Transfused     {report.unitsTransfused} units");
            metrics.AppendLine($"Duration       {report.DurationText}");
            metrics.AppendLine($"Steps          {report.stepsCompleted}/{report.stepsTotal}");
            metrics.AppendLine($"Accuracy       {report.accuracyAverage * 100f:0}%");
            metrics.AppendLine($"Tissue damage  {report.tissueDamage:0}");
            metrics.AppendLine($"Wrong tools    {report.wrongToolUses}");
            metrics.AppendLine($"Extra actions  {report.unnecessaryActions}");
            metrics.AppendLine($"Complications  {report.complicationsResolved}/{report.complicationsTriggered}");
            metrics.AppendLine($"Drug accuracy  {report.correctDoses} correct / {report.incorrectDoses} wrong");
            metrics.AppendLine($"Infection risk {report.infectionRisk:0}%");
            metrics.AppendLine($"Retained items {report.retainedItems}");
            _metricsText.text = metrics.ToString();

            ClearChildren(_breakdown);
            UIFactory.CreateHeader(_breakdown, "SCORE BREAKDOWN", "");

            foreach (ScoreLine line in report.lines)
            {
                RectTransform row = UIFactory.CreateRect("Line_" + line.label, _breakdown);
                UIFactory.SetSize(row.gameObject, 52f);
                row.gameObject.AddComponent<Image>().color = UITheme.PanelSoft;

                VerticalLayoutGroup group = row.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = 2f;
                group.padding = new RectOffset(12, 12, 6, 6);
                group.childForceExpandHeight = false;
                group.childControlHeight = true;
                group.childControlWidth = true;

                float ratio = line.maxPoints > 0 ? Mathf.Clamp01((float)line.points / line.maxPoints) : 0f;
                Color color = line.points < 0 ? UITheme.Critical
                    : ratio > 0.8f ? UITheme.Success
                    : ratio > 0.5f ? UITheme.Warning : UITheme.Critical;

                Text label = UIFactory.CreateText("Label", row,
                    $"<b>{line.label}</b>   <color=#{ColorUtility.ToHtmlStringRGB(color)}>" +
                    $"{line.points}/{line.maxPoints}</color>" +
                    $"   <size={UITheme.FontSmall}><color=#9AACB6>{line.detail}</color></size>",
                    UITheme.FontBody, UITheme.TextPrimary);
                UIFactory.SetSize(label.gameObject, 22f);

                Image bar = UIFactory.CreateBar("Bar", row, color, 8f);
                bar.fillAmount = ratio;
            }

            var notes = new StringBuilder();
            if (report.notes.Count == 0)
            {
                notes.AppendLine("Uneventful procedure. No incidents recorded.");
            }
            else
            {
                foreach (string note in report.notes)
                {
                    notes.AppendLine("• " + note);
                }
            }

            _notesText.text = notes.ToString();
        }
    }
}
