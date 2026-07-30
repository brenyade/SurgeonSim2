using System.Collections.Generic;
using System.Text;
using TraumaSurgeon.Core;
using TraumaSurgeon.InputSystem;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Player;
using TraumaSurgeon.Surgery;
using TraumaSurgeon.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// The in-surgery HUD: vitals monitor, objectives, instrument tray, hand stability, the
    /// interaction prompt and the laparoscope picture-in-picture. Everything is pinned to the
    /// screen edges so the centre of the view - the surgical field - stays clear.
    /// </summary>
    public class HudScreen : UIScreen
    {
        protected override bool Transparent => true;
        public override bool BlocksGameplay => false;

        private Text _vitalsText;
        private Text _rhythmText;
        private Text _objectiveTitle;
        private Text _objectiveList;
        private Text _hintText;
        private Text _promptText;
        private Text _toolText;
        private Text _statusText;
        private Text _timerText;
        private Image _stabilityFill;
        private Image _staminaFill;
        private Image _bloodFill;
        private Image _anesthesiaFill;
        private RawImage _scopeView;
        private RectTransform _crosshair;
        private RectTransform _toolBar;
        private readonly List<Text> _toolSlots = new List<Text>();

        protected override void Build()
        {
            BuildVitalsPanel();
            BuildObjectivePanel();
            BuildToolBar();
            BuildCentre();
            BuildScopeView();
        }

        private void BuildVitalsPanel()
        {
            RectTransform panel = UIFactory.CreateRect("VitalsPanel", Root);
            UIFactory.Anchor(panel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-360f, -300f), new Vector2(-24f, -24f));
            panel.gameObject.AddComponent<Image>().color = UITheme.PanelHud;

            VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 4f;
            group.padding = new RectOffset(14, 14, 12, 12);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;

            Text header = UIFactory.CreateText("Header", panel, "PATIENT MONITOR", UITheme.FontSmall,
                UITheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetSize(header.gameObject, 18f);

            _vitalsText = UIFactory.CreateText("Vitals", panel, "", UITheme.FontBody, UITheme.TextPrimary);
            UIFactory.SetSize(_vitalsText.gameObject, 130f);

            _rhythmText = UIFactory.CreateText("Rhythm", panel, "", UITheme.FontSmall, UITheme.Success);
            UIFactory.SetSize(_rhythmText.gameObject, 18f);

            Text bloodLabel = UIFactory.CreateText("BloodLabel", panel, "Blood volume", UITheme.FontSmall,
                UITheme.TextMuted);
            UIFactory.SetSize(bloodLabel.gameObject, 16f);
            _bloodFill = UIFactory.CreateBar("BloodBar", panel, UITheme.Critical, 12f);

            Text anesLabel = UIFactory.CreateText("AnesLabel", panel, "Anaesthetic depth", UITheme.FontSmall,
                UITheme.TextMuted);
            UIFactory.SetSize(anesLabel.gameObject, 16f);
            _anesthesiaFill = UIFactory.CreateBar("AnesBar", panel, UITheme.Accent, 12f);
        }

        private void BuildObjectivePanel()
        {
            RectTransform panel = UIFactory.CreateRect("ObjectivePanel", Root);
            UIFactory.Anchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -330f), new Vector2(24f, -24f));
            panel.sizeDelta = new Vector2(430f, 306f);
            panel.pivot = new Vector2(0f, 1f);
            panel.gameObject.AddComponent<Image>().color = UITheme.PanelHud;

            VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 4f;
            group.padding = new RectOffset(14, 14, 12, 12);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;

            _objectiveTitle = UIFactory.CreateText("ObjTitle", panel, "OBJECTIVES", UITheme.FontSmall,
                UITheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetSize(_objectiveTitle.gameObject, 18f);

            _objectiveList = UIFactory.CreateText("ObjList", panel, "", UITheme.FontSmall, UITheme.TextPrimary);
            UIFactory.SetSize(_objectiveList.gameObject, 170f);

            _hintText = UIFactory.CreateText("Hint", panel, "", UITheme.FontSmall, UITheme.Warning);
            UIFactory.SetSize(_hintText.gameObject, 54f);

            _timerText = UIFactory.CreateText("Timer", panel, "", UITheme.FontBody, UITheme.TextMuted);
            UIFactory.SetSize(_timerText.gameObject, 22f);
        }

        private void BuildToolBar()
        {
            _toolBar = UIFactory.CreateRect("ToolBar", Root);
            UIFactory.Anchor(_toolBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(0f, 24f));
            _toolBar.pivot = new Vector2(0.5f, 0f);
            _toolBar.sizeDelta = new Vector2(1180f, 62f);

            HorizontalLayoutGroup group = _toolBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 6f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childForceExpandWidth = true;
            group.childControlWidth = true;
            group.childControlHeight = true;

            for (int i = 0; i < ToolController.SlotsPerPage; i++)
            {
                Image slot = UIFactory.CreatePanel("Slot" + i, _toolBar, UITheme.PanelHud);
                UIFactory.SetSize(slot.gameObject, 58f, 58f, 100f, 130f);

                Text label = UIFactory.CreateText("Label", slot.transform, "", UITheme.FontSmall,
                    UITheme.TextMuted, TextAnchor.MiddleCenter);
                UIFactory.Stretch((RectTransform)label.transform, 4f);
                _toolSlots.Add(label);
            }

            _toolText = UIFactory.CreateText("EquippedTool", Root, "", UITheme.FontSubheading,
                UITheme.Accent, TextAnchor.LowerCenter, FontStyle.Bold);
            UIFactory.Anchor((RectTransform)_toolText.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-400f, 92f), new Vector2(400f, 124f));
        }

        private void BuildCentre()
        {
            // Crosshair - a small ring that does not obscure the field.
            _crosshair = UIFactory.CreateRect("Crosshair", Root);
            UIFactory.Anchor(_crosshair, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _crosshair.sizeDelta = new Vector2(10f, 10f);
            Image dot = _crosshair.gameObject.AddComponent<Image>();
            dot.color = new Color(1f, 1f, 1f, 0.55f);

            _promptText = UIFactory.CreateText("Prompt", Root, "", UITheme.FontBody, UITheme.TextPrimary,
                TextAnchor.MiddleCenter);
            UIFactory.Anchor((RectTransform)_promptText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300f, -84f), new Vector2(300f, -50f));

            _statusText = UIFactory.CreateText("Status", Root, "", UITheme.FontSmall, UITheme.TextMuted,
                TextAnchor.MiddleCenter);
            UIFactory.Anchor((RectTransform)_statusText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-320f, 44f), new Vector2(320f, 76f));

            // Hand stability + steady-hand stamina, bottom right.
            RectTransform handPanel = UIFactory.CreateRect("HandPanel", Root);
            UIFactory.Anchor(handPanel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-320f, 24f), new Vector2(-24f, 108f));
            handPanel.gameObject.AddComponent<Image>().color = UITheme.PanelHud;

            VerticalLayoutGroup group = handPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 3f;
            group.padding = new RectOffset(12, 12, 8, 8);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;

            Text steadyLabel = UIFactory.CreateText("SteadyLabel", handPanel, "HAND STABILITY",
                UITheme.FontSmall, UITheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.SetSize(steadyLabel.gameObject, 16f);
            _stabilityFill = UIFactory.CreateBar("StabilityBar", handPanel, UITheme.Success, 12f);

            Text staminaLabel = UIFactory.CreateText("StaminaLabel", handPanel, "STEADY-HAND STAMINA (Shift)",
                UITheme.FontSmall, UITheme.TextMuted);
            UIFactory.SetSize(staminaLabel.gameObject, 16f);
            _staminaFill = UIFactory.CreateBar("StaminaBar", handPanel, UITheme.Accent, 12f);
        }

        private void BuildScopeView()
        {
            RectTransform holder = UIFactory.CreateRect("ScopeView", Root);
            UIFactory.Anchor(holder, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-404f, -140f), new Vector2(-24f, 140f));
            holder.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            RectTransform view = UIFactory.CreateRect("Feed", holder);
            UIFactory.Stretch(view, 6f);
            _scopeView = view.gameObject.AddComponent<RawImage>();
            _scopeView.color = Color.white;

            holder.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            SurgeryManager surgery = SurgeryManager.Active;
            if (surgery == null || surgery.Patient == null)
            {
                return;
            }

            UpdateVitals(surgery.Patient);
            UpdateObjectives(surgery);
            UpdateTools();
            UpdateHand();
            UpdatePrompt();
            UpdateScope(surgery);
            UpdateHudVisibility();
        }

        private void UpdateVitals(PatientController patient)
        {
            VitalSigns v = patient.Vitals;

            Color hrColor = UITheme.VitalColor(v.heartRate, 55f, 110f, 40f, 150f);
            Color mapColor = UITheme.VitalColor(v.MeanArterialPressure, 65f, 110f, 55f, 130f);
            Color spo2Color = UITheme.VitalColor(v.oxygenSaturation, 94f, 100f, 88f, 101f);
            Color tempColor = UITheme.VitalColor(v.temperature, 36f, 37.6f, 34.5f, 39f);

            var sb = new StringBuilder();
            sb.AppendLine($"HR    <color=#{ColorUtility.ToHtmlStringRGB(hrColor)}><b>{v.heartRate:0}</b></color> bpm");
            sb.AppendLine($"BP    <color=#{ColorUtility.ToHtmlStringRGB(mapColor)}><b>{v.BloodPressureText}</b></color>" +
                          $"  (MAP {v.MeanArterialPressure:0})");
            sb.AppendLine($"SpO2  <color=#{ColorUtility.ToHtmlStringRGB(spo2Color)}><b>{v.oxygenSaturation:0}%</b></color>");
            sb.AppendLine($"RR    {v.respiratoryRate:0}/min    Temp <color=#{ColorUtility.ToHtmlStringRGB(tempColor)}>{v.temperature:0.0}C</color>");
            sb.Append($"Blood {v.bloodVolumeMl:0} ml  ·  Loss {v.BloodLostMl:0} ml");
            _vitalsText.text = sb.ToString();

            bool arrest = v.InCardiacArrest;
            _rhythmText.color = arrest ? UITheme.Critical : UITheme.Success;
            _rhythmText.text = $"Rhythm: {Prettify(v.rhythm)}  ·  {patient.Anesthesia.StatusText}" +
                               (patient.PneumothoraxSeverity > 0.1f
                                   ? $"  ·  <color=#F0B23C>Pneumothorax {patient.PneumothoraxSeverity * 100f:0}%</color>"
                                   : string.Empty);

            _bloodFill.fillAmount = v.BloodFraction;
            _bloodFill.color = v.BloodFraction > 0.7f ? UITheme.Success
                : v.BloodFraction > 0.5f ? UITheme.Warning : UITheme.Critical;

            _anesthesiaFill.fillAmount = Mathf.Clamp01(v.anesthesiaDepth / 100f);
            _anesthesiaFill.color = patient.Anesthesia.IsTooDeep ? UITheme.Critical
                : patient.Anesthesia.IsTooLight ? UITheme.Warning : UITheme.Accent;
        }

        private static string Prettify(CardiacRhythm rhythm)
        {
            switch (rhythm)
            {
                case CardiacRhythm.NormalSinus: return "Normal sinus";
                case CardiacRhythm.VentricularFibrillation: return "VENTRICULAR FIBRILLATION";
                case CardiacRhythm.VentricularTachycardia: return "VENTRICULAR TACHYCARDIA";
                case CardiacRhythm.PEA: return "PULSELESS ELECTRICAL ACTIVITY";
                case CardiacRhythm.Asystole: return "ASYSTOLE";
                default: return rhythm.ToString();
            }
        }

        private void UpdateObjectives(SurgeryManager surgery)
        {
            ObjectiveManager objectives = surgery.Objectives;
            if (objectives == null)
            {
                return;
            }

            _objectiveTitle.text = $"OBJECTIVES  ({objectives.CompletedCount}/{objectives.Steps.Count})";

            var sb = new StringBuilder();
            int shown = 0;
            for (int i = 0; i < objectives.Steps.Count && shown < 7; i++)
            {
                ObjectiveState state = objectives.Steps[i];

                // Show completed steps immediately before the pointer plus the upcoming ones.
                if (state.Complete && i < objectives.CurrentIndex - 1)
                {
                    continue;
                }

                shown++;
                string prefix = state.Complete ? "<color=#5AD48C>[x]</color>" :
                    i == objectives.CurrentIndex ? "<color=#43C9D9>[>]</color>" : "<color=#6E7A82>[ ]</color>";

                string progress = state.Data.repetitions > 1 && !state.Complete
                    ? $"  ({state.Progress}/{state.Data.repetitions})"
                    : string.Empty;

                string colour = state.Complete ? "#6E7A82" : i == objectives.CurrentIndex ? "#EAF1F5" : "#9AACB6";
                sb.AppendLine($"{prefix} <color={colour}>{state.Data.title}{progress}</color>");
            }

            _objectiveList.text = sb.ToString();

            bool tutorialsOn = !SettingsManager.Exists || SettingsManager.Instance.Data.showTutorials;
            string hint = objectives.CurrentHint(surgery.Profile.ShowHints && tutorialsOn);
            ToolType required = objectives.CurrentRequiredTool();
            if (!string.IsNullOrEmpty(hint))
            {
                _hintText.text = required != ToolType.None
                    ? $"{hint}\n<b>Instrument:</b> {ToolController.NameOf(required)}"
                    : hint;
            }
            else
            {
                _hintText.text = string.Empty;
            }

            if (surgery.TimeRemaining >= 0f)
            {
                int minutes = Mathf.FloorToInt(surgery.TimeRemaining / 60f);
                int seconds = Mathf.FloorToInt(surgery.TimeRemaining % 60f);
                _timerText.color = surgery.TimeRemaining < 60f ? UITheme.Critical : UITheme.TextMuted;
                _timerText.text = $"Time remaining {minutes:00}:{seconds:00}";
            }
            else if (surgery.Scoring != null)
            {
                float elapsed = surgery.Scoring.ElapsedSeconds;
                _timerText.color = UITheme.TextMuted;
                _timerText.text = $"Elapsed {Mathf.FloorToInt(elapsed / 60f):00}:{Mathf.FloorToInt(elapsed % 60f):00}" +
                                  $"  ·  par {surgery.Procedure.parTimeSeconds / 60} min";
            }
        }

        private void UpdateTools()
        {
            ToolController tools = GameManager.Instance.PlayerRig != null
                ? GameManager.Instance.PlayerRig.Tools
                : null;

            if (tools == null)
            {
                return;
            }

            List<ToolType> page = tools.CurrentPageTools();
            for (int i = 0; i < _toolSlots.Count; i++)
            {
                Text label = _toolSlots[i];
                Image bg = label.transform.parent.GetComponent<Image>();

                if (i < page.Count)
                {
                    bool equipped = page[i] == tools.EquippedType;
                    label.text = $"<b>{i + 1}</b>\n<size={UITheme.FontSmall - 2}>{ToolController.NameOf(page[i])}</size>";
                    label.color = equipped ? UITheme.TextPrimary : UITheme.TextMuted;
                    bg.color = equipped ? UITheme.AccentDim : UITheme.PanelHud;
                }
                else
                {
                    label.text = string.Empty;
                    bg.color = new Color(0f, 0f, 0f, 0.25f);
                }
            }

            SurgicalToolBase equippedTool = tools.Equipped;
            if (equippedTool != null)
            {
                string extra = string.Empty;
                if (equippedTool is Syringe syringe)
                {
                    MedicationDefinition def = MedicationSystem.Get(syringe.SelectedDrug);
                    extra = $"  —  {def.DisplayName} {syringe.Dose:0.##} {def.Unit} " +
                            $"<size={UITheme.FontSmall}>(wheel adjusts, RMB changes drug)</size>";
                }

                string pageInfo = tools.PageCount > 1 ? $"  [page {tools.Page + 1}/{tools.PageCount} - 0 cycles]" : "";
                _toolText.text = equippedTool.DisplayName + extra + pageInfo;
            }
            else
            {
                _toolText.text = string.Empty;
            }
        }

        private void UpdateHand()
        {
            PlayerRigBuilder.Rig rig = GameManager.Instance.PlayerRig;
            if (rig == null || rig.Hand == null)
            {
                return;
            }

            _stabilityFill.fillAmount = rig.Hand.Stability;
            _stabilityFill.color = rig.Hand.Stability > 0.7f ? UITheme.Success
                : rig.Hand.Stability > 0.45f ? UITheme.Warning : UITheme.Critical;

            _staminaFill.fillAmount = rig.Hand.Stamina / 100f;

            // Crosshair grows as the hand gets shakier.
            float size = Mathf.Lerp(26f, 8f, rig.Hand.Stability);
            _crosshair.sizeDelta = new Vector2(size, size);

            AnatomyPartLabel(rig);
        }

        private void AnatomyPartLabel(PlayerRigBuilder.Rig rig)
        {
            var hovered = rig.Hand.HoveredPart;
            if (hovered != null)
            {
                _statusText.text = hovered.StatusLine() +
                                   (hovered.HasUncontrolledBleeding ? "  <color=#F05C5C>· bleeding</color>" : "");
            }
            else
            {
                _statusText.text = string.Empty;
            }
        }

        private void UpdatePrompt()
        {
            PlayerRigBuilder.Rig rig = GameManager.Instance.PlayerRig;
            if (rig == null || rig.Interactor == null)
            {
                return;
            }

            string prompt = rig.Interactor.CurrentPrompt;
            if (!string.IsNullOrEmpty(prompt) && InputManager.Exists)
            {
                _promptText.text = $"[{InputManager.Instance.GetKeyLabel(GameAction.Interact)}]  {prompt}";
            }
            else
            {
                _promptText.text = string.Empty;
            }
        }

        private void UpdateScope(SurgeryManager surgery)
        {
            bool active = surgery.Laparoscopy != null && surgery.Laparoscopy.IsActive;
            GameObject holder = _scopeView.transform.parent.gameObject;

            if (holder.activeSelf != active)
            {
                holder.SetActive(active);
            }

            if (active)
            {
                _scopeView.texture = surgery.Laparoscopy.ScopeTexture;
            }
        }

        private void UpdateHudVisibility()
        {
            bool show = !SettingsManager.Exists || SettingsManager.Instance.Data.showHud;
            if (Root.GetComponent<CanvasGroup>() == null)
            {
                Root.gameObject.AddComponent<CanvasGroup>();
            }

            Root.GetComponent<CanvasGroup>().alpha = show ? 1f : 0f;
        }
    }
}
