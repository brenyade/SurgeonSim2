using UnityEngine;

namespace TraumaSurgeon.Visuals
{
    /// <summary>Colour scheme for a theatre figure.</summary>
    public struct HumanoidStyle
    {
        public Color Gown;
        public Color Scrubs;
        public Color Cap;
        public Color Skin;
        public Color Glove;
        public float Height;

        public static HumanoidStyle Default(Color gown)
        {
            return new HumanoidStyle
            {
                Gown = gown,
                Scrubs = gown * 0.85f,
                Cap = gown * 0.7f,
                Skin = new Color(0.80f, 0.63f, 0.53f),
                Glove = new Color(0.44f, 0.70f, 0.78f),
                Height = 1.75f
            };
        }
    }

    /// <summary>
    /// Bones a figure exposes for animation. Anything the animation code wants to move is here so
    /// it never has to search the hierarchy by name.
    /// </summary>
    public class FigureRig
    {
        public Transform Root;
        public Transform Hips;
        public Transform Chest;
        public Transform Head;
        public Transform ShoulderL;
        public Transform ShoulderR;
        public Transform ForearmL;
        public Transform ForearmR;

        /// <summary>True when this rig came from a supplied model rather than primitives.</summary>
        public bool IsExternalModel;
    }

    /// <summary>
    /// Builds a stylised human figure for the operating room team.
    ///
    /// Proportions are real (roughly a 1.75 m adult: head one seventh of height, shoulders at 82%,
    /// elbows at the waist) and the limbs are a proper joint chain, so the figures read as people
    /// rather than as stacked capsules and can be animated at the joints.
    ///
    /// REPLACING WITH REAL MODELS
    /// Drop a rigged humanoid prefab at
    ///     Assets/TraumaSurgeon/Resources/TraumaSurgeonModels/Staff_&lt;Role&gt;.prefab
    /// (or Staff_Default.prefab as a fallback for every role) and it is instantiated instead of
    /// this primitive figure - no code change. See README "Replacing the placeholder art".
    /// </summary>
    public static class HumanoidFigureBuilder
    {
        public const string ModelResourceFolder = "TraumaSurgeonModels/";

        /// <summary>
        /// Instantiates a supplied prefab if one exists for this name, otherwise builds the
        /// primitive stand-in.
        /// </summary>
        public static FigureRig Build(Transform parent, string modelName, HumanoidStyle style)
        {
            FigureRig external = TryBuildFromPrefab(parent, modelName);
            if (external != null)
            {
                return external;
            }

            return BuildPrimitiveFigure(parent, style);
        }

        private static FigureRig TryBuildFromPrefab(Transform parent, string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return null;
            }

            GameObject prefab = Resources.Load<GameObject>(ModelResourceFolder + modelName);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(ModelResourceFolder + "Staff_Default");
            }

            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, parent);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // A supplied model brings its own rig; we only need the root to animate it crudely if
            // it has no Animator of its own.
            return new FigureRig
            {
                Root = instance.transform,
                Hips = instance.transform,
                Chest = instance.transform,
                IsExternalModel = true
            };
        }

        private static FigureRig BuildPrimitiveFigure(Transform parent, HumanoidStyle style)
        {
            float h = style.Height <= 0f ? 1.75f : style.Height;

            Material gown = MaterialLibrary.CreateInstance(style.Gown, 0f, 0.18f);
            Material scrubs = MaterialLibrary.CreateInstance(style.Scrubs, 0f, 0.18f);
            Material cap = MaterialLibrary.CreateInstance(style.Cap, 0f, 0.15f);
            Material skin = MaterialLibrary.CreateInstance(style.Skin, 0f, 0.25f);
            Material glove = MaterialLibrary.CreateInstance(style.Glove, 0f, 0.35f);
            Material mask = MaterialLibrary.Get("surgical_mask", new Color(0.88f, 0.91f, 0.93f));

            var rig = new FigureRig();
            rig.Root = PrimitiveFactory.Empty("Figure", parent).transform;

            // ---- Legs (hidden below the gown, but they give the silhouette weight) ----
            for (int side = -1; side <= 1; side += 2)
            {
                float x = 0.095f * side;

                PrimitiveFactory.Create(PrimitiveType.Capsule, "Thigh", rig.Root,
                    new Vector3(x, h * 0.41f, 0f), new Vector3(0.135f, h * 0.115f, 0.135f),
                    scrubs, false);

                PrimitiveFactory.Create(PrimitiveType.Capsule, "Shin", rig.Root,
                    new Vector3(x, h * 0.16f, 0f), new Vector3(0.115f, h * 0.115f, 0.115f),
                    scrubs, false);

                PrimitiveFactory.Create(PrimitiveType.Cube, "Shoe", rig.Root,
                    new Vector3(x, 0.03f, 0.035f), new Vector3(0.11f, 0.06f, 0.24f),
                    MaterialLibrary.Get("theatre_clog", new Color(0.92f, 0.93f, 0.94f)), false);
            }

            // ---- Hips and torso ----
            rig.Hips = PrimitiveFactory.Empty("Hips", rig.Root, new Vector3(0f, h * 0.53f, 0f)).transform;

            PrimitiveFactory.Create(PrimitiveType.Capsule, "Pelvis", rig.Hips,
                new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f),
                new Vector3(0.19f, 0.11f, 0.16f), gown, false);

            // Chest is its own pivot so breathing and the bow into compressions rotate correctly.
            rig.Chest = PrimitiveFactory.Empty("Chest", rig.Hips, new Vector3(0f, h * 0.06f, 0f)).transform;

            PrimitiveFactory.Create(PrimitiveType.Capsule, "Abdomen", rig.Chest,
                new Vector3(0f, 0f, 0f), new Vector3(0.30f, 0.10f, 0.21f), gown, false);

            PrimitiveFactory.Create(PrimitiveType.Capsule, "Ribcage", rig.Chest,
                new Vector3(0f, h * 0.09f, 0f), new Vector3(0.36f, 0.12f, 0.23f), gown, false);

            // Sterile gown front, slightly proud of the chest.
            PrimitiveFactory.Create(PrimitiveType.Cube, "GownFront", rig.Chest,
                new Vector3(0f, h * 0.045f, 0.115f), new Vector3(0.30f, h * 0.20f, 0.03f), gown, false);

            // ---- Shoulders and arms, angled forward over the table ----
            for (int side = -1; side <= 1; side += 2)
            {
                bool left = side < 0;
                float x = 0.185f * side;

                Transform shoulder = PrimitiveFactory.Empty(left ? "ShoulderL" : "ShoulderR",
                    rig.Chest, new Vector3(x, h * 0.115f, 0f)).transform;

                PrimitiveFactory.Create(PrimitiveType.Sphere, "Deltoid", shoulder,
                    Vector3.zero, new Vector3(0.135f, 0.135f, 0.135f), gown, false);

                // Upper arm hangs down and slightly forward.
                shoulder.localRotation = Quaternion.Euler(28f, 0f, -12f * side);

                PrimitiveFactory.Create(PrimitiveType.Capsule, "UpperArm", shoulder,
                    new Vector3(0f, -h * 0.085f, 0f), new Vector3(0.105f, h * 0.075f, 0.105f),
                    gown, false);

                Transform forearm = PrimitiveFactory.Empty(left ? "ForearmL" : "ForearmR",
                    shoulder, new Vector3(0f, -h * 0.16f, 0f)).transform;

                // Elbow bent so the hands sit over the operating field.
                forearm.localRotation = Quaternion.Euler(-62f, 0f, 0f);

                PrimitiveFactory.Create(PrimitiveType.Capsule, "Forearm", forearm,
                    new Vector3(0f, -h * 0.07f, 0f), new Vector3(0.092f, h * 0.065f, 0.092f),
                    gown, false);

                PrimitiveFactory.Create(PrimitiveType.Sphere, "Hand", forearm,
                    new Vector3(0f, -h * 0.145f, 0f), new Vector3(0.085f, 0.10f, 0.06f),
                    glove, false);

                if (left)
                {
                    rig.ShoulderL = shoulder;
                    rig.ForearmL = forearm;
                }
                else
                {
                    rig.ShoulderR = shoulder;
                    rig.ForearmR = forearm;
                }
            }

            // ---- Neck and head ----
            PrimitiveFactory.Create(PrimitiveType.Capsule, "Neck", rig.Chest,
                new Vector3(0f, h * 0.155f, 0f), new Vector3(0.075f, 0.045f, 0.075f), skin, false);

            rig.Head = PrimitiveFactory.Empty("Head", rig.Chest, new Vector3(0f, h * 0.215f, 0f)).transform;

            PrimitiveFactory.Create(PrimitiveType.Sphere, "Skull", rig.Head,
                Vector3.zero, new Vector3(0.175f, 0.215f, 0.195f), skin, false);

            // Scrub cap covering the upper half of the head.
            PrimitiveFactory.Create(PrimitiveType.Sphere, "Cap", rig.Head,
                new Vector3(0f, 0.035f, 0f), new Vector3(0.186f, 0.19f, 0.204f), cap, false);

            // Surgical mask across the lower face.
            PrimitiveFactory.Create(PrimitiveType.Cube, "Mask", rig.Head,
                new Vector3(0f, -0.045f, 0.055f), new Vector3(0.155f, 0.085f, 0.075f), mask, false);

            // Eyes, just enough to give the figure a facing direction.
            Material eye = MaterialLibrary.Get("eye", new Color(0.14f, 0.15f, 0.17f));
            PrimitiveFactory.Create(PrimitiveType.Sphere, "EyeL", rig.Head,
                new Vector3(-0.042f, 0.022f, 0.086f), new Vector3(0.032f, 0.022f, 0.018f), eye, false);
            PrimitiveFactory.Create(PrimitiveType.Sphere, "EyeR", rig.Head,
                new Vector3(0.042f, 0.022f, 0.086f), new Vector3(0.032f, 0.022f, 0.018f), eye, false);

            return rig;
        }
    }
}
