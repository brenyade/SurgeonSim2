using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Surgical drill - pilot holes in long bones, burr holes in the skull.
    /// Secondary drives screws through an installed plate.
    /// Misuse: plunging through the far cortex into whatever is behind it.
    /// </summary>
    public class SurgicalDrill : SurgicalToolBase
    {
        public override bool IsContinuous => true;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                StopContinuousAudio();
                return;
            }

            if (ctx.Target.layer != AnatomyLayer.Bone)
            {
                if (Random.value < 2f * ctx.DeltaTime)
                {
                    ReportMisuse(ref ctx, "drill bit into soft tissue");
                }

                return;
            }

            if (AudioManager.Exists && !AudioManager.Instance.IsLooping(SoundId.Drill))
            {
                AudioManager.Instance.StartLoop(SoundId.Drill, 0.45f);
            }

            bool holeDone = BoneWorkSystem.Drill(ref ctx, 0.55f, out bool plunged);

            if (plunged)
            {
                GameEvents.RaiseNotification("The drill plunged too deep!", NotificationType.Critical);
            }

            if (holeDone)
            {
                PlaySound(SoundId.Drill, ctx.Point, 0.5f);
                GameEvents.RaiseNotification("Hole drilled.", NotificationType.Success);
                ReportAction(SurgicalActionType.Drill, ctx.Target, ctx.Quality);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("no bone under the drill");
                return;
            }

            if (BoneWorkSystem.InstallScrew(ref ctx, out string failReason))
            {
                PlaySound(SoundId.Drill, ctx.Point, 0.5f, 0.8f);
                GameEvents.RaiseNotification("Screw driven home.", NotificationType.Success);
                ReportAction(SurgicalActionType.Implant, ctx.Target, ctx.Quality);
            }
            else
            {
                GameEvents.RaiseNotification($"Cannot place screw: {failReason}", NotificationType.Warning);
            }
        }

        public override void OnPrimaryReleased()
        {
            StopContinuousAudio();
        }

        protected override void StopContinuousAudio()
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.StopLoop(SoundId.Drill);
            }
        }
    }

    /// <summary>
    /// Fixation plate applicator - aligns the fracture (secondary) and lays the plate (primary).
    /// Misuse: plating an unreduced fracture leaves the limb malaligned.
    /// </summary>
    public class FixationPlateTool : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (!ctx.HasTarget)
            {
                ReportUnnecessary("no bone selected");
                return;
            }

            if (BoneWorkSystem.InstallPlate(ref ctx, out string failReason))
            {
                PlaySound(SoundId.ClampClick, ctx.Point, 0.7f, 0.8f);
                GameEvents.RaiseNotification("Fixation plate seated.", NotificationType.Success);
                ReportAction(SurgicalActionType.Implant, ctx.Target, ctx.Quality);
            }
            else
            {
                GameEvents.RaiseNotification($"Cannot place plate: {failReason}", NotificationType.Warning);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                return;
            }

            // Single click = one firm reduction manoeuvre, not one frame of progress.
            ToolUseContext manoeuvre = ctx;
            manoeuvre.DeltaTime = 0.35f;

            if (BoneWorkSystem.Align(ref manoeuvre, 0.9f))
            {
                PlaySound(SoundId.ClampClick, ctx.Point, 0.6f, 0.7f);
                GameEvents.RaiseNotification("Fracture reduced and aligned.", NotificationType.Success);
                ReportAction(SurgicalActionType.Repair, ctx.Target, ctx.Quality);
            }
        }
    }
}
