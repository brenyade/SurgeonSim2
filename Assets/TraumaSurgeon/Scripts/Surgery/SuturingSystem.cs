using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Tools;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    public struct StitchResult
    {
        public bool Placed;
        public bool Complete;
        public float Accuracy;
        public int Placed_Count;
        public int Required;
    }

    /// <summary>
    /// Suturing and stapling. Stitches must be reasonably evenly spaced along the wound: placing
    /// them on top of each other wastes time and leaves gaps that leak.
    /// </summary>
    public static class SuturingSystem
    {
        private const float MinSpacing = 0.018f;

        public static StitchResult PlaceStitch(ref ToolUseContext ctx, bool staple = false)
        {
            var result = new StitchResult();
            if (!ctx.HasTarget)
            {
                return result;
            }

            AnatomyPart part = ctx.Target;
            result.Required = part.suturesRequired;

            // Reject a stitch placed right on top of the previous one.
            SutureMarker[] existing = part.GetComponentsInChildren<SutureMarker>();
            foreach (SutureMarker marker in existing)
            {
                if (Vector3.Distance(marker.transform.position, ctx.Point) < MinSpacing)
                {
                    result.Accuracy = 0.2f;
                    GameEvents.RaiseNotification("Stitches are bunching up - space them out.",
                        NotificationType.Info);
                    return result;
                }
            }

            result.Accuracy = Mathf.Clamp01(ctx.Quality);
            result.Placed = true;

            SpawnStitchVisual(part, part.transform.InverseTransformPoint(ctx.Point), staple);

            // A stitch through a bleeder controls it.
            BleedingPoint bleeder = ctx.Bleeder ?? part.FindNearestBleeder(ctx.Point, 0.05f);
            if (bleeder != null)
            {
                bleeder.Suture();
            }

            result.Complete = part.PlaceSuture();
            result.Placed_Count = part.suturesPlaced;

            // A sloppy stitch on a hollow organ or vessel leaks.
            if (result.Accuracy < 0.4f && (part.layer == AnatomyLayer.Vessel || part.layer == AnatomyLayer.Organ))
            {
                part.SpawnBleeder(Random.insideUnitSphere * 0.02f, 0.9f);
                GameEvents.RaiseNotification("That stitch is leaking.", NotificationType.Warning);
            }

            return result;
        }

        /// <summary>Repair progress for non-stitch repairs (patching a bowel, packing a liver).</summary>
        public static bool ApplyRepair(ref ToolUseContext ctx, float amountPerSecond)
        {
            if (!ctx.HasTarget)
            {
                return false;
            }

            return ctx.Target.ApplyRepair(amountPerSecond * ctx.DeltaTime * Mathf.Lerp(0.4f, 1.3f, ctx.Quality));
        }

        private static void SpawnStitchVisual(AnatomyPart part, Vector3 localPosition, bool staple)
        {
            Vector3 size = staple ? new Vector3(0.010f, 0.003f, 0.004f) : new Vector3(0.003f, 0.003f, 0.012f);
            PrimitiveFactory.Create(
                PrimitiveType.Cube,
                staple ? "Staple" : "Suture",
                part.transform,
                localPosition,
                size,
                staple ? MaterialLibrary.Steel : MaterialLibrary.DarkSteel,
                false).AddComponent<SutureMarker>();
        }
    }

    /// <summary>Marker component so placed stitches can be counted and spaced.</summary>
    public class SutureMarker : MonoBehaviour
    {
    }
}
