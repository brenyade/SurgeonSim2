using TraumaSurgeon.Core;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Anatomy
{
    /// <summary>
    /// Assembles a simplified, clinically styled body from Unity primitives.
    /// Layout: the patient lies supine with the head toward +Z. Layers are stacked in +Y so the
    /// surgeon works "down" through skin, fat, muscle and into the cavity.
    ///
    /// To replace the placeholder art, swap the <see cref="MakePart"/> primitive calls for
    /// prefab instantiation - the ids, layers and gameplay flags stay identical.
    /// </summary>
    public static class AnatomyBuilder
    {
        public static BodyAnatomy Build(Transform parent, Vector3 localPosition)
        {
            GameObject root = PrimitiveFactory.Empty("PatientBody", parent, localPosition);
            var body = root.AddComponent<BodyAnatomy>();

            BuildTorsoShell(body, root.transform);
            BuildSkeleton(body, root.transform);
            BuildThorax(body, root.transform);
            BuildAbdomen(body, root.transform);
            BuildVessels(body, root.transform);
            BuildHead(body, root.transform);
            BuildLimbs(body, root.transform);
            LinkCoverage(body);

            return body;
        }

        // ---- Region builders --------------------------------------------------

        private static void BuildTorsoShell(BodyAnatomy body, Transform root)
        {
            // Abdomen stack
            AnatomyPart skinAbd = MakePart(body, root, AnatomyIds.SkinAbdomen, "Abdominal Skin", AnatomyLayer.Skin,
                PrimitiveType.Cube, new Vector3(0f, 0.150f, 0.14f), new Vector3(0.34f, 0.020f, 0.30f),
                new Color(0.86f, 0.68f, 0.58f), blocksAccess: true, resistance: 0.30f);
            IncisionGuide.Create(skinAbd.transform, new Vector3(0.10f, 0.163f, 0.02f), new Vector3(0.12f, 0.163f, 0.14f));

            MakePart(body, root, AnatomyIds.FatAbdomen, "Subcutaneous Fat", AnatomyLayer.Fat,
                PrimitiveType.Cube, new Vector3(0f, 0.128f, 0.14f), new Vector3(0.32f, 0.020f, 0.28f),
                new Color(0.95f, 0.86f, 0.55f), blocksAccess: true, resistance: 0.18f);

            MakePart(body, root, AnatomyIds.MuscleAbdomen, "Abdominal Wall", AnatomyLayer.Muscle,
                PrimitiveType.Cube, new Vector3(0f, 0.106f, 0.14f), new Vector3(0.30f, 0.020f, 0.26f),
                new Color(0.68f, 0.24f, 0.24f), blocksAccess: true, resistance: 0.45f);

            // Chest stack
            AnatomyPart skinChest = MakePart(body, root, AnatomyIds.SkinChest, "Chest Skin", AnatomyLayer.Skin,
                PrimitiveType.Cube, new Vector3(0f, 0.150f, 0.45f), new Vector3(0.36f, 0.020f, 0.32f),
                new Color(0.86f, 0.68f, 0.58f), blocksAccess: true, resistance: 0.30f);
            IncisionGuide.Create(skinChest.transform, new Vector3(0f, 0.163f, 0.32f), new Vector3(0f, 0.163f, 0.58f));

            MakePart(body, root, AnatomyIds.FatChest, "Chest Fat", AnatomyLayer.Fat,
                PrimitiveType.Cube, new Vector3(0f, 0.128f, 0.45f), new Vector3(0.34f, 0.020f, 0.30f),
                new Color(0.95f, 0.86f, 0.55f), blocksAccess: true, resistance: 0.18f);

            MakePart(body, root, AnatomyIds.MuscleChest, "Pectoral Muscle", AnatomyLayer.Muscle,
                PrimitiveType.Cube, new Vector3(0f, 0.108f, 0.45f), new Vector3(0.32f, 0.018f, 0.28f),
                new Color(0.68f, 0.24f, 0.24f), blocksAccess: true, resistance: 0.5f);

            // Pleural space - the target for a chest tube.
            AnatomyPart pleura = MakePart(body, root, AnatomyIds.PleuralSpace, "Pleural Space", AnatomyLayer.Cavity,
                PrimitiveType.Cube, new Vector3(-0.14f, 0.09f, 0.45f), new Vector3(0.06f, 0.05f, 0.22f),
                new Color(0.55f, 0.62f, 0.70f), blocksAccess: false, resistance: 0.25f);
            pleura.injurySensitivity = 0.4f;
        }

        private static void BuildSkeleton(BodyAnatomy body, Transform root)
        {
            // Ribs: one logical part, several primitive bars.
            AnatomyPart ribs = MakeMultiPart(body, root, AnatomyIds.Ribs, "Rib Cage", AnatomyLayer.Bone,
                new Color(0.93f, 0.91f, 0.84f), blocksAccess: true, resistance: 0.95f,
                highlightSize: new Vector3(0.34f, 0.06f, 0.30f),
                highlightOffset: new Vector3(0f, 0.088f, 0.45f));
            for (int i = 0; i < 6; i++)
            {
                float z = 0.32f + i * 0.052f;
                PrimitiveFactory.Create(PrimitiveType.Cylinder, "Rib" + i, ribs.MeshRoot,
                    new Vector3(0f, 0.088f, z), Quaternion.Euler(0f, 0f, 90f),
                    new Vector3(0.016f, 0.155f, 0.016f), MaterialLibrary.Bone);
            }

            ribs.Initialise(ribs.MeshRoot, new Color(0.93f, 0.91f, 0.84f));

            MakePart(body, root, AnatomyIds.Sternum, "Sternum", AnatomyLayer.Bone,
                PrimitiveType.Cube, new Vector3(0f, 0.092f, 0.45f), new Vector3(0.045f, 0.016f, 0.24f),
                new Color(0.93f, 0.91f, 0.84f), blocksAccess: true, resistance: 0.98f);
        }

        private static void BuildThorax(BodyAnatomy body, Transform root)
        {
            AnatomyPart lungL = MakePart(body, root, AnatomyIds.LungLeft, "Left Lung", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(-0.095f, 0.055f, 0.46f), new Vector3(0.11f, 0.11f, 0.11f),
                new Color(0.80f, 0.55f, 0.58f), blocksAccess: false, resistance: 0.22f);
            lungL.requiresRepair = false;
            lungL.injurySensitivity = 2.2f;

            AnatomyPart lungR = MakePart(body, root, AnatomyIds.LungRight, "Right Lung", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(0.095f, 0.055f, 0.46f), new Vector3(0.11f, 0.11f, 0.11f),
                new Color(0.80f, 0.55f, 0.58f), blocksAccess: false, resistance: 0.22f);
            lungR.injurySensitivity = 2.2f;

            AnatomyPart heart = MakePart(body, root, AnatomyIds.Heart, "Heart", AnatomyLayer.Organ,
                PrimitiveType.Sphere, new Vector3(-0.02f, 0.055f, 0.42f), new Vector3(0.09f, 0.10f, 0.10f),
                new Color(0.70f, 0.20f, 0.22f), blocksAccess: false, resistance: 0.30f);
            heart.injurySensitivity = 4f;
            heart.canBeRemoved = false;

            AnatomyPart coronary = MakePart(body, root, AnatomyIds.CoronaryArtery, "Left Anterior Descending",
                AnatomyLayer.Vessel, PrimitiveType.Cylinder, new Vector3(-0.045f, 0.088f, 0.42f),
                Quaternion.Euler(70f, 0f, 12f), new Vector3(0.008f, 0.045f, 0.008f),
                new Color(0.55f, 0.13f, 0.16f), blocksAccess: false, resistance: 0.25f);
            coronary.injurySensitivity = 3.5f;
            coronary.suturesRequired = 6;
        }

        private static void BuildAbdomen(BodyAnatomy body, Transform root)
        {
            AnatomyPart liver = MakePart(body, root, AnatomyIds.Liver, "Liver", AnatomyLayer.Organ,
                PrimitiveType.Cube, new Vector3(0.075f, 0.062f, 0.245f), new Vector3(0.16f, 0.06f, 0.14f),
                new Color(0.48f, 0.20f, 0.18f), blocksAccess: false, resistance: 0.25f);
            liver.injurySensitivity = 2.6f;

            AnatomyPart stomach = MakePart(body, root, AnatomyIds.Stomach, "Stomach", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(-0.075f, 0.062f, 0.245f), Quaternion.Euler(0f, 0f, 60f),
                new Vector3(0.075f, 0.075f, 0.075f), new Color(0.78f, 0.55f, 0.50f),
                blocksAccess: false, resistance: 0.22f);
            stomach.injurySensitivity = 1.8f;

            AnatomyPart spleen = MakePart(body, root, AnatomyIds.Spleen, "Spleen", AnatomyLayer.Organ,
                PrimitiveType.Sphere, new Vector3(-0.115f, 0.055f, 0.205f), new Vector3(0.085f, 0.055f, 0.07f),
                new Color(0.45f, 0.16f, 0.22f), blocksAccess: false, resistance: 0.20f);
            spleen.canBeRemoved = true;
            spleen.injurySensitivity = 2.4f;

            AnatomyPart gall = MakePart(body, root, AnatomyIds.Gallbladder, "Gallbladder", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(0.055f, 0.048f, 0.215f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.032f, 0.030f, 0.032f), new Color(0.45f, 0.55f, 0.28f),
                blocksAccess: false, resistance: 0.18f);
            gall.canBeRemoved = true;

            AnatomyPart mesentery = MakePart(body, root, AnatomyIds.Mesentery, "Mesentery", AnatomyLayer.Vessel,
                PrimitiveType.Cube, new Vector3(0f, 0.048f, 0.115f), new Vector3(0.22f, 0.012f, 0.16f),
                new Color(0.72f, 0.48f, 0.36f), blocksAccess: false, resistance: 0.15f);
            mesentery.injurySensitivity = 2f;

            AnatomyPart intestines = MakeMultiPart(body, root, AnatomyIds.Intestines, "Small Bowel", AnatomyLayer.Organ,
                new Color(0.82f, 0.62f, 0.48f), blocksAccess: false, resistance: 0.18f,
                highlightSize: new Vector3(0.26f, 0.08f, 0.22f),
                highlightOffset: new Vector3(0f, 0.04f, 0.115f));
            for (int i = 0; i < 5; i++)
            {
                float z = 0.045f + i * 0.036f;
                float x = (i % 2 == 0) ? -0.045f : 0.045f;
                PrimitiveFactory.Create(PrimitiveType.Capsule, "Loop" + i, intestines.MeshRoot,
                    new Vector3(x, 0.040f, z), Quaternion.Euler(0f, 0f, 90f),
                    new Vector3(0.05f, 0.085f, 0.05f), MaterialLibrary.Intestine);
            }

            intestines.Initialise(intestines.MeshRoot, new Color(0.82f, 0.62f, 0.48f));
            intestines.suturesRequired = 5;
            intestines.injurySensitivity = 1.6f;

            AnatomyPart appendix = MakePart(body, root, AnatomyIds.Appendix, "Appendix", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(0.105f, 0.036f, 0.035f), Quaternion.Euler(35f, 0f, 0f),
                new Vector3(0.022f, 0.038f, 0.022f), new Color(0.80f, 0.45f, 0.40f),
                blocksAccess: false, resistance: 0.16f);
            appendix.canBeRemoved = true;

            MakePart(body, root, AnatomyIds.KidneyLeft, "Left Kidney", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(-0.125f, 0.030f, 0.155f), Quaternion.Euler(0f, 0f, 0f),
                new Vector3(0.045f, 0.045f, 0.045f), new Color(0.52f, 0.24f, 0.24f),
                blocksAccess: false, resistance: 0.24f);

            MakePart(body, root, AnatomyIds.KidneyRight, "Right Kidney", AnatomyLayer.Organ,
                PrimitiveType.Capsule, new Vector3(0.125f, 0.030f, 0.155f), Quaternion.Euler(0f, 0f, 0f),
                new Vector3(0.045f, 0.045f, 0.045f), new Color(0.52f, 0.24f, 0.24f),
                blocksAccess: false, resistance: 0.24f);
        }

        private static void BuildVessels(BodyAnatomy body, Transform root)
        {
            AnatomyPart aorta = MakePart(body, root, AnatomyIds.Aorta, "Abdominal Aorta", AnatomyLayer.Vessel,
                PrimitiveType.Cylinder, new Vector3(-0.02f, 0.022f, 0.20f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.022f, 0.20f, 0.022f), new Color(0.62f, 0.12f, 0.14f),
                blocksAccess: false, resistance: 0.30f);
            aorta.injurySensitivity = 6f;
            aorta.suturesRequired = 6;

            AnatomyPart cava = MakePart(body, root, AnatomyIds.VenaCava, "Inferior Vena Cava", AnatomyLayer.Vessel,
                PrimitiveType.Cylinder, new Vector3(0.02f, 0.022f, 0.20f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.024f, 0.20f, 0.024f), new Color(0.30f, 0.18f, 0.42f),
                blocksAccess: false, resistance: 0.28f);
            cava.injurySensitivity = 6f;
            cava.suturesRequired = 6;

            AnatomyPart femoral = MakePart(body, root, AnatomyIds.FemoralArtery, "Femoral Artery", AnatomyLayer.Vessel,
                PrimitiveType.Cylinder, new Vector3(-0.055f, 0.055f, -0.42f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.014f, 0.16f, 0.014f), new Color(0.62f, 0.12f, 0.14f),
                blocksAccess: false, resistance: 0.28f);
            femoral.injurySensitivity = 4.5f;
        }

        private static void BuildHead(BodyAnatomy body, Transform root)
        {
            AnatomyPart scalp = MakePart(body, root, AnatomyIds.Scalp, "Scalp", AnatomyLayer.Skin,
                PrimitiveType.Sphere, new Vector3(0f, 0.115f, 0.80f), new Vector3(0.19f, 0.10f, 0.21f),
                new Color(0.84f, 0.66f, 0.56f), blocksAccess: true, resistance: 0.32f);
            IncisionGuide.Create(scalp.transform, new Vector3(-0.06f, 0.17f, 0.78f), new Vector3(-0.06f, 0.17f, 0.86f), 5);

            MakePart(body, root, AnatomyIds.Skull, "Skull", AnatomyLayer.Bone,
                PrimitiveType.Sphere, new Vector3(0f, 0.110f, 0.80f), new Vector3(0.175f, 0.095f, 0.195f),
                new Color(0.93f, 0.91f, 0.84f), blocksAccess: true, resistance: 1f);

            AnatomyPart brain = MakePart(body, root, AnatomyIds.Brain, "Brain", AnatomyLayer.Organ,
                PrimitiveType.Sphere, new Vector3(0f, 0.105f, 0.80f), new Vector3(0.155f, 0.085f, 0.175f),
                new Color(0.86f, 0.76f, 0.72f), blocksAccess: false, resistance: 0.12f);
            brain.injurySensitivity = 8f;
            brain.canBeRemoved = false;

            AnatomyPart hematoma = MakePart(body, root, AnatomyIds.Hematoma, "Subdural Hematoma", AnatomyLayer.Organ,
                PrimitiveType.Sphere, new Vector3(-0.055f, 0.135f, 0.80f), new Vector3(0.075f, 0.045f, 0.085f),
                new Color(0.32f, 0.05f, 0.08f), blocksAccess: false, resistance: 0.08f);
            hematoma.canBeRemoved = true;
            hematoma.MeshRoot.gameObject.SetActive(false);   // only shown for the neuro case
        }

        private static void BuildLimbs(BodyAnatomy body, Transform root)
        {
            AnatomyPart skinLeg = MakePart(body, root, AnatomyIds.SkinLeg, "Thigh Skin", AnatomyLayer.Skin,
                PrimitiveType.Cube, new Vector3(-0.075f, 0.085f, -0.42f), new Vector3(0.13f, 0.02f, 0.34f),
                new Color(0.86f, 0.68f, 0.58f), blocksAccess: true, resistance: 0.32f);
            IncisionGuide.Create(skinLeg.transform, new Vector3(-0.075f, 0.098f, -0.50f), new Vector3(-0.075f, 0.098f, -0.34f), 6);

            MakePart(body, root, AnatomyIds.MuscleLeg, "Quadriceps", AnatomyLayer.Muscle,
                PrimitiveType.Cube, new Vector3(-0.075f, 0.062f, -0.42f), new Vector3(0.12f, 0.02f, 0.32f),
                new Color(0.68f, 0.24f, 0.24f), blocksAccess: true, resistance: 0.5f);

            AnatomyPart femurL = MakePart(body, root, AnatomyIds.FemurLeft, "Left Femur", AnatomyLayer.Bone,
                PrimitiveType.Cylinder, new Vector3(-0.075f, 0.030f, -0.42f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.035f, 0.18f, 0.035f), new Color(0.93f, 0.91f, 0.84f),
                blocksAccess: false, resistance: 1f);
            femurL.suturesRequired = 4;

            MakePart(body, root, AnatomyIds.FemurRight, "Right Femur", AnatomyLayer.Bone,
                PrimitiveType.Cylinder, new Vector3(0.075f, 0.030f, -0.42f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.035f, 0.18f, 0.035f), new Color(0.93f, 0.91f, 0.84f),
                blocksAccess: false, resistance: 1f);

            AnatomyPart skinArm = MakePart(body, root, AnatomyIds.SkinArm, "Forearm Skin", AnatomyLayer.Skin,
                PrimitiveType.Cube, new Vector3(0.245f, 0.055f, 0.30f), new Vector3(0.08f, 0.018f, 0.26f),
                new Color(0.86f, 0.68f, 0.58f), blocksAccess: true, resistance: 0.30f);
            skinArm.injurySensitivity = 0.8f;

            MakePart(body, root, AnatomyIds.HumerusLeft, "Left Humerus", AnatomyLayer.Bone,
                PrimitiveType.Cylinder, new Vector3(0.245f, 0.030f, 0.30f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.028f, 0.13f, 0.028f), new Color(0.93f, 0.91f, 0.84f),
                blocksAccess: false, resistance: 1f);
        }

        /// <summary>Wires up which layers hide which structures.</summary>
        private static void LinkCoverage(BodyAnatomy body)
        {
            Cover(body, AnatomyIds.SkinAbdomen, AnatomyIds.FatAbdomen);
            Cover(body, AnatomyIds.FatAbdomen, AnatomyIds.MuscleAbdomen);
            Cover(body, AnatomyIds.MuscleAbdomen,
                AnatomyIds.Liver, AnatomyIds.Stomach, AnatomyIds.Spleen, AnatomyIds.Gallbladder,
                AnatomyIds.Intestines, AnatomyIds.Appendix, AnatomyIds.Mesentery,
                AnatomyIds.KidneyLeft, AnatomyIds.KidneyRight, AnatomyIds.Aorta, AnatomyIds.VenaCava);

            Cover(body, AnatomyIds.SkinChest, AnatomyIds.FatChest);
            Cover(body, AnatomyIds.FatChest, AnatomyIds.MuscleChest);
            Cover(body, AnatomyIds.MuscleChest, AnatomyIds.Ribs, AnatomyIds.Sternum, AnatomyIds.PleuralSpace);
            Cover(body, AnatomyIds.Ribs, AnatomyIds.LungLeft, AnatomyIds.LungRight, AnatomyIds.Heart,
                AnatomyIds.CoronaryArtery);
            Cover(body, AnatomyIds.Sternum, AnatomyIds.Heart, AnatomyIds.CoronaryArtery);

            Cover(body, AnatomyIds.Scalp, AnatomyIds.Skull);
            Cover(body, AnatomyIds.Skull, AnatomyIds.Brain, AnatomyIds.Hematoma);

            Cover(body, AnatomyIds.SkinLeg, AnatomyIds.MuscleLeg);
            Cover(body, AnatomyIds.MuscleLeg, AnatomyIds.FemurLeft, AnatomyIds.FemoralArtery);
            Cover(body, AnatomyIds.SkinArm, AnatomyIds.HumerusLeft);

            // Everything covered starts inaccessible.
            foreach (AnatomyPart part in body.AllParts)
            {
                foreach (AnatomyPart covered in part.Covered)
                {
                    covered.SetAccessible(false);
                }
            }
        }

        private static void Cover(BodyAnatomy body, string coverId, params string[] coveredIds)
        {
            AnatomyPart cover = body.Get(coverId);
            if (cover == null)
            {
                return;
            }

            foreach (string id in coveredIds)
            {
                AnatomyPart part = body.Get(id);
                if (part != null && !cover.Covered.Contains(part))
                {
                    cover.Covered.Add(part);
                }
            }
        }

        // ---- Primitive helpers ------------------------------------------------

        private static AnatomyPart MakePart(BodyAnatomy body, Transform root, string id, string name,
            AnatomyLayer layer, PrimitiveType prim, Vector3 pos, Vector3 scale, Color color,
            bool blocksAccess, float resistance)
        {
            return MakePart(body, root, id, name, layer, prim, pos, Quaternion.identity, scale, color,
                blocksAccess, resistance);
        }

        private static AnatomyPart MakePart(BodyAnatomy body, Transform root, string id, string name,
            AnatomyLayer layer, PrimitiveType prim, Vector3 pos, Quaternion rot, Vector3 scale, Color color,
            bool blocksAccess, float resistance)
        {
            GameObject partRoot = PrimitiveFactory.Empty(id, root);
            var part = partRoot.AddComponent<AnatomyPart>();
            part.partId = id;
            part.displayName = name;
            part.layer = layer;
            part.blocksAccess = blocksAccess;
            part.resistance = resistance;

            GameObject mesh = PrimitiveFactory.Create(prim, "Mesh", partRoot.transform, pos, rot, scale,
                MaterialLibrary.Organ);
            part.Initialise(mesh.transform, color);

            body.Register(part);
            return part;
        }

        /// <summary>Creates a part whose mesh is a group of primitives (ribs, bowel loops).</summary>
        private static AnatomyPart MakeMultiPart(BodyAnatomy body, Transform root, string id, string name,
            AnatomyLayer layer, Color color, bool blocksAccess, float resistance, Vector3 highlightSize,
            Vector3 highlightOffset)
        {
            GameObject partRoot = PrimitiveFactory.Empty(id, root);
            var part = partRoot.AddComponent<AnatomyPart>();
            part.partId = id;
            part.displayName = name;
            part.layer = layer;
            part.blocksAccess = blocksAccess;
            part.resistance = resistance;
            part.highlightSize = highlightSize;
            part.highlightOffset = highlightOffset;

            GameObject meshRoot = PrimitiveFactory.Empty("Mesh", partRoot.transform);
            part.Initialise(meshRoot.transform, color);

            body.Register(part);
            return part;
        }
    }
}
