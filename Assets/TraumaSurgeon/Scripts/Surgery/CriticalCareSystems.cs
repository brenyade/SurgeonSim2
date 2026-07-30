using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Tools;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>Manual chest compressions.</summary>
    public static class CPRSystem
    {
        public const float TargetRateBpm = 110f;

        /// <summary>
        /// Delivers compressions. Rate matters: the player is scored on how close their click
        /// cadence is to 100-120/min.
        /// </summary>
        public static float Compress(PatientController patient, float intervalSinceLast)
        {
            if (patient == null)
            {
                return 0f;
            }

            patient.DeliverCompression();

            if (intervalSinceLast <= 0f)
            {
                return 0.5f;
            }

            float bpm = 60f / intervalSinceLast;
            float quality = 1f - Mathf.Clamp01(Mathf.Abs(bpm - TargetRateBpm) / 60f);

            // Over-vigorous compressions crack ribs.
            if (bpm > 160f && Random.value < 0.15f)
            {
                AnatomyPart ribs = patient.Body.Get(AnatomyIds.Ribs);
                if (ribs != null)
                {
                    patient.OrganDamage.ReportInjury(ribs, 4f, "rib fracture from compressions", true);
                }
            }

            return quality;
        }
    }

    /// <summary>Defibrillator charge/shock cycle.</summary>
    public static class DefibrillationSystem
    {
        public const float ChargeSeconds = 1.6f;

        public static bool Shock(PatientController patient, float energyJoules)
        {
            if (patient == null)
            {
                return false;
            }

            return patient.Defibrillate(energyJoules);
        }

        /// <summary>Shocking with a wet/blood-soaked field or a hand on the patient is unsafe.</summary>
        public static bool IsFieldSafe(PatientController patient)
        {
            return patient == null || patient.BloodLoss.PooledBloodMl < 300f;
        }
    }

    /// <summary>Chest tube (tube thoracostomy) placement and drainage.</summary>
    public static class ChestTubeSystem
    {
        /// <summary>
        /// Inserts a chest tube. Correct placement is into the pleural space over the rib;
        /// placing it into lung tissue causes injury.
        /// </summary>
        public static bool Insert(ref ToolUseContext ctx, out string message)
        {
            message = string.Empty;
            if (!ctx.HasTarget || ctx.Patient == null)
            {
                message = "Nothing to insert into.";
                return false;
            }

            AnatomyPart target = ctx.Target;

            if (target.partId == AnatomyIds.PleuralSpace)
            {
                if (target.isOpen)
                {
                    message = "A tube is already in place.";
                    return false;
                }

                target.Open();
                SpawnTubeVisual(target);
                ctx.Patient.RelievePneumothorax(1f);
                message = "Chest tube in the pleural space - air rushing out.";
                return true;
            }

            if (target.partId == AnatomyIds.LungLeft || target.partId == AnatomyIds.LungRight)
            {
                ctx.Patient.OrganDamage.ReportInjury(target, 18f, "chest tube through lung parenchyma", true);
                message = "The tube went through the lung.";
                return false;
            }

            if (target.layer == AnatomyLayer.Skin || target.layer == AnatomyLayer.Muscle)
            {
                message = "Too shallow - you need to reach the pleural space.";
                return false;
            }

            message = "That is not a chest tube site.";
            return false;
        }

        /// <summary>Drains blood and air once the tube is in.</summary>
        public static float Drain(PatientController patient, float mlPerSecond, float dt)
        {
            if (patient == null)
            {
                return 0f;
            }

            AnatomyPart pleura = patient.Body.Get(AnatomyIds.PleuralSpace);
            if (pleura == null || !pleura.isOpen)
            {
                return 0f;
            }

            patient.RelievePneumothorax(0.25f * dt);
            return patient.BloodLoss.Suction(mlPerSecond * dt);
        }

        private static void SpawnTubeVisual(AnatomyPart pleura)
        {
            Vector3 basePos = pleura.MeshRoot != null ? pleura.MeshRoot.localPosition : Vector3.zero;
            PrimitiveFactory.Create(
                PrimitiveType.Cylinder,
                "ChestTube",
                pleura.transform,
                basePos + new Vector3(-0.06f, 0.06f, 0f),
                Quaternion.Euler(0f, 0f, 65f),
                new Vector3(0.012f, 0.09f, 0.012f),
                MaterialLibrary.Plastic,
                false);
        }
    }

    /// <summary>Bullets, glass, bone fragments - things that must come out.</summary>
    public static class ForeignObjectSystem
    {
        /// <summary>Spawns a foreign body inside a part (used by case setup).</summary>
        public static GameObject Spawn(AnatomyPart host, string id, Vector3 offset, float scale = 0.018f)
        {
            if (host == null)
            {
                return null;
            }

            Vector3 basePos = host.MeshRoot != null ? host.MeshRoot.localPosition : Vector3.zero;
            GameObject go = PrimitiveFactory.Create(
                PrimitiveType.Capsule,
                id,
                host.transform,
                basePos + offset,
                Vector3.one * scale,
                MaterialLibrary.DarkSteel);
            go.AddComponent<ForeignObject>().objectId = id;
            return go;
        }

        /// <summary>
        /// Attempts extraction with a grasping instrument. Rough handling drags the object through
        /// tissue and causes further damage.
        /// </summary>
        public static bool Extract(ref ToolUseContext ctx, out string message)
        {
            message = string.Empty;
            if (ctx.HitCollider == null)
            {
                return false;
            }

            ForeignObject fo = ctx.HitCollider.GetComponentInParent<ForeignObject>();
            if (fo == null)
            {
                return false;
            }

            if (ctx.Quality < 0.35f && ctx.Patient != null && ctx.Target != null)
            {
                ctx.Patient.OrganDamage.ReportInjury(ctx.Target, 6f,
                    "foreign body dragged through tissue", true);
                message = "You tore tissue pulling that out.";
            }
            else
            {
                message = $"{fo.objectId} removed.";
            }

            if (ctx.Patient != null && ctx.Patient.Body != null)
            {
                ctx.Patient.Body.RemoveForeignObject(fo.gameObject);
            }

            Object.Destroy(fo.gameObject);
            return true;
        }
    }

    /// <summary>Tag component for extractable foreign bodies.</summary>
    public class ForeignObject : MonoBehaviour
    {
        public string objectId = "foreign_object";
    }
}
