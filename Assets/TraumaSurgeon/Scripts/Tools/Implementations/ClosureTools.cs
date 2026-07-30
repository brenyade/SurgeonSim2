using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Needle holder with suture - repairs organs and vessels and closes deep layers.
    /// Secondary applies a running repair to a damaged organ instead of a single stitch.
    /// Misuse: stitching the wrong structure, or bunched stitches that leak.
    /// </summary>
    public class NeedleHolder : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (!ctx.HasTarget)
            {
                ReportUnnecessary("stitching air");
                return;
            }

            if (ctx.Target.isRepaired)
            {
                ReportUnnecessary($"{ctx.Target.displayName} is already closed");
                return;
            }

            StitchResult result = SuturingSystem.PlaceStitch(ref ctx);
            if (!result.Placed)
            {
                return;
            }

            PlaySound(SoundId.SutureStitch, ctx.Point, 0.5f, Random.Range(0.95f, 1.06f));

            SurgicalActionType action = ctx.Target.layer == AnatomyLayer.Skin
                ? SurgicalActionType.Suture
                : SurgicalActionType.Repair;

            ReportAction(action, ctx.Target, result.Accuracy);

            if (result.Complete)
            {
                GameEvents.RaiseNotification($"{ctx.Target.displayName} closed ({result.Placed_Count} stitches).",
                    NotificationType.Success);
                if (ctx.Target.layer == AnatomyLayer.Skin)
                {
                    ReportAction(SurgicalActionType.Close, ctx.Target, result.Accuracy);
                }
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("nothing to repair");
                return;
            }

            if (SuturingSystem.ApplyRepair(ref ctx, 55f))
            {
                PlaySound(SoundId.SutureStitch, ctx.Point, 0.6f);
                GameEvents.RaiseNotification($"{ctx.Target.displayName} repaired.", NotificationType.Success);
                ReportAction(SurgicalActionType.Repair, ctx.Target, ctx.Quality);
            }
        }
    }

    /// <summary>
    /// Skin stapler - fast skin closure. Faster than suturing but cosmetically worse and
    /// useless on anything except skin. Misuse: stapling deep tissue or organs.
    /// </summary>
    public class SkinStapler : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (!ctx.HasTarget)
            {
                ReportUnnecessary("stapling air");
                return;
            }

            if (ctx.Target.layer != AnatomyLayer.Skin)
            {
                ReportMisuse(ref ctx, "staples do not belong in deep tissue");
                return;
            }

            if (ctx.Target.isRepaired)
            {
                ReportUnnecessary("skin is already closed");
                return;
            }

            StitchResult result = SuturingSystem.PlaceStitch(ref ctx, staple: true);
            if (!result.Placed)
            {
                return;
            }

            PlaySound(SoundId.StaplerFire, ctx.Point, 0.6f, Random.Range(0.96f, 1.05f));
            ReportAction(SurgicalActionType.Staple, ctx.Target, result.Accuracy);

            if (result.Complete)
            {
                GameEvents.RaiseNotification($"{ctx.Target.displayName} closed with staples.",
                    NotificationType.Success);
                ReportAction(SurgicalActionType.Close, ctx.Target, result.Accuracy);
            }
        }
    }
}
