using System;
using TraumaSurgeon.InputSystem;
using TraumaSurgeon.Save;
using UnityEngine;

namespace TraumaSurgeon.Core
{
    /// <summary>
    /// Owns <see cref="SettingsData"/>: loads it at boot, applies it to the engine and input
    /// layer, and writes it back to disk on change.
    /// </summary>
    public class SettingsManager : MonoSingleton<SettingsManager>
    {
        public static event Action<SettingsData> SettingsChanged;

        public SettingsData Data { get; private set; } = new SettingsData();

        public static readonly string[] QualityNames = { "Low", "Medium", "High", "Ultra" };

        protected override void OnSingletonAwake()
        {
            Data = SaveManager.LoadSettings();
            if (Data.bindings == null || Data.bindings.Count == 0)
            {
                if (InputManager.Exists)
                {
                    Data.bindings = InputManager.Instance.ExportBindings();
                }
            }

            Apply();
        }

        /// <summary>Pushes the current settings into the engine, input and audio systems.</summary>
        public void Apply()
        {
            if (InputManager.Exists)
            {
                InputManager.Instance.MouseSensitivity = Data.mouseSensitivity;
                InputManager.Instance.InvertY = Data.invertMouseY;
                InputManager.Instance.ApplyBindings(Data.bindings);
            }

            ApplyGraphics(Data.graphicsQuality);
            AudioListener.volume = Mathf.Clamp01(Data.masterVolume);
            SettingsChanged?.Invoke(Data);
        }

        public void Save()
        {
            if (InputManager.Exists)
            {
                Data.bindings = InputManager.Instance.ExportBindings();
            }

            SaveManager.SaveSettings(Data);
        }

        public void ApplyAndSave()
        {
            Apply();
            Save();
        }

        /// <summary>Maps the four presets onto Unity quality/shadow/particle knobs.</summary>
        public void ApplyGraphics(int level)
        {
            level = Mathf.Clamp(level, 0, 3);
            Data.graphicsQuality = level;

            // names.Length rather than QualitySettings.count so this compiles on 2021 LTS too.
            int unityLevels = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (unityLevels > 0)
            {
                int mapped = Mathf.Clamp(Mathf.RoundToInt(level / 3f * (unityLevels - 1)), 0, unityLevels - 1);
                QualitySettings.SetQualityLevel(mapped, true);
            }

            switch (level)
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowDistance = 12f;
                    QualitySettings.pixelLightCount = 1;
                    QualitySettings.antiAliasing = 0;
                    SetTextureMipmapLimit(1);
                    QualitySettings.softParticles = false;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowDistance = 25f;
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.antiAliasing = 2;
                    SetTextureMipmapLimit(0);
                    QualitySettings.softParticles = false;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 45f;
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.antiAliasing = 4;
                    SetTextureMipmapLimit(0);
                    QualitySettings.softParticles = true;
                    break;
                default:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 70f;
                    QualitySettings.pixelLightCount = 8;
                    QualitySettings.antiAliasing = 8;
                    SetTextureMipmapLimit(0);
                    QualitySettings.softParticles = true;
                    break;
            }

            QualitySettings.vSyncCount = level >= 2 ? 1 : 0;
        }

        /// <summary>
        /// Texture mipmap limit. The property was renamed in 2022.2 (masterTextureLimit ->
        /// globalTextureMipmapLimit), so this picks whichever one the running editor has.
        /// </summary>
        private static void SetTextureMipmapLimit(int limit)
        {
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = limit;
#else
            QualitySettings.masterTextureLimit = limit;
#endif
        }

        public string QualityName => QualityNames[Mathf.Clamp(Data.graphicsQuality, 0, 3)];
    }
}
