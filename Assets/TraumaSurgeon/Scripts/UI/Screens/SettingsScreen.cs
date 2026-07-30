using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.InputSystem;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Settings overlay: mouse, audio, graphics presets and full key rebinding.
    /// Rebinding works by clicking an action then pressing the new key.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        private GameAction? _rebindTarget;
        private readonly Dictionary<GameAction, Text> _bindingLabels = new Dictionary<GameAction, Text>();
        private Text _sensitivityLabel;
        private Text _qualityLabel;
        private Text _rebindHint;

        protected override void Build()
        {
            Image dim = RootBackground;
            dim.color = new Color(0.02f, 0.03f, 0.04f, 0.92f);

            Text title = UIFactory.CreateText("Title", Root, "SETTINGS", UITheme.FontHeading,
                UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(140f, -100f), new Vector2(-140f, -46f));

            // Left: gameplay/audio/graphics.
            RectTransform left = MakeColumn("General", new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(140f, 100f), new Vector2(-10f, -120f));
            BuildGeneral(left);

            // Right: key bindings.
            RectTransform right = MakeColumn("Bindings", new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                new Vector2(10f, 100f), new Vector2(-140f, -120f));
            BuildBindings(right);

            Button close = UIFactory.CreateButton("Close", Root, "APPLY AND CLOSE", () =>
            {
                SettingsManager.Instance.ApplyAndSave();
                UIManager.Instance.CloseOverlay(this);
            }, UITheme.FontSubheading);

            RectTransform closeRect = (RectTransform)close.transform;
            UIFactory.Anchor(closeRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(0f, 30f));
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(340f, 52f);
        }

        private RectTransform MakeColumn(string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = UIFactory.CreateRect(name, Root);
            UIFactory.Anchor(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            rect.gameObject.AddComponent<Image>().color = UITheme.Panel;

            RectTransform content = UIFactory.CreateScrollView(name + "Scroll", rect, out _);
            UIFactory.Stretch((RectTransform)content.parent.parent);
            return content;
        }

        private void BuildGeneral(Transform parent)
        {
            SettingsData data = SettingsManager.Instance.Data;

            UIFactory.CreateHeader(parent, "MOUSE", "");
            _sensitivityLabel = UIFactory.CreateText("SensLabel", parent,
                $"Sensitivity: {data.mouseSensitivity:0.0}", UITheme.FontBody, UITheme.TextPrimary);
            UIFactory.SetSize(_sensitivityLabel.gameObject, 22f);

            UIFactory.CreateSlider("Sensitivity", parent, 0.4f, 8f, data.mouseSensitivity, v =>
            {
                data.mouseSensitivity = v;
                _sensitivityLabel.text = $"Sensitivity: {v:0.0}";
                if (InputManager.Exists)
                {
                    InputManager.Instance.MouseSensitivity = v;
                }
            });

            UIFactory.CreateToggle("InvertY", parent, "Invert vertical look", data.invertMouseY, v =>
            {
                data.invertMouseY = v;
                if (InputManager.Exists)
                {
                    InputManager.Instance.InvertY = v;
                }
            });

            UIFactory.CreateHeader(parent, "AUDIO", "");
            AddVolume(parent, "Master", data.masterVolume, v =>
            {
                data.masterVolume = v;
                AudioListener.volume = v;
            });
            AddVolume(parent, "Effects", data.sfxVolume, v => data.sfxVolume = v);
            AddVolume(parent, "Music", data.musicVolume, v => data.musicVolume = v);
            AddVolume(parent, "Ambience", data.ambienceVolume, v => data.ambienceVolume = v);

            UIFactory.CreateToggle("Subtitles", parent, "Staff dialogue subtitles", data.showSubtitles,
                v => data.showSubtitles = v);

            UIFactory.CreateHeader(parent, "GRAPHICS", "");
            _qualityLabel = UIFactory.CreateText("QualityLabel", parent,
                $"Preset: {SettingsManager.QualityNames[Mathf.Clamp(data.graphicsQuality, 0, 3)]}",
                UITheme.FontBody, UITheme.TextPrimary);
            UIFactory.SetSize(_qualityLabel.gameObject, 22f);

            HorizontalLayoutGroup row = UIFactory.CreateHorizontalGroup("QualityRow", parent, 6f);
            UIFactory.SetSize(row.gameObject, 42f);
            for (int i = 0; i < SettingsManager.QualityNames.Length; i++)
            {
                int level = i;
                Button button = UIFactory.CreateButton("Q" + i, row.transform,
                    SettingsManager.QualityNames[i], () =>
                    {
                        SettingsManager.Instance.ApplyGraphics(level);
                        _qualityLabel.text = $"Preset: {SettingsManager.QualityNames[level]}";
                    });
                UIFactory.SetSize(button.gameObject, 38f);
            }

            Text fovLabel = UIFactory.CreateText("FovLabel", parent,
                $"Field of view: {data.fieldOfView:0}", UITheme.FontBody, UITheme.TextPrimary);
            UIFactory.SetSize(fovLabel.gameObject, 22f);
            UIFactory.CreateSlider("Fov", parent, 55f, 95f, data.fieldOfView, v =>
            {
                data.fieldOfView = v;
                fovLabel.text = $"Field of view: {v:0}";
                SettingsManager.Instance.Apply();
            });

            UIFactory.CreateToggle("Dof", parent, "Depth-based camera focus", data.depthOfField,
                v => data.depthOfField = v);
            UIFactory.CreateToggle("Bob", parent, "Camera bob while walking", data.cameraBob,
                v => data.cameraBob = v);
            UIFactory.CreateToggle("Hud", parent, "Show HUD", data.showHud, v => data.showHud = v);
            UIFactory.CreateToggle("Tutorials", parent, "Show tutorial prompts", data.showTutorials,
                v => data.showTutorials = v);
        }

        private void AddVolume(Transform parent, string label, float value, System.Action<float> setter)
        {
            Text text = UIFactory.CreateText(label + "Label", parent, $"{label}: {value * 100f:0}%",
                UITheme.FontBody, UITheme.TextPrimary);
            UIFactory.SetSize(text.gameObject, 22f);

            UIFactory.CreateSlider(label, parent, 0f, 1f, value, v =>
            {
                setter(v);
                text.text = $"{label}: {v * 100f:0}%";
                SettingsManager.Instance.Apply();
            });
        }

        private void BuildBindings(Transform parent)
        {
            UIFactory.CreateHeader(parent, "KEY BINDINGS", "Click an action, then press the new key");

            _rebindHint = UIFactory.CreateText("RebindHint", parent, "", UITheme.FontSmall, UITheme.Warning);
            UIFactory.SetSize(_rebindHint.gameObject, 22f);

            _bindingLabels.Clear();
            foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
            {
                GameAction captured = action;
                HorizontalLayoutGroup row = UIFactory.CreateHorizontalGroup("Row_" + action, parent, 8f);
                UIFactory.SetSize(row.gameObject, 32f);

                Text label = UIFactory.CreateText("Name", row.transform, Prettify(action), UITheme.FontBody,
                    UITheme.TextMuted, TextAnchor.MiddleLeft);
                UIFactory.SetSize(label.gameObject, 30f, 30f, 200f, 260f);

                Button button = UIFactory.CreateButton("Key", row.transform, "", () => BeginRebind(captured));
                UIFactory.SetSize(button.gameObject, 30f, 30f, 110f, 140f);
                _bindingLabels[action] = button.GetComponentInChildren<Text>();
            }

            Button reset = UIFactory.CreateButton("ResetBindings", parent, "RESET TO DEFAULTS", () =>
            {
                InputManager.Instance.ResetToDefaults();
                SettingsManager.Instance.Data.bindings = InputManager.Instance.ExportBindings();
                RefreshBindingLabels();
            });
            UIFactory.SetSize(reset.gameObject, 42f);
        }

        private static string Prettify(GameAction action)
        {
            switch (action)
            {
                case GameAction.UseTool: return "Use tool";
                case GameAction.AltUseTool: return "Alternate function";
                case GameAction.DropTool: return "Return tool";
                case GameAction.SteadyHand: return "Steady hand";
                case GameAction.PatientChart: return "Patient chart";
                case GameAction.RequestAssistance: return "Request assistance";
                case GameAction.CommandWheel: return "Command wheel";
                case GameAction.ToggleImaging: return "Toggle imaging";
                case GameAction.NextToolPage: return "Next tool page";
                default: return action.ToString();
            }
        }

        private void BeginRebind(GameAction action)
        {
            _rebindTarget = action;
            _rebindHint.text = $"Press a key for <b>{Prettify(action)}</b>  (Esc cancels)";
        }

        private void Update()
        {
            if (!IsVisible || !_rebindTarget.HasValue || !InputManager.Exists)
            {
                return;
            }

            if (!UnityEngine.Input.anyKeyDown)
            {
                return;
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!UnityEngine.Input.GetKeyDown(key))
                {
                    continue;
                }

                if (key == KeyCode.Escape)
                {
                    _rebindTarget = null;
                    _rebindHint.text = "Rebinding cancelled.";
                    return;
                }

                InputManager.Instance.Rebind(_rebindTarget.Value, key);
                SettingsManager.Instance.Data.bindings = InputManager.Instance.ExportBindings();
                _rebindTarget = null;
                _rebindHint.text = "Binding updated.";
                RefreshBindingLabels();
                return;
            }
        }

        private void RefreshBindingLabels()
        {
            if (!InputManager.Exists)
            {
                return;
            }

            foreach (KeyValuePair<GameAction, Text> pair in _bindingLabels)
            {
                if (pair.Value != null)
                {
                    pair.Value.text = InputManager.Instance.GetKeyLabel(pair.Key);
                }
            }
        }

        public override void Refresh()
        {
            RefreshBindingLabels();
            _rebindTarget = null;
            if (_rebindHint != null)
            {
                _rebindHint.text = string.Empty;
            }
        }
    }
}
