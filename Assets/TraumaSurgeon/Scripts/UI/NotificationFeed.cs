using System.Collections.Generic;
using TraumaSurgeon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Bottom-left message log for notifications and staff dialogue. Entries fade out so the
    /// surgical field is never permanently obscured.
    /// </summary>
    public class NotificationFeed : MonoBehaviour
    {
        private const int MaxEntries = 6;
        private const float Lifetime = 6.5f;

        private class Entry
        {
            public Text Label;
            public float Age;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private RectTransform _container;

        public void Initialise(Transform canvas)
        {
            RectTransform root = UIFactory.CreateRect("NotificationFeed", canvas);
            UIFactory.Anchor(root, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 150f), new Vector2(24f, 150f));
            root.sizeDelta = new Vector2(620f, 240f);
            root.pivot = new Vector2(0f, 0f);

            VerticalLayoutGroup group = root.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 4f;
            group.childAlignment = TextAnchor.LowerLeft;
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandWidth = true;

            _container = root;
        }

        private void OnEnable()
        {
            GameEvents.Notification += OnNotification;
            GameEvents.StaffSpeech += OnStaffSpeech;
            GameEvents.ComplicationStarted += OnComplication;
        }

        private void OnDisable()
        {
            GameEvents.Notification -= OnNotification;
            GameEvents.StaffSpeech -= OnStaffSpeech;
            GameEvents.ComplicationStarted -= OnComplication;
        }

        private void OnNotification(string message, NotificationType type)
        {
            Color color;
            switch (type)
            {
                case NotificationType.Success: color = UITheme.Success; break;
                case NotificationType.Warning: color = UITheme.Warning; break;
                case NotificationType.Critical: color = UITheme.Critical; break;
                default: color = UITheme.TextPrimary; break;
            }

            Push(message, color);
        }

        private void OnStaffSpeech(StaffRole role, string line)
        {
            bool subtitles = !SettingsManager.Exists || SettingsManager.Instance.Data.showSubtitles;
            if (!subtitles)
            {
                return;
            }

            Push($"<b>{Pretty(role)}:</b> \"{line}\"", UITheme.Accent);
        }

        private void OnComplication(ComplicationType type, string message)
        {
            Push($"<b>! {message}</b>", UITheme.Critical);
        }

        private static string Pretty(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Anesthesiologist: return "Anaesthetist";
                case StaffRole.ScrubNurse: return "Scrub Nurse";
                case StaffRole.CirculatingNurse: return "Circulating Nurse";
                case StaffRole.SurgicalAssistant: return "Assistant";
                default: return "Consultant";
            }
        }

        public void Push(string message, Color color)
        {
            if (_container == null)
            {
                return;
            }

            Text label = UIFactory.CreateText("Entry", _container, message, UITheme.FontBody, color);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.SetSize(label.gameObject, 22f, 44f);

            var shadow = label.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);

            _entries.Add(new Entry { Label = label });

            while (_entries.Count > MaxEntries)
            {
                Entry oldest = _entries[0];
                _entries.RemoveAt(0);
                if (oldest.Label != null)
                {
                    Destroy(oldest.Label.gameObject);
                }
            }
        }

        private void Update()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                entry.Age += Time.unscaledDeltaTime;

                if (entry.Label == null)
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                if (entry.Age > Lifetime)
                {
                    Destroy(entry.Label.gameObject);
                    _entries.RemoveAt(i);
                }
                else if (entry.Age > Lifetime - 1.5f)
                {
                    Color c = entry.Label.color;
                    c.a = Mathf.Clamp01((Lifetime - entry.Age) / 1.5f);
                    entry.Label.color = c;
                }
            }
        }
    }
}
