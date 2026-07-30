using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Forceps - fine grasping and tissue handling. Also extracts foreign bodies.
    /// Misuse: crushing an organ with a rough grip.
    /// </summary>
    public class Forceps : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (ForeignObjectSystem.Extract(ref ctx, out string message))
            {
                PlaySound(SoundId.ClampClick, ctx.Point, 0.6f);
                GameEvents.RaiseNotification(message, NotificationType.Success);
                ReportAction(SurgicalActionType.Remove, ctx.Target, ctx.Quality);
                return;
            }

            if (!ctx.HasTarget)
            {
                ReportUnnecessary("grasping nothing");
                return;
            }

            // Grasping a removable structure that is clamped and divided takes it out.
            if (ctx.Target.canBeRemoved && ctx.Target.requiresRemoval &&
                ctx.Target.state == DamageState.Perforated)
            {
                ctx.Target.Remove();
                PlaySound(SoundId.ClampClick, ctx.Point, 0.7f);
                GameEvents.RaiseNotification($"{ctx.Target.displayName} removed.", NotificationType.Success);
                ReportAction(SurgicalActionType.Remove, ctx.Target, ctx.Quality);
                return;
            }

            if (ctx.Quality < 0.3f && ctx.Patient != null)
            {
                ReportMisuse(ref ctx, "crushed tissue with a rough grip");
                return;
            }

            ReportAction(SurgicalActionType.Grasp, ctx.Target, ctx.Quality);
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("nothing to inspect");
                return;
            }

            GameEvents.RaiseNotification(ctx.Target.StatusLine(), NotificationType.Info);
            ReportAction(SurgicalActionType.Inspect, ctx.Target, 1f);
        }
    }

    /// <summary>
    /// Hemostat - clamps bleeding vessels and structures before division.
    /// Misuse: clamping healthy bowel or nerve tissue crushes it.
    /// </summary>
    public class Hemostat : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (!ctx.HasTarget)
            {
                ReportUnnecessary("clamping air");
                return;
            }

            if (ClampingSystem.Clamp(ref ctx, out string what))
            {
                PlaySound(SoundId.ClampClick, ctx.Point, 0.7f);
                GameEvents.RaiseNotification($"Clamped: {what}", NotificationType.Success);
                ReportAction(SurgicalActionType.Clamp, ctx.Target, ctx.Quality);
                return;
            }

            // Nothing worth clamping here - it just crushes tissue.
            if (ctx.Target.layer == AnatomyLayer.Organ && !ctx.Target.canBeRemoved)
            {
                ReportMisuse(ref ctx, "clamped healthy tissue");
            }
            else
            {
                ReportUnnecessary("nothing to clamp here");
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            // Release the most recent clamp on this part.
            if (!ctx.HasTarget)
            {
                return;
            }

            ClampMarker marker = ctx.Target.GetComponentInChildren<ClampMarker>();
            if (marker != null)
            {
                Destroy(marker.gameObject);
                PlaySound(SoundId.ClampClick, ctx.Point, 0.5f, 0.85f);
                GameEvents.RaiseNotification("Clamp released.", NotificationType.Info);
            }
            else
            {
                ReportUnnecessary("no clamp to release");
            }
        }
    }

    /// <summary>
    /// Retractor - holds a layer open. Held down to maintain retraction.
    /// Misuse: excessive traction tears the tissue edge.
    /// </summary>
    public class Retractor : SurgicalToolBase
    {
        public override bool IsContinuous => true;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                return;
            }

            if (RetractionSystem.Retract(ref ctx, 1.4f, out bool tore))
            {
                if (tore)
                {
                    GameEvents.RaiseNotification("Too much traction - the edge tore.", NotificationType.Warning);
                }
                else if (Random.value < 1.5f * ctx.DeltaTime)
                {
                    ReportAction(SurgicalActionType.Retract, ctx.Target, ctx.Quality);
                }
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (ctx.HasTarget)
            {
                ctx.Target.SetRetraction(0f);
                GameEvents.RaiseNotification("Retraction released.", NotificationType.Info);
            }
        }
    }

    /// <summary>
    /// Rib spreader - opens the chest after the sternum has been divided.
    /// Misuse: cranking it open on intact ribs fractures them.
    /// </summary>
    public class RibSpreader : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (!ctx.HasTarget || ctx.Patient == null)
            {
                ReportUnnecessary("nothing to spread");
                return;
            }

            AnatomyPart ribs = ctx.Patient.Body.Get(AnatomyIds.Ribs);
            AnatomyPart sternum = ctx.Patient.Body.Get(AnatomyIds.Sternum);

            if (ribs == null)
            {
                return;
            }

            bool sternotomyDone = sternum != null && sternum.isOpen;
            if (!sternotomyDone)
            {
                ctx.Patient.OrganDamage.ReportInjury(ribs, 15f, "ribs fractured by the spreader", true);
                GameEvents.RaiseNotification("You need to divide the sternum first.", NotificationType.Warning);
                return;
            }

            ribs.Open();
            ribs.SetRetraction(1f);
            PlaySound(SoundId.ClampClick, ctx.Point, 0.9f, 0.7f);
            GameEvents.RaiseNotification("Chest is open - heart and lungs exposed.", NotificationType.Success);
            ReportAction(SurgicalActionType.SpreadRibs, ribs, ctx.Quality);
        }
    }

    /// <summary>
    /// Laparoscopic grasper - long, indirect grasping through a port.
    /// Reach is longer but precision is lower, so accidental injury is easier.
    /// </summary>
    public class LaparoscopicGrasper : SurgicalToolBase
    {
        public override float Reach => 1.9f;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            // Working through a port halves your effective steadiness.
            ctx.Stability *= 0.6f;

            if (ForeignObjectSystem.Extract(ref ctx, out string message))
            {
                GameEvents.RaiseNotification(message, NotificationType.Success);
                ReportAction(SurgicalActionType.Remove, ctx.Target, ctx.Quality);
                return;
            }

            if (!ctx.HasTarget)
            {
                ReportUnnecessary("grasper closed on nothing");
                return;
            }

            if (ctx.Quality < 0.35f)
            {
                ReportMisuse(ref ctx, "blind grasp tore tissue");
                return;
            }

            ReportAction(SurgicalActionType.Grasp, ctx.Target, ctx.Quality);
            if (ctx.Target.canBeRemoved && ctx.Target.requiresRemoval &&
                ctx.Target.state == DamageState.Perforated)
            {
                ctx.Target.Remove();
                ReportAction(SurgicalActionType.Remove, ctx.Target, ctx.Quality);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (ctx.HasTarget)
            {
                RetractionSystem.Retract(ref ctx, 1f, out _);
                ReportAction(SurgicalActionType.Retract, ctx.Target, ctx.Quality);
            }
        }
    }
}
