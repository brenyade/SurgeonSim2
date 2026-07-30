using System.Text;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Pre-operative briefing: patient chart, imaging, planned steps and the tutorial note.
    /// This is where the player reviews the case before scrubbing in.
    /// </summary>
    public class BriefingScreen : UIScreen
    {
        private Text _title;
        private Text _chartText;
        private Text _planText;
        private Text _tutorialText;
        private Image _imaging;
        private Text _imagingCaption;

        protected override void Build()
        {
            _title = UIFactory.CreateText("Title", Root, "CASE BRIEFING", UITheme.FontTitle,
                UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor((RectTransform)_title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(90f, -110f), new Vector2(-90f, -40f));

            // Chart column.
            RectTransform chartPanel = MakePanel("ChartPanel",
                new Vector2(0f, 0f), new Vector2(0.36f, 1f),
                new Vector2(90f, 110f), new Vector2(-12f, -130f));
            UIFactory.CreateHeader(chartPanel, "PATIENT CHART", "Tab reopens this during surgery");
            _chartText = UIFactory.CreateText("ChartText", chartPanel, "", UITheme.FontBody,
                UITheme.TextPrimary);
            UIFactory.SetSize(_chartText.gameObject, 380f);

            // Imaging column.
            RectTransform imagingPanel = MakePanel("ImagingPanel",
                new Vector2(0.36f, 0f), new Vector2(0.66f, 1f),
                new Vector2(12f, 110f), new Vector2(-12f, -130f));
            UIFactory.CreateHeader(imagingPanel, "IMAGING", "");

            RectTransform imageHolder = UIFactory.CreateRect("ImageHolder", imagingPanel);
            UIFactory.SetSize(imageHolder.gameObject, 300f);
            _imaging = imageHolder.gameObject.AddComponent<Image>();
            _imaging.color = Color.white;
            _imaging.preserveAspect = true;

            _imagingCaption = UIFactory.CreateText("Caption", imagingPanel, "", UITheme.FontSmall,
                UITheme.TextMuted);
            UIFactory.SetSize(_imagingCaption.gameObject, 90f);

            // Plan column.
            RectTransform planPanel = MakePanel("PlanPanel",
                new Vector2(0.66f, 0f), new Vector2(1f, 1f),
                new Vector2(12f, 110f), new Vector2(-90f, -130f));
            UIFactory.CreateHeader(planPanel, "OPERATIVE PLAN", "");

            RectTransform planScroll = UIFactory.CreateRect("PlanScroll", planPanel);
            UIFactory.SetSize(planScroll.gameObject, 280f);
            RectTransform planContent = UIFactory.CreateScrollView("PlanList", planScroll, out _);
            UIFactory.Stretch((RectTransform)planContent.parent.parent);
            // No LayoutElement: the Text reports its own preferred height so the scroll view grows
            // with the operative plan and the instrument codex.
            _planText = UIFactory.CreateText("PlanText", planContent, "", UITheme.FontBody,
                UITheme.TextPrimary);

            _tutorialText = UIFactory.CreateText("Tutorial", planPanel, "", UITheme.FontSmall,
                UITheme.Warning);
            UIFactory.SetSize(_tutorialText.gameObject, 110f);

            // Footer buttons.
            Button start = UIFactory.CreateButton("Start", Root, "SCRUB IN AND BEGIN",
                () => GameManager.Instance.BeginSurgery(), UITheme.FontSubheading);
            RectTransform startRect = (RectTransform)start.transform;
            UIFactory.Anchor(startRect, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-90f, 36f), new Vector2(-90f, 36f));
            startRect.pivot = new Vector2(1f, 0f);
            startRect.sizeDelta = new Vector2(360f, 56f);

            Button back = UIFactory.CreateButton("Back", Root, "BACK",
                () => GameManager.Instance.GoToMainMenu(), UITheme.FontBody);
            RectTransform backRect = (RectTransform)back.transform;
            UIFactory.Anchor(backRect, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(90f, 36f), new Vector2(90f, 36f));
            backRect.pivot = Vector2.zero;
            backRect.sizeDelta = new Vector2(200f, 56f);
        }

        private RectTransform MakePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = UIFactory.CreateRect(name, Root);
            UIFactory.Anchor(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            rect.gameObject.AddComponent<Image>().color = UITheme.Panel;

            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 8f;
            group.padding = new RectOffset(18, 18, 18, 18);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;
            return rect;
        }

        public override void Refresh()
        {
            ProcedureData procedure = GameManager.Exists ? GameManager.Instance.PendingProcedure : null;
            if (procedure == null)
            {
                return;
            }

            PatientTemplateData p = procedure.patient;
            DifficultyProfile profile = DifficultyProfile.Get(GameManager.Instance.DifficultyLevel);

            _title.text = $"{procedure.displayName}   " +
                          $"<size={UITheme.FontSubheading}><color=#9AACB6>{profile.DisplayName}</color></size>";

            var chart = new StringBuilder();
            chart.AppendLine($"<b>{p.name}</b>");
            chart.AppendLine($"{p.age} years · {p.sex} · {p.weightKg:0} kg");
            chart.AppendLine($"Blood type <b>{p.bloodType}</b>");
            chart.AppendLine($"Allergies: {p.allergies}");
            chart.AppendLine();
            chart.AppendLine("<b>Presentation</b>");
            chart.AppendLine(p.presentation);
            chart.AppendLine();
            chart.AppendLine("<b>History</b>");
            chart.AppendLine(p.history);
            chart.AppendLine();
            chart.AppendLine("<b>Vitals on arrival</b>");
            chart.AppendLine($"HR {p.startHeartRate:0} bpm   BP {p.startSystolic:0}/{p.startDiastolic:0}");
            chart.AppendLine($"SpO2 {p.startSpO2:0}%   RR {p.startRespRate:0}   Temp {p.startTemperature:0.0} C");
            chart.AppendLine($"Estimated blood volume {p.startBloodVolumeMl:0} ml");
            if (p.internalBleedRate > 0f || p.externalBleedRate > 0f)
            {
                chart.AppendLine($"<color=#F05C5C>Active bleeding on arrival</color>");
            }

            _chartText.text = chart.ToString();

            _imaging.sprite = ImagingGenerator.GetSprite(p.imagingType, procedure.bodyRegion,
                procedure.id.GetHashCode());
            _imagingCaption.text = $"<b>{p.imagingType}</b> — {p.imagingCaption}";

            var plan = new StringBuilder();
            for (int i = 0; i < procedure.steps.Length; i++)
            {
                ProcedureStepData step = procedure.steps[i];
                plan.AppendLine($"<b>{i + 1}. {step.title}</b>{(step.optional ? "  (optional)" : "")}");
                plan.AppendLine($"<size={UITheme.FontSmall}><color=#9AACB6>{step.description}</color></size>");
                plan.AppendLine();
            }

            AppendInstrumentCodex(plan, procedure);
            _planText.text = plan.ToString();

            var tutorial = new StringBuilder();
            if (!string.IsNullOrEmpty(procedure.tutorial))
            {
                tutorial.AppendLine(procedure.tutorial);
            }

            if (procedure.failureConditions != null && procedure.failureConditions.Length > 0)
            {
                tutorial.AppendLine();
                tutorial.AppendLine("<b>Failure conditions:</b> " + string.Join("; ", procedure.failureConditions));
            }

            _tutorialText.text = tutorial.ToString();
        }

        /// <summary>
        /// Lists the instruments on the tray for this case with what each one does, what its
        /// alternate function is, and what happens if it is used on the wrong thing.
        /// </summary>
        private static void AppendInstrumentCodex(StringBuilder plan, ProcedureData procedure)
        {
            plan.AppendLine();
            plan.AppendLine($"<size={UITheme.FontSubheading}><b>INSTRUMENT TRAY</b></size>");
            plan.AppendLine();

            foreach (string toolId in procedure.requiredTools)
            {
                if (!System.Enum.TryParse(toolId, true, out ToolType type))
                {
                    continue;
                }

                ToolData tool = DataLibrary.GetTool(type);
                if (tool == null)
                {
                    continue;
                }

                plan.AppendLine($"<b>{tool.displayName}</b>  " +
                                $"<size={UITheme.FontSmall}><color=#9AACB6>{tool.category} · " +
                                $"{tool.length * 100f:0} cm</color></size>");
                plan.AppendLine($"<size={UITheme.FontSmall}><color=#9AACB6>{tool.description}</color></size>");
                plan.AppendLine($"<size={UITheme.FontSmall}><color=#43C9D9>Left click: {tool.Primary}" +
                                $"   ·   Right click: {tool.Secondary}</color></size>");
                plan.AppendLine($"<size={UITheme.FontSmall}><color=#F0B23C>Misuse: " +
                                $"{tool.misuseConsequence}</color></size>");
                plan.AppendLine();
            }
        }
    }
}
