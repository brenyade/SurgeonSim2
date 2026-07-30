using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Tools;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>Applying and removing hemostats.</summary>
    public static class ClampingSystem
    {
        /// <summary>
        /// Clamps the nearest bleeder, or applies a clamp to a structure about to be divided
        /// (appendix base, splenic hilum). Returns true when something was actually clamped.
        /// </summary>
        public static bool Clamp(ref ToolUseContext ctx, out string what)
        {
            what = string.Empty;
            if (!ctx.HasTarget)
            {
                return false;
            }

            BleedingPoint bleeder = ctx.Bleeder ?? ctx.Target.FindNearestBleeder(ctx.Point, 0.12f);
            if (bleeder != null && bleeder.Clamp())
            {
                SpawnClampVisual(ctx.Target, bleeder.transform.localPosition);
                what = ctx.Target.displayName + " bleeder";
                return true;
            }

            // Prophylactic clamp on a structure with no active bleeding.
            if (ctx.Target.canBeRemoved || ctx.Target.layer == AnatomyLayer.Vessel)
            {
                if (CuttingSystem.HasClamp(ctx.Target))
                {
                    return false;
                }

                SpawnClampVisual(ctx.Target, ctx.Target.transform.InverseTransformPoint(ctx.Point));
                what = ctx.Target.displayName;
                return true;
            }

            return false;
        }

        private static void SpawnClampVisual(AnatomyPart part, Vector3 localPosition)
        {
            GameObject clamp = PrimitiveFactory.Create(
                PrimitiveType.Cube,
                "Clamp",
                part.transform,
                localPosition,
                new Vector3(0.012f, 0.008f, 0.05f),
                MaterialLibrary.Steel,
                false);
            var marker = clamp.AddComponent<ClampMarker>();
            marker.Owner = part;
        }
    }

    /// <summary>Electrocautery: seals small bleeders, risks burns and surgical fire.</summary>
    public static class CauterySystem
    {
        /// <summary>Returns true when a bleeder was sealed.</summary>
        public static bool Cauterise(ref ToolUseContext ctx, float power, out bool majorVesselAttempt)
        {
            majorVesselAttempt = false;
            if (!ctx.HasTarget || ctx.Patient == null)
            {
                return false;
            }

            BleedingPoint bleeder = ctx.Bleeder ?? ctx.Target.FindNearestBleeder(ctx.Point, 0.10f);
            if (bleeder == null)
            {
                // Burning healthy tissue for no reason.
                ctx.Patient.OrganDamage.ReportInjury(ctx.Target, power * 3.5f * ctx.DeltaTime,
                    "cautery burn on healthy tissue", true);
                return false;
            }

            if (bleeder.majorVessel)
            {
                majorVesselAttempt = true;
                return false;
            }

            bool sealed_ = bleeder.Cauterise();
            if (sealed_)
            {
                // Even a good cautery leaves a small burn.
                ctx.Patient.OrganDamage.ReportInjury(ctx.Target, 0.8f, "cautery", false);
            }

            return sealed_;
        }

        /// <summary>Oxygen-rich field plus high power equals a surgical fire.</summary>
        public static bool CheckFireRisk(ref ToolUseContext ctx, float power, float continuousSeconds)
        {
            if (ctx.Patient == null)
            {
                return false;
            }

            float oxygen = ctx.Patient.Vitals.oxygenSaturation;
            float risk = 0f;
            if (continuousSeconds > 2.5f)
            {
                risk += (continuousSeconds - 2.5f) * 0.02f;
            }

            if (power > 0.8f)
            {
                risk += 0.01f;
            }

            if (oxygen > 99f)
            {
                risk += 0.01f;
            }

            return Random.value < risk * Time.deltaTime * 30f;
        }
    }

    /// <summary>Removing blood and fluid from the surgical field.</summary>
    public static class SuctionSystem
    {
        /// <summary>Returns millilitres removed this frame.</summary>
        public static float Suction(ref ToolUseContext ctx, float mlPerSecond)
        {
            if (ctx.Patient == null)
            {
                return 0f;
            }

            float removed = ctx.Patient.BloodLoss.Suction(mlPerSecond * ctx.DeltaTime);

            // Suctioning directly against delicate tissue causes trauma.
            if (ctx.HasTarget && ctx.Target.layer == AnatomyLayer.Organ && ctx.Stability < 0.35f)
            {
                ctx.Patient.OrganDamage.ReportInjury(ctx.Target, 1.2f * ctx.DeltaTime,
                    "suction tip trauma", true);
            }

            return removed;
        }
    }

    /// <summary>Irrigation: washes contamination out, lowers infection risk, cools tissue.</summary>
    public static class IrrigationSystem
    {
        public static void Irrigate(ref ToolUseContext ctx, float litresPerSecond)
        {
            if (ctx.Patient == null)
            {
                return;
            }

            float amount = litresPerSecond * ctx.DeltaTime;
            ctx.Patient.Vitals.infectionRisk = Mathf.Max(0f,
                ctx.Patient.Vitals.infectionRisk - amount * 22f);
            ctx.Patient.Vitals.temperature -= amount * 0.02f;

            // Irrigation also floats debris out of the field.
            ctx.Patient.BloodLoss.AddPooledBlood(amount * 40f);
        }
    }

    /// <summary>Holding tissue out of the way so deeper structures can be reached.</summary>
    public static class RetractionSystem
    {
        public static bool Retract(ref ToolUseContext ctx, float amountPerSecond, out bool tore)
        {
            tore = false;
            if (!ctx.HasTarget || ctx.Patient == null)
            {
                return false;
            }

            AnatomyPart part = ctx.Target;
            if (!part.blocksAccess && part.layer != AnatomyLayer.Organ)
            {
                return false;
            }

            float amount = amountPerSecond * ctx.DeltaTime;
            part.SetRetraction(Mathf.Clamp01(part.IsRetracted ? 1f : amount * 3f));

            // Yanking on tissue with a shaky hand tears it.
            if (ctx.Stability < 0.3f && Random.value < 0.4f * ctx.DeltaTime)
            {
                tore = true;
                ctx.Patient.OrganDamage.ReportInjury(part, 5f, "tissue torn by retractor", true);
            }

            foreach (AnatomyPart covered in part.Covered)
            {
                covered.SetAccessible(true);
            }

            return true;
        }
    }
}
