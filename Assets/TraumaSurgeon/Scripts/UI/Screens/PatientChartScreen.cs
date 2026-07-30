using System.Text;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Surgery;
using TraumaSurgeon.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Tab overlay during surgery: live chart, imaging viewer, problem list and drug record.
    /// </summary>
    public class PatientChartScreen : UIScreen
    {
        private Text _chartText;
        private Text _problemText;
        private Text _drugText;
        private Image _imaging;
        private Text _imagingCaption;

        protected override void Build()
        {
            Image dim = RootBackground;
            dim.color = new Color(0.02f, 0.03f, 0.04f, 0.88f);

            Text title = UIFactory.CreateText("Title", Root, "PATIENT CHART", UITheme.FontHeading,
                UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(120f, -110f), new Vector2(-120f, -50f));

            Text close = UIFactory.CreateText("CloseHint", Root, "Tab to close", UITheme.FontBody,
                UITheme.TextMuted, TextAnchor.MiddleRight);
            UIFactory.Anchor((RectTransform)close.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(120f, -110f), new Vector2(-120f, -50f));

            RectTransform left = MakePanel("ChartPanel", new Vector2(0f, 0f), new Vector2(0.4f, 1f),
                new Vector2(120f, 90f), new Vector2(-10f, -130f));
            UIFactory.CreateHeader(left, "DEMOGRAPHICS & VITALS", "");
            _chartText = UIFactory.CreateText("Chart", left, "", UITheme.FontBody, UITheme.TextPrimary);
            UIFactory.SetSize(_chartText.gameObject, 420f);

            RectTransform middle = MakePanel("ImagingPanel", new Vector2(0.4f, 0f), new Vector2(0.7f, 1f),
                new Vector2(10f, 90f), new Vector2(-10f, -130f));
            UIFactory.CreateHeader(middle, "IMAGING", "");
            RectTransform holder = UIFactory.CreateRect("ImageHolder", middle);
            UIFactory.SetSize(holder.gameObject, 320f);
            _imaging = holder.gameObject.AddComponent<Image>();
            _imaging.preserveAspect = true;
            _imagingCaption = UIFactory.CreateText("Caption", middle, "", UITheme.FontSmall, UITheme.TextMuted);
            UIFactory.SetSize(_imagingCaption.gameObject, 90f);

            RectTransform right = MakePanel("StatusPanel", new Vector2(0.7f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, 90f), new Vector2(-120f, -130f));
            UIFactory.CreateHeader(right, "PROBLEM LIST", "");
            _problemText = UIFactory.CreateText("Problems", right, "", UITheme.FontSmall, UITheme.TextPrimary);
            UIFactory.SetSize(_problemText.gameObject, 260f);

            UIFactory.CreateHeader(right, "DRUG RECORD", "");
            _drugText = UIFactory.CreateText("Drugs", right, "", UITheme.FontSmall, UITheme.TextMuted);
            UIFactory.SetSize(_drugText.gameObject, 160f);
        }

        private RectTransform MakePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = UIFactory.CreateRect(name, Root);
            UIFactory.Anchor(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            rect.gameObject.AddComponent<Image>().color = UITheme.Panel;

            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 6f;
            group.padding = new RectOffset(16, 16, 16, 16);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;
            return rect;
        }

        public override void Show()
        {
            base.Show();

            // Opening the chart satisfies the "review the chart" objective.
            if (SurgeryManager.Active != null)
            {
                GameEvents.RaiseActionPerformed(SurgicalActionType.ReviewChart, null, 1f);
            }
        }

        public override void Refresh()
        {
            SurgeryManager surgery = SurgeryManager.Active;
            if (surgery == null || surgery.Patient == null)
            {
                _chartText.text = "No patient on the table.";
                return;
            }

            PatientController patient = surgery.Patient;
            PatientTemplateData template = patient.Template;
            VitalSigns v = patient.Vitals;

            var sb = new StringBuilder();
            sb.AppendLine($"<b>{template.name}</b>   {template.age}y   {template.sex}");
            sb.AppendLine($"Weight {template.weightKg:0} kg   Blood type {template.bloodType}");
            sb.AppendLine($"Allergies: {template.allergies}");
            sb.AppendLine();
            sb.AppendLine("<b>Live vitals</b>");
            sb.AppendLine($"Heart rate          {v.heartRate:0} bpm");
            sb.AppendLine($"Blood pressure      {v.BloodPressureText} (MAP {v.MeanArterialPressure:0})");
            sb.AppendLine($"Oxygen saturation   {v.oxygenSaturation:0}%");
            sb.AppendLine($"Respiratory rate    {v.respiratoryRate:0}/min");
            sb.AppendLine($"Temperature         {v.temperature:0.0} C");
            sb.AppendLine($"Blood volume        {v.bloodVolumeMl:0} / {v.maxBloodVolumeMl:0} ml");
            sb.AppendLine($"Estimated loss      {v.BloodLostMl:0} ml (shock class {v.ShockClass})");
            sb.AppendLine($"Consciousness       {v.consciousness}");
            sb.AppendLine($"Pain score          {v.painLevel:0}/100");
            sb.AppendLine($"Anaesthetic depth   {v.anesthesiaDepth:0}/100 ({patient.Anesthesia.StatusText})");
            sb.AppendLine($"Infection risk      {v.infectionRisk:0}%");
            sb.AppendLine($"Cardiac rhythm      {v.rhythm}");
            sb.AppendLine();
            sb.AppendLine("<b>Theatre</b>");
            sb.AppendLine($"Pooled blood in field  {patient.BloodLoss.PooledBloodMl:0} ml");
            sb.AppendLine($"Units transfused       {patient.BloodLoss.UnitsTransfused} " +
                          $"({patient.BloodLoss.AvailableUnits} remaining)");
            sb.AppendLine($"Items inside patient   {patient.RetainedItemCount}");
            _chartText.text = sb.ToString();

            _imaging.sprite = ImagingGenerator.GetSprite(template.imagingType, surgery.Procedure.bodyRegion,
                surgery.Procedure.id.GetHashCode());
            _imagingCaption.text = $"<b>{template.imagingType}</b> — {template.imagingCaption}";

            var problems = new StringBuilder();
            foreach (AnatomyPart part in patient.Body.AllParts)
            {
                if (part.state == DamageState.Healthy && !part.HasUncontrolledBleeding)
                {
                    continue;
                }

                string colour = part.isRepaired || part.isRemoved ? "#5AD48C"
                    : part.HasUncontrolledBleeding ? "#F05C5C" : "#F0B23C";
                problems.AppendLine($"<color={colour}>• {part.StatusLine()}" +
                                    $"{(part.HasUncontrolledBleeding ? " — bleeding" : "")}</color>");
            }

            if (patient.PneumothoraxSeverity > 0.05f)
            {
                problems.AppendLine($"<color=#F05C5C>• Pneumothorax {patient.PneumothoraxSeverity * 100f:0}%</color>");
            }

            _problemText.text = problems.Length == 0 ? "No active problems." : problems.ToString();

            var drugs = new StringBuilder();
            foreach (string entry in patient.Medications.AdministeredLog)
            {
                drugs.AppendLine("• " + entry);
            }

            _drugText.text = drugs.Length == 0 ? "No drugs administered." : drugs.ToString();
        }
    }
}
