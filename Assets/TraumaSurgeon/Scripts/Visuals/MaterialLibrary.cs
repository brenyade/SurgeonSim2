using System.Collections.Generic;
using UnityEngine;

namespace TraumaSurgeon.Visuals
{
    /// <summary>
    /// Creates and caches the handful of runtime materials the placeholder art uses.
    /// Works with both the Built-in pipeline and URP by probing for a usable shader and the
    /// matching colour property, so the project drops into either template unchanged.
    /// </summary>
    public static class MaterialLibrary
    {
        private static Shader _litShader;
        private static Shader _unlitShader;
        private static string _colorProperty;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Shader LitShader
        {
            get
            {
                if (_litShader == null)
                {
                    _litShader = Shader.Find("Universal Render Pipeline/Lit");
                    _colorProperty = "_BaseColor";

                    if (_litShader == null)
                    {
                        _litShader = Shader.Find("Standard");
                        _colorProperty = "_Color";
                    }

                    if (_litShader == null)
                    {
                        _litShader = Shader.Find("Legacy Shaders/Diffuse");
                        _colorProperty = "_Color";
                    }

                    if (_litShader == null)
                    {
                        _litShader = Shader.Find("Sprites/Default");
                        _colorProperty = "_Color";
                    }
                }

                return _litShader;
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                if (_unlitShader == null)
                {
                    _unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (_unlitShader == null)
                    {
                        _unlitShader = Shader.Find("Unlit/Color");
                    }

                    if (_unlitShader == null)
                    {
                        _unlitShader = LitShader;
                    }
                }

                return _unlitShader;
            }
        }

        public static string ColorProperty
        {
            get
            {
                if (string.IsNullOrEmpty(_colorProperty))
                {
                    _ = LitShader;
                }

                return _colorProperty;
            }
        }

        /// <summary>Sets colour on a material regardless of pipeline naming.</summary>
        public static void SetColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(ColorProperty))
            {
                material.SetColor(ColorProperty, color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
        }

        public static Color GetColor(Material material)
        {
            if (material == null)
            {
                return Color.white;
            }

            if (material.HasProperty(ColorProperty))
            {
                return material.GetColor(ColorProperty);
            }

            return Color.white;
        }

        /// <summary>Opaque material with optional metallic/smoothness, cached by key.</summary>
        public static Material Get(string key, Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            if (Cache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            var mat = new Material(LitShader) { name = "TS_" + key };
            SetColor(mat, color);
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", smoothness);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            Cache[key] = mat;
            return mat;
        }

        /// <summary>Transparent material (used for highlights, guide paths and drapes).</summary>
        public static Material GetTransparent(string key, Color color)
        {
            string cacheKey = "trans_" + key;
            if (Cache.TryGetValue(cacheKey, out Material cached) && cached != null)
            {
                return cached;
            }

            var mat = new Material(LitShader) { name = "TS_" + cacheKey };
            EnableTransparency(mat);
            SetColor(mat, color);
            Cache[cacheKey] = mat;
            return mat;
        }

        /// <summary>Emissive/unlit material used for monitors, guide lines and highlights.</summary>
        public static Material GetEmissive(string key, Color color)
        {
            string cacheKey = "emis_" + key;
            if (Cache.TryGetValue(cacheKey, out Material cached) && cached != null)
            {
                return cached;
            }

            var mat = new Material(UnlitShader) { name = "TS_" + cacheKey };
            SetColor(mat, color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2f);
            }

            Cache[cacheKey] = mat;
            return mat;
        }

        /// <summary>Creates a unique (non cached) instance so per-object tinting is possible.</summary>
        public static Material CreateInstance(Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            var mat = new Material(LitShader);
            SetColor(mat, color);
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", smoothness);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            return mat;
        }

        private static void EnableTransparency(Material mat)
        {
            // Built-in Standard shader transparency setup.
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f);
            }

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f); // URP transparent
            }

            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (mat.HasProperty("_DstBlend"))
            {
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetInt("_ZWrite", 0);
            }

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // ---- Named palette ----------------------------------------------------

        public static Material Skin => Get("skin", new Color(0.86f, 0.68f, 0.58f), 0f, 0.25f);
        public static Material Fat => Get("fat", new Color(0.95f, 0.86f, 0.55f), 0f, 0.2f);
        public static Material Muscle => Get("muscle", new Color(0.68f, 0.24f, 0.24f), 0f, 0.3f);
        public static Material Bone => Get("bone", new Color(0.93f, 0.91f, 0.84f), 0f, 0.15f);
        public static Material Organ => Get("organ", new Color(0.62f, 0.26f, 0.28f), 0f, 0.45f);
        public static Material Lung => Get("lung", new Color(0.80f, 0.55f, 0.58f), 0f, 0.3f);
        public static Material Heart => Get("heart", new Color(0.70f, 0.20f, 0.22f), 0f, 0.5f);
        public static Material Vessel => Get("vessel", new Color(0.55f, 0.13f, 0.16f), 0f, 0.5f);
        public static Material Intestine => Get("intestine", new Color(0.82f, 0.62f, 0.48f), 0f, 0.45f);
        public static Material Blood => Get("blood", new Color(0.45f, 0.05f, 0.07f), 0f, 0.6f);
        public static Material Steel => Get("steel", new Color(0.78f, 0.81f, 0.84f), 0.85f, 0.75f);
        public static Material DarkSteel => Get("darksteel", new Color(0.32f, 0.35f, 0.38f), 0.7f, 0.5f);
        public static Material Drape => Get("drape", new Color(0.24f, 0.45f, 0.55f), 0f, 0.2f);
        public static Material Floor => Get("floor", new Color(0.72f, 0.74f, 0.75f), 0f, 0.35f);
        public static Material Wall => Get("wall", new Color(0.80f, 0.84f, 0.86f), 0f, 0.1f);
        public static Material Plastic => Get("plastic", new Color(0.90f, 0.92f, 0.94f), 0f, 0.4f);
        public static Material Highlight => GetTransparent("highlight", new Color(0.2f, 1f, 0.6f, 0.35f));
        public static Material GuideLine => GetEmissive("guide", new Color(0.15f, 0.95f, 0.75f));
        public static Material Screen => GetEmissive("screen", new Color(0.05f, 0.12f, 0.10f));
    }
}
