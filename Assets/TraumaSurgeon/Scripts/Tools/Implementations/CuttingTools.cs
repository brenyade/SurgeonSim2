using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Scalpel - the primary incision instrument.
    /// Primary: cut along the guide path. Secondary: sharp division of a clamped structure.
    /// Misuse: cutting an organ or vessel directly causes bleeding and tissue damage.
    /// </summary>
    public class Scalpel : SurgicalToolBase
    {
        public override bool IsContinuous => true;

        private float _soundCooldown;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                return;
            }

            // Slicing straight into an organ or vessel is never the plan.
            if (IsLayer(ctx.Target, AnatomyLayer.Organ, AnatomyLayer.Vessel) && !ctx.Target.requiresRepair)
            {
                if (Random.value < 2f * ctx.DeltaTime)
                {
                    ReportMisuse(ref ctx, "blade into an organ");
                }

                return;
            }

            if (ctx.Target.layer == AnatomyLayer.Bone)
            {
                ReportUnnecessary("a scalpel will not cut bone");
                return;
            }

            CutResult result = CuttingSystem.Cut(ref ctx, 0.55f);
            if (!result.Progressed)
            {
                return;
            }

            _soundCooldown -= ctx.DeltaTime;
            if (_soundCooldown <= 0f)
            {
                _soundCooldown = 0.25f;
                PlaySound(SoundId.Incision, ctx.Point, 0.5f, Random.Range(0.92f, 1.08f));
                PlayUseAnimation();
            }

            if (result.Opened)
            {
                ReportAction(SurgicalActionType.Incise, ctx.Target, result.Accuracy);
                GameEvents.RaiseNotification($"{ctx.Target.displayName} opened.", NotificationType.Success);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("nothing to divide");
                return;
            }

            if (CuttingSystem.Divide(ref ctx, out bool clamped))
            {
                PlaySound(SoundId.Incision, ctx.Point, 0.7f);
                PlayUseAnimation();
                ReportAction(SurgicalActionType.Cut, ctx.Target, clamped ? ctx.Quality : ctx.Quality * 0.4f);
            }
        }
    }

    /// <summary>
    /// Trauma shears - fast, blunt cutting for clothing, dressings and tough tissue.
    /// Misuse: crushes and tears delicate structures instead of cutting them cleanly.
    /// </summary>
    public class TraumaShears : SurgicalToolBase
    {
        public override bool IsContinuous => false;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("cutting air");
                return;
            }

            PlaySound(SoundId.ScissorsCut, ctx.Point, 0.6f);
            PlayUseAnimation();

            if (IsLayer(ctx.Target, AnatomyLayer.Organ, AnatomyLayer.Vessel))
            {
                ReportMisuse(ref ctx, "shears crushed delicate tissue");
                return;
            }

            // Shears open a layer quickly but leave a ragged edge.
            var scratchCtx = ctx;
            scratchCtx.DeltaTime = 0.35f;
            CutResult result = CuttingSystem.Cut(ref scratchCtx, 1.6f, 0.9f);
            if (result.Opened)
            {
                ReportAction(SurgicalActionType.Incise, ctx.Target, result.Accuracy * 0.75f);
            }
            else
            {
                ReportAction(SurgicalActionType.Cut, ctx.Target, result.Accuracy * 0.7f);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (ctx.HasTarget && CuttingSystem.Divide(ref ctx, out _))
            {
                PlaySound(SoundId.ScissorsCut, ctx.Point, 0.7f);
                ReportAction(SurgicalActionType.Cut, ctx.Target, ctx.Quality * 0.8f);
            }
        }
    }

    /// <summary>
    /// Oscillating bone saw - sternotomy and bone division.
    /// Misuse: overshoot straight into the heart or lungs behind the sternum.
    /// </summary>
    public class BoneSaw : SurgicalToolBase
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
                    ReportMisuse(ref ctx, "saw blade on soft tissue");
                }

                return;
            }

            if (AudioManager.Exists && !AudioManager.Instance.IsLooping(SoundId.BoneSaw))
            {
                AudioManager.Instance.StartLoop(SoundId.BoneSaw, 0.5f);
            }

            bool opened = BoneWorkSystem.Saw(ref ctx, 0.30f, out bool overshoot);
            if (overshoot)
            {
                GameEvents.RaiseNotification("The saw slipped past the bone!", NotificationType.Critical);
            }

            if (opened)
            {
                ReportAction(SurgicalActionType.Saw, ctx.Target, ctx.Quality);
                GameEvents.RaiseNotification($"{ctx.Target.displayName} divided.", NotificationType.Success);
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
                AudioManager.Instance.StopLoop(SoundId.BoneSaw);
            }
        }
    }
}
