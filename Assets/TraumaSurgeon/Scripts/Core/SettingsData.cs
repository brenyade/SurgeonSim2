using System;
using System.Collections.Generic;

namespace TraumaSurgeon.Core
{
    [Serializable]
    public class KeyBindingEntry
    {
        public string action;
        public int keyCode;

        public KeyBindingEntry() { }

        public KeyBindingEntry(string action, int keyCode)
        {
            this.action = action;
            this.keyCode = keyCode;
        }
    }

    /// <summary>
    /// User configurable settings. Serialized to <c>settings.json</c> next to the save files.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        public float mouseSensitivity = 2.2f;
        public bool invertMouseY;
        public float masterVolume = 1f;
        public float sfxVolume = 0.9f;
        public float musicVolume = 0.5f;
        public float ambienceVolume = 0.6f;

        /// <summary>0 = Low, 1 = Medium, 2 = High, 3 = Ultra.</summary>
        public int graphicsQuality = 2;

        public float fieldOfView = 72f;
        public bool showHud = true;
        public bool showSubtitles = true;
        public bool depthOfField = true;
        public bool cameraBob = true;
        public bool showTutorials = true;

        public List<KeyBindingEntry> bindings = new List<KeyBindingEntry>();
    }
}
