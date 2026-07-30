using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Builds placeholder instrument models from primitives and attaches the matching behaviour
    /// component. Each model points along +Z with a "Tip" transform at the working end.
    ///
    /// Replacing the art later is a one-line change per tool: instantiate a prefab instead of
    /// calling <see cref="Build"/>, then add the same behaviour component.
    /// </summary>
    public static class ToolFactory
    {
        public static SurgicalToolBase Create(ToolType type, Transform parent)
        {
            GameObject root = new GameObject("Tool_" + type);
            root.transform.SetParent(parent, false);

            ToolData data = DataLibrary.GetTool(type);
            Color color = data != null ? data.Color : new Color(0.78f, 0.81f, 0.84f);
            Material metal = MaterialLibrary.CreateInstance(color, 0.8f, 0.7f);
            Material grip = MaterialLibrary.Get("toolgrip", new Color(0.16f, 0.18f, 0.22f), 0.1f, 0.3f);

            Transform tip = Build(type, root.transform, metal, grip);
            SurgicalToolBase tool = AttachBehaviour(type, root);
            tool.Type = type;
            tool.Tip = tip;

            // Instruments are visual only - physics contacts are resolved by the hand raycast.
            PrimitiveFactory.StripColliders(root);
            return tool;
        }

        private static SurgicalToolBase AttachBehaviour(ToolType type, GameObject root)
        {
            switch (type)
            {
                case ToolType.Scalpel: return root.AddComponent<Scalpel>();
                case ToolType.TraumaShears: return root.AddComponent<TraumaShears>();
                case ToolType.Forceps: return root.AddComponent<Forceps>();
                case ToolType.Hemostat: return root.AddComponent<Hemostat>();
                case ToolType.Retractor: return root.AddComponent<Retractor>();
                case ToolType.Suction: return root.AddComponent<SuctionDevice>();
                case ToolType.Electrocautery: return root.AddComponent<ElectrocauteryPencil>();
                case ToolType.NeedleHolder: return root.AddComponent<NeedleHolder>();
                case ToolType.SutureNeedle: return root.AddComponent<NeedleHolder>();
                case ToolType.SkinStapler: return root.AddComponent<SkinStapler>();
                case ToolType.RibSpreader: return root.AddComponent<RibSpreader>();
                case ToolType.BoneSaw: return root.AddComponent<BoneSaw>();
                case ToolType.SurgicalDrill: return root.AddComponent<SurgicalDrill>();
                case ToolType.LaparoscopicCamera: return root.AddComponent<LaparoscopicCamera>();
                case ToolType.LaparoscopicGrasper: return root.AddComponent<LaparoscopicGrasper>();
                case ToolType.Irrigation: return root.AddComponent<IrrigationDevice>();
                case ToolType.Defibrillator: return root.AddComponent<Defibrillator>();
                case ToolType.Syringe: return root.AddComponent<Syringe>();
                case ToolType.ChestTube: return root.AddComponent<ChestTube>();
                case ToolType.Sponge: return root.AddComponent<SurgicalSponge>();
                case ToolType.FixationPlate: return root.AddComponent<FixationPlateTool>();
                default: return root.AddComponent<Hands>();
            }
        }

        /// <summary>Creates the geometry and returns the tip transform.</summary>
        private static Transform Build(ToolType type, Transform root, Material metal, Material grip)
        {
            switch (type)
            {
                case ToolType.Scalpel:
                    Handle(root, grip, 0.09f);
                    Blade(root, metal, 0.045f, 0.012f, 0.10f);
                    return Tip(root, 0.135f);

                case ToolType.TraumaShears:
                    Handle(root, grip, 0.08f);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "BladeA", root,
                        new Vector3(0.006f, 0f, 0.11f), Quaternion.Euler(0f, 6f, 0f),
                        new Vector3(0.006f, 0.014f, 0.07f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "BladeB", root,
                        new Vector3(-0.006f, 0f, 0.11f), Quaternion.Euler(0f, -6f, 0f),
                        new Vector3(0.006f, 0.014f, 0.07f), metal, false);
                    return Tip(root, 0.15f);

                case ToolType.Forceps:
                    Prongs(root, metal, 0.13f, 0.004f);
                    Handle(root, grip, 0.05f);
                    return Tip(root, 0.15f);

                case ToolType.Hemostat:
                    Handle(root, grip, 0.06f);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Ratchet", root,
                        new Vector3(0f, 0f, 0.045f), new Vector3(0.018f, 0.008f, 0.03f), metal, false);
                    Prongs(root, metal, 0.14f, 0.0035f);
                    return Tip(root, 0.155f);

                case ToolType.Retractor:
                    Handle(root, grip, 0.10f);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Shaft", root,
                        new Vector3(0f, 0f, 0.12f), new Vector3(0.010f, 0.006f, 0.10f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Blade", root,
                        new Vector3(0f, -0.02f, 0.175f), new Vector3(0.05f, 0.035f, 0.008f), metal, false);
                    return Tip(root, 0.18f);

                case ToolType.Suction:
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Body", root,
                        new Vector3(0f, 0f, 0.06f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.016f, 0.06f, 0.016f), MaterialLibrary.Plastic, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Nozzle", root,
                        new Vector3(0f, 0f, 0.145f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.010f, 0.03f, 0.010f), MaterialLibrary.Plastic, false);
                    return Tip(root, 0.18f);

                case ToolType.Electrocautery:
                    Handle(root, grip, 0.09f);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Button", root,
                        new Vector3(0f, 0.012f, 0.05f), new Vector3(0.012f, 0.005f, 0.02f),
                        MaterialLibrary.Get("cautery_btn", new Color(0.9f, 0.75f, 0.1f)), false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Electrode", root,
                        new Vector3(0f, 0f, 0.125f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.004f, 0.035f, 0.004f), metal, false);
                    return Tip(root, 0.16f);

                case ToolType.NeedleHolder:
                case ToolType.SutureNeedle:
                    Handle(root, grip, 0.07f);
                    Prongs(root, metal, 0.12f, 0.004f);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Needle", root,
                        new Vector3(0.004f, 0f, 0.155f), Quaternion.Euler(0f, 55f, 90f),
                        new Vector3(0.002f, 0.012f, 0.002f), metal, false);
                    return Tip(root, 0.165f);

                case ToolType.SkinStapler:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Body", root,
                        new Vector3(0f, 0f, 0.06f), new Vector3(0.030f, 0.045f, 0.11f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Head", root,
                        new Vector3(0f, -0.018f, 0.125f), new Vector3(0.022f, 0.014f, 0.03f), metal, false);
                    return Tip(root, 0.14f);

                case ToolType.RibSpreader:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Rack", root,
                        new Vector3(0f, 0f, 0.09f), new Vector3(0.10f, 0.012f, 0.014f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "ArmL", root,
                        new Vector3(-0.045f, -0.025f, 0.11f), new Vector3(0.012f, 0.05f, 0.05f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "ArmR", root,
                        new Vector3(0.045f, -0.025f, 0.11f), new Vector3(0.012f, 0.05f, 0.05f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Crank", root,
                        new Vector3(0.07f, 0f, 0.06f), Quaternion.Euler(0f, 0f, 90f),
                        new Vector3(0.008f, 0.03f, 0.008f), grip, false);
                    return Tip(root, 0.13f);

                case ToolType.BoneSaw:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Body", root,
                        new Vector3(0f, 0f, 0.06f), new Vector3(0.045f, 0.05f, 0.12f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Blade", root,
                        new Vector3(0f, 0f, 0.16f), new Vector3(0.035f, 0.002f, 0.06f), metal, false);
                    return Tip(root, 0.19f);

                case ToolType.SurgicalDrill:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Body", root,
                        new Vector3(0f, 0f, 0.05f), new Vector3(0.05f, 0.055f, 0.10f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Grip", root,
                        new Vector3(0f, -0.05f, 0.03f), new Vector3(0.035f, 0.07f, 0.035f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Bit", root,
                        new Vector3(0f, 0f, 0.13f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.006f, 0.04f, 0.006f), metal, false);
                    return Tip(root, 0.17f);

                case ToolType.LaparoscopicCamera:
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Shaft", root,
                        new Vector3(0f, 0f, 0.16f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.010f, 0.16f, 0.010f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Housing", root,
                        new Vector3(0f, 0f, 0.02f), new Vector3(0.035f, 0.035f, 0.05f), grip, false);
                    return Tip(root, 0.32f);

                case ToolType.LaparoscopicGrasper:
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Shaft", root,
                        new Vector3(0f, 0f, 0.16f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.008f, 0.16f, 0.008f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Handle", root,
                        new Vector3(0f, -0.02f, 0.01f), new Vector3(0.02f, 0.05f, 0.04f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "JawA", root,
                        new Vector3(0.003f, 0f, 0.325f), new Vector3(0.004f, 0.004f, 0.02f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "JawB", root,
                        new Vector3(-0.003f, 0f, 0.325f), new Vector3(0.004f, 0.004f, 0.02f), metal, false);
                    return Tip(root, 0.34f);

                case ToolType.Irrigation:
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Bottle", root,
                        new Vector3(0f, -0.01f, 0.05f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.035f, 0.05f, 0.035f), MaterialLibrary.Plastic, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Nozzle", root,
                        new Vector3(0f, 0f, 0.13f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.008f, 0.03f, 0.008f), MaterialLibrary.Plastic, false);
                    return Tip(root, 0.16f);

                case ToolType.Defibrillator:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "PaddleL", root,
                        new Vector3(-0.05f, 0f, 0.10f), new Vector3(0.07f, 0.07f, 0.015f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cube, "PaddleR", root,
                        new Vector3(0.05f, 0f, 0.10f), new Vector3(0.07f, 0.07f, 0.015f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "HandleL", root,
                        new Vector3(-0.05f, 0.03f, 0.05f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.014f, 0.04f, 0.014f), metal, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "HandleR", root,
                        new Vector3(0.05f, 0.03f, 0.05f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.014f, 0.04f, 0.014f), metal, false);
                    return Tip(root, 0.12f);

                case ToolType.Syringe:
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Barrel", root,
                        new Vector3(0f, 0f, 0.06f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.018f, 0.055f, 0.018f), MaterialLibrary.Plastic, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Plunger", root,
                        new Vector3(0f, 0f, -0.01f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.012f, 0.04f, 0.012f), grip, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Needle", root,
                        new Vector3(0f, 0f, 0.14f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.002f, 0.03f, 0.002f), metal, false);
                    return Tip(root, 0.175f);

                case ToolType.ChestTube:
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Tube", root,
                        new Vector3(0f, 0f, 0.10f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.014f, 0.10f, 0.014f), MaterialLibrary.Plastic, false);
                    PrimitiveFactory.Create(PrimitiveType.Cylinder, "Trocar", root,
                        new Vector3(0f, 0f, 0.21f), Quaternion.Euler(90f, 0f, 0f),
                        new Vector3(0.006f, 0.02f, 0.006f), metal, false);
                    return Tip(root, 0.23f);

                case ToolType.Sponge:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Gauze", root,
                        new Vector3(0f, 0f, 0.07f), new Vector3(0.06f, 0.02f, 0.06f),
                        MaterialLibrary.Get("sponge", new Color(0.95f, 0.95f, 0.92f)), false);
                    return Tip(root, 0.10f);

                case ToolType.FixationPlate:
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Plate", root,
                        new Vector3(0f, 0f, 0.11f), new Vector3(0.02f, 0.005f, 0.13f), metal, false);
                    Handle(root, grip, 0.05f);
                    return Tip(root, 0.18f);

                default:   // Hands
                    PrimitiveFactory.Create(PrimitiveType.Cube, "Palm", root,
                        new Vector3(0f, 0f, 0.06f), new Vector3(0.075f, 0.028f, 0.09f),
                        MaterialLibrary.Get("glove", new Color(0.35f, 0.62f, 0.72f)), false);
                    for (int i = 0; i < 4; i++)
                    {
                        PrimitiveFactory.Create(PrimitiveType.Cube, "Finger" + i, root,
                            new Vector3(-0.027f + i * 0.018f, 0f, 0.115f),
                            new Vector3(0.014f, 0.020f, 0.05f),
                            MaterialLibrary.Get("glove", new Color(0.35f, 0.62f, 0.72f)), false);
                    }

                    return Tip(root, 0.14f);
            }
        }

        // ---- Shape helpers ----------------------------------------------------

        private static void Handle(Transform root, Material grip, float length)
        {
            PrimitiveFactory.Create(PrimitiveType.Cube, "Handle", root,
                new Vector3(0f, 0f, length * 0.5f),
                new Vector3(0.016f, 0.010f, length), grip, false);
        }

        private static void Blade(Transform root, Material metal, float width, float thickness, float z)
        {
            PrimitiveFactory.Create(PrimitiveType.Cube, "Blade", root,
                new Vector3(0f, 0.002f, z),
                Quaternion.Euler(0f, 0f, 0f),
                new Vector3(thickness, width * 0.55f, width),
                metal, false);
        }

        private static void Prongs(Transform root, Material metal, float z, float thickness)
        {
            PrimitiveFactory.Create(PrimitiveType.Cube, "ProngA", root,
                new Vector3(thickness * 1.4f, 0f, z * 0.75f), Quaternion.Euler(0f, 2.5f, 0f),
                new Vector3(thickness, thickness * 2f, z * 0.55f), metal, false);
            PrimitiveFactory.Create(PrimitiveType.Cube, "ProngB", root,
                new Vector3(-thickness * 1.4f, 0f, z * 0.75f), Quaternion.Euler(0f, -2.5f, 0f),
                new Vector3(thickness, thickness * 2f, z * 0.55f), metal, false);
        }

        private static Transform Tip(Transform root, float z)
        {
            GameObject tip = PrimitiveFactory.Empty("Tip", root, new Vector3(0f, 0f, z));
            return tip.transform;
        }
    }
}
