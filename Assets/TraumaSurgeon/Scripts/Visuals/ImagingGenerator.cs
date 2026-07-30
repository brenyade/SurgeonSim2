using System.Collections.Generic;
using UnityEngine;

namespace TraumaSurgeon.Visuals
{
    /// <summary>
    /// Generates placeholder X-ray / CT / ultrasound textures procedurally so the imaging viewer
    /// and the wall display have something clinically plausible to show without shipping images.
    /// Replace with real DICOM-style artwork by loading a Texture2D from Resources instead.
    /// </summary>
    public static class ImagingGenerator
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string imagingType, string bodyRegion, int seed = 0)
        {
            string key = $"{imagingType}_{bodyRegion}_{seed}";
            if (Cache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Generate(imagingType, bodyRegion, seed);
            Cache[key] = texture;
            return texture;
        }

        private static Texture2D Generate(string imagingType, string bodyRegion, int seed)
        {
            const int width = 384;
            const int height = 288;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Imaging_{imagingType}_{bodyRegion}"
            };

            var rng = new System.Random(seed == 0 ? (imagingType + bodyRegion).GetHashCode() : seed);
            float offsetX = (float)rng.NextDouble() * 100f;
            float offsetY = (float)rng.NextDouble() * 100f;

            bool isCt = imagingType == "CT";
            bool isUltrasound = imagingType == "Ultrasound";

            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;
                    float ny = (float)y / height;

                    // Body silhouette: an ellipse fading at the edges.
                    float dx = (nx - 0.5f) / 0.36f;
                    float dy = (ny - 0.5f) / 0.42f;
                    float radial = dx * dx + dy * dy;
                    float body = Mathf.Clamp01(1.15f - radial);

                    // Tissue texture.
                    float noise = Mathf.PerlinNoise(offsetX + nx * 9f, offsetY + ny * 9f);
                    float fine = Mathf.PerlinNoise(offsetX + nx * 34f, offsetY + ny * 34f) * 0.3f;

                    float value = body * (0.42f + noise * 0.35f + fine);

                    // Region specific dense structures.
                    value += DenseStructure(bodyRegion, nx, ny) * body;

                    if (isUltrasound)
                    {
                        // Sector cone shape.
                        float cone = Mathf.Clamp01(1f - Mathf.Abs(nx - 0.5f) / Mathf.Max(0.05f, ny * 0.55f));
                        value *= cone * (0.5f + noise);
                    }

                    value = Mathf.Clamp01(value);

                    Color color = isCt
                        ? new Color(value, value, value)
                        : new Color(value * 0.86f, value * 0.92f, value);

                    if (isUltrasound)
                    {
                        color = new Color(value, value * 0.95f, value * 0.78f);
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Adds bright (dense) structures typical of the region being imaged.</summary>
        private static float DenseStructure(string bodyRegion, float nx, float ny)
        {
            switch (bodyRegion)
            {
                case "Chest":
                {
                    // Rib arcs plus a central mediastinal shadow.
                    float ribs = 0f;
                    for (int i = 0; i < 6; i++)
                    {
                        float ry = 0.22f + i * 0.10f;
                        ribs += Mathf.Exp(-Mathf.Pow((ny - ry) * 40f, 2f)) * 0.30f *
                                Mathf.Clamp01(1f - Mathf.Abs(nx - 0.5f) * 1.6f);
                    }

                    float spine = Mathf.Exp(-Mathf.Pow((nx - 0.5f) * 22f, 2f)) * 0.35f;
                    return ribs + spine;
                }

                case "Head":
                {
                    // Skull vault ring and a hyperdense collection on one side.
                    float r = Mathf.Sqrt(Mathf.Pow((nx - 0.5f) / 0.33f, 2f) + Mathf.Pow((ny - 0.5f) / 0.38f, 2f));
                    float vault = Mathf.Exp(-Mathf.Pow((r - 1f) * 12f, 2f)) * 0.55f;
                    float hematoma = Mathf.Exp(-(Mathf.Pow((nx - 0.29f) * 12f, 2f) +
                                                 Mathf.Pow((ny - 0.5f) * 7f, 2f))) * 0.45f;
                    return vault + hematoma;
                }

                case "Limb":
                {
                    // Long bone shaft with a fracture line.
                    float shaft = Mathf.Exp(-Mathf.Pow((nx - 0.5f) * 12f, 2f)) * 0.6f;
                    float fracture = Mathf.Abs(ny - 0.46f) < 0.012f ? -0.5f : 0f;
                    return shaft + fracture;
                }

                default:
                {
                    // Abdomen: spine, gas pockets, free fluid.
                    float spine = Mathf.Exp(-Mathf.Pow((nx - 0.5f) * 18f, 2f)) * 0.3f;
                    float gas = -Mathf.Exp(-(Mathf.Pow((nx - 0.62f) * 9f, 2f) +
                                             Mathf.Pow((ny - 0.55f) * 11f, 2f))) * 0.25f;
                    return spine + gas;
                }
            }
        }

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>Creates (and caches) a Sprite for use in uGUI Image components.</summary>
        public static Sprite GetSprite(string imagingType, string bodyRegion, int seed = 0)
        {
            string key = $"{imagingType}_{bodyRegion}_{seed}";
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Get(imagingType, bodyRegion, seed);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            SpriteCache[key] = sprite;
            return sprite;
        }
    }
}
