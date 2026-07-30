using UnityEngine;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Single source of truth for UI colours, sizes and the runtime font.
    /// Clinical palette: deep slate panels, cyan accents, amber warnings, red criticals.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.055f, 0.075f, 0.094f, 0.96f);
        public static readonly Color Panel = new Color(0.094f, 0.125f, 0.153f, 0.96f);
        public static readonly Color PanelSoft = new Color(0.129f, 0.169f, 0.204f, 0.92f);
        public static readonly Color PanelHud = new Color(0.055f, 0.075f, 0.094f, 0.72f);
        public static readonly Color Accent = new Color(0.263f, 0.788f, 0.851f);
        public static readonly Color AccentDim = new Color(0.157f, 0.451f, 0.502f);
        public static readonly Color Success = new Color(0.353f, 0.831f, 0.549f);
        public static readonly Color Warning = new Color(0.973f, 0.741f, 0.235f);
        public static readonly Color Critical = new Color(0.937f, 0.353f, 0.353f);
        public static readonly Color TextPrimary = new Color(0.918f, 0.945f, 0.961f);
        public static readonly Color TextMuted = new Color(0.612f, 0.671f, 0.71f);
        public static readonly Color ButtonNormal = new Color(0.153f, 0.204f, 0.243f, 0.98f);
        public static readonly Color ButtonHover = new Color(0.212f, 0.290f, 0.337f, 1f);
        public static readonly Color ButtonPressed = new Color(0.263f, 0.788f, 0.851f, 0.9f);
        public static readonly Color Divider = new Color(1f, 1f, 1f, 0.09f);

        public const int FontTitle = 46;
        public const int FontHeading = 26;
        public const int FontSubheading = 20;
        public const int FontBody = 16;
        public const int FontSmall = 13;

        private static Font _font;

        /// <summary>
        /// Unity's built-in runtime font. Unity 6 ships "LegacyRuntime.ttf"; older versions used
        /// "Arial.ttf", so we probe both and fall back to any dynamic font in the project.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null)
                {
                    return _font;
                }

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                if (_font == null)
                {
                    Font[] all = Resources.FindObjectsOfTypeAll<Font>();
                    if (all != null && all.Length > 0)
                    {
                        _font = all[0];
                    }
                }

                return _font;
            }
        }

        /// <summary>Colour for a vital sign relative to its safe range.</summary>
        public static Color VitalColor(float value, float safeLow, float safeHigh,
            float criticalLow, float criticalHigh)
        {
            if (value <= criticalLow || value >= criticalHigh)
            {
                return Critical;
            }

            if (value < safeLow || value > safeHigh)
            {
                return Warning;
            }

            return Success;
        }

        public static Color RankColor(string rank)
        {
            switch (rank)
            {
                case "S": return new Color(1f, 0.84f, 0.33f);
                case "A": return Success;
                case "B": return Accent;
                case "C": return Warning;
                case "D": return new Color(0.9f, 0.5f, 0.3f);
                default: return Critical;
            }
        }
    }
}
