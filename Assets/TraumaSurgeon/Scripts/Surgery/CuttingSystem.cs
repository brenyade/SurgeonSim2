using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Tools;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    public struct CutResult
    {
        public bool Progressed;
        public bool Opened;
        public float Accuracy;
        public bool OffGuide;
    }

    /// <summary>
    /// Rules for cutting tissue: guide-path accuracy, tissue resistance, and the collateral damage
    /// that follows a wandering blade.
    /// </summary>
    public static class CuttingSystem
    {
        /// <summary>
        /// Applies a cut to the part under the tool.
        /// <paramref name="ratePerSecond"/> is how much of the layer a perfect cut opens per second.
        /// </summary>
        public static CutResult Cut(ref ToolUseContext ctx, float ratePerSecond, float bluntness = 1f)
        {
            var result = new CutResult { Accuracy = 1f };
            if (!ctx.HasTarget)
            {
                return result;
            }

            AnatomyPart part = ctx.Target;

            // Guide path accuracy. A layer can carry several planned approaches (the chest wall has
            // both a midline sternotomy and a lateral chest-drain line), so we score against the
            // closest one and let the player choose their approach.
            IncisionGuide[] guides = part.GetComponentsInChildren<IncisionGuide>(true);
            IncisionGuide best = null;
            float bestAccuracy = -1f;

            foreach (IncisionGuide guide in guides)
            {
                if (guide.Points.Count == 0)
                {
                    continue;
                }

                float accuracy = guide.Accuracy(ctx.Point);
                if (accuracy > bestAccuracy)
                {
                    bestAccuracy = accuracy;
                    best = guide;
                }
            }

            if (best != null)
            {
                result.Accuracy = bestAccuracy;
                result.OffGuide = result.Accuracy < 0.45f;
                if (!result.OffGuide)
                {
                    best.MarkProgress(ctx.Point);
                }
            }
            else
            {
                result.Accuracy = ctx.Quality;
            }

            float quality = Mathf.Clamp01(result.Accuracy * 0.6f + ctx.Quality * 0.4f);
            float amount = ratePerSecond * ctx.DeltaTime * bluntness;

            if (part.isOpen)
            {
                // Cutting an already-open layer just makes a mess.
                if (ctx.Patient != null)
                {
                    ctx.Patient.OrganDamage.ReportInjury(part, amount * 14f, "extending an open incision", true);
                }

                result.Progressed = true;
                return result;
            }

            result.Opened = part.ApplyIncision(amount, quality);
            result.Progressed = true;

            // A wandering blade damages the layer beneath.
            if (result.OffGuide && ctx.Patient != null && Random.value < 0.35f * ctx.DeltaTime * 10f)
            {
                AnatomyPart below = part.Covered.Count > 0 ? part.Covered[0] : null;
                if (below != null)
                {
                    ctx.Patient.OrganDamage.ReportInjury(below, 3.5f, "blade strayed off the incision line", true);
                }
            }

            return result;
        }

        /// <summary>
        /// Sharp division of a structure (appendix base, graft, adhesions). Requires the structure
        /// to be clamped first or it bleeds badly.
        /// </summary>
        public static bool Divide(ref ToolUseContext ctx, out bool wasClamped)
        {
            wasClamped = false;
            if (!ctx.HasTarget || ctx.Patient == null)
            {
                return false;
            }

            AnatomyPart part = ctx.Target;
            wasClamped = HasClamp(part);

            if (!wasClamped)
            {
                // Dividing an unclamped structure opens a brisk bleeder.
                part.SpawnBleeder(Random.insideUnitSphere * 0.02f, 3.5f);
                GameEvents.RaiseNotification($"{part.displayName} divided without a clamp - bleeding!",
                    NotificationType.Warning);
            }

            part.state = DamageState.Perforated;
            part.RefreshVisual();
            return true;
        }

        public static bool HasClamp(AnatomyPart part)
        {
            if (part == null)
            {
                return false;
            }

            foreach (BleedingPoint bp in part.BleedingPoints)
            {
                if (bp != null && bp.IsClamped)
                {
                    return true;
                }
            }

            return part.GetComponentInChildren<ClampMarker>() != null;
        }
    }

    /// <summary>Visual marker left behind by a hemostat so clamps persist on structures.</summary>
    public class ClampMarker : MonoBehaviour
    {
        public AnatomyPart Owner;
    }
}
