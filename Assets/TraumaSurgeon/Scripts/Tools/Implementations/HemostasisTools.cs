using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Electrocautery pencil - seals small bleeders with heat.
    /// Secondary raises the power setting. Misuse: burns healthy tissue, cannot seal a major
    /// vessel, and sustained high power in an oxygen rich field starts a surgical fire.
    /// </summary>
    public class ElectrocauteryPencil : SurgicalToolBase
    {
        public override bool IsContinuous => true;

        private float _power = 0.5f;
        private float _continuousSeconds;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                _continuousSeconds = 0f;
                StopContinuousAudio();
                return;
            }

            if (AudioManager.Exists && !AudioManager.Instance.IsLooping(SoundId.Cautery))
            {
                AudioManager.Instance.StartLoop(SoundId.Cautery, 0.35f);
            }

            _continuousSeconds += ctx.DeltaTime;

            if (CauterySystem.Cauterise(ref ctx, _power, out bool majorVessel))
            {
                PlaySound(SoundId.Cautery, ctx.Point, 0.4f);
                ReportAction(SurgicalActionType.Cauterize, ctx.Target, ctx.Quality);
                GameEvents.RaiseNotification("Bleeder sealed.", NotificationType.Success);
            }
            else if (majorVessel && Random.value < 2f * ctx.DeltaTime)
            {
                GameEvents.RaiseNotification("Too big to cauterise - clamp and tie it.", NotificationType.Warning);
            }

            if (CauterySystem.CheckFireRisk(ref ctx, _power, _continuousSeconds))
            {
                GameEvents.RaiseComplicationStarted(ComplicationType.SurgicalFire,
                    "Surgical fire in the field!");
                _continuousSeconds = 0f;
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            _power = _power >= 0.9f ? 0.35f : _power + 0.25f;
            GameEvents.RaiseNotification($"Cautery power: {Mathf.RoundToInt(_power * 100f)}%",
                NotificationType.Info);
        }

        public override void OnPrimaryReleased()
        {
            _continuousSeconds = 0f;
            StopContinuousAudio();
        }

        protected override void StopContinuousAudio()
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.StopLoop(SoundId.Cautery);
            }
        }
    }

    /// <summary>
    /// Suction device (Yankauer) - clears blood so the field stays visible.
    /// Misuse: pressing the tip into an organ causes suction trauma.
    /// </summary>
    public class SuctionDevice : SurgicalToolBase
    {
        public override bool IsContinuous => true;

        private float _removedThisUse;
        private float _activeSeconds;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (AudioManager.Exists && !AudioManager.Instance.IsLooping(SoundId.Suction))
            {
                AudioManager.Instance.StartLoop(SoundId.Suction, 0.4f);
            }

            float removed = SuctionSystem.Suction(ref ctx, 120f);
            removed += ChestTubeSystem.Drain(ctx.Patient, 40f, ctx.DeltaTime);
            _removedThisUse += removed;
            _activeSeconds += ctx.DeltaTime;

            // Report on volume cleared, or on sustained use: checking a dry field for bleeding is a
            // legitimate surgical step and must never be impossible to complete.
            if (_removedThisUse > 60f || _activeSeconds > 1.5f)
            {
                _removedThisUse = 0f;
                _activeSeconds = 0f;
                ReportAction(SurgicalActionType.Suction, ctx.Target, ctx.Quality);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            // Quick burst: clears a fixed amount instantly.
            if (ctx.Patient != null)
            {
                float removed = ctx.Patient.BloodLoss.Suction(150f);
                if (removed > 1f)
                {
                    PlaySound(SoundId.Suction, ctx.Point, 0.6f);
                    ReportAction(SurgicalActionType.Suction, ctx.Target, ctx.Quality);
                }
                else
                {
                    ReportUnnecessary("field is already dry");
                }
            }
        }

        public override void OnPrimaryReleased()
        {
            _activeSeconds = 0f;
            StopContinuousAudio();
        }

        protected override void StopContinuousAudio()
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.StopLoop(SoundId.Suction);
            }
        }
    }

    /// <summary>
    /// Irrigation device - washes out contamination and debris.
    /// Misuse: over-irrigating cools the patient and floods the field.
    /// </summary>
    public class IrrigationDevice : SurgicalToolBase
    {
        public override bool IsContinuous => true;

        private float _seconds;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (ctx.Patient == null)
            {
                return;
            }

            if (AudioManager.Exists && !AudioManager.Instance.IsLooping(SoundId.Irrigation))
            {
                AudioManager.Instance.StartLoop(SoundId.Irrigation, 0.35f);
            }

            IrrigationSystem.Irrigate(ref ctx, 0.4f);
            _seconds += ctx.DeltaTime;

            if (_seconds > 1.2f)
            {
                _seconds = 0f;
                ReportAction(SurgicalActionType.Irrigate, ctx.Target, ctx.Quality);
            }

            if (ctx.Patient.Vitals.temperature < 34.5f && Random.value < 0.5f * ctx.DeltaTime)
            {
                GameEvents.RaiseNotification("The patient is getting cold - ease off the irrigation.",
                    NotificationType.Warning);
            }
        }

        public override void OnPrimaryReleased()
        {
            _seconds = 0f;
            StopContinuousAudio();
        }

        protected override void StopContinuousAudio()
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.StopLoop(SoundId.Irrigation);
            }
        }
    }

    /// <summary>
    /// Surgical sponge - packs a bleeding area. Primary packs, secondary retrieves.
    /// Misuse: a sponge left inside at closure is a catastrophic scoring penalty.
    /// </summary>
    public class SurgicalSponge : SurgicalToolBase
    {
        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            SpongeTracker tracker = SurgeryServices.Sponges;
            if (tracker == null || !ctx.HasTarget)
            {
                ReportUnnecessary("nowhere to pack");
                return;
            }

            tracker.Pack(ctx.Target, ctx.Point);
            GameEvents.RaiseNotification($"Sponge packed into {ctx.Target.displayName}.",
                NotificationType.Info);
            ReportAction(SurgicalActionType.Pack, ctx.Target, ctx.Quality);
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            SpongeTracker tracker = SurgeryServices.Sponges;
            if (tracker == null)
            {
                return;
            }

            PackedSponge sponge = ctx.HitCollider != null
                ? ctx.HitCollider.GetComponentInParent<PackedSponge>()
                : null;

            if (sponge == null && ctx.HasTarget)
            {
                sponge = ctx.Target.GetComponentInChildren<PackedSponge>();
            }

            if (sponge != null && tracker.Remove(sponge))
            {
                GameEvents.RaiseNotification("Sponge retrieved.", NotificationType.Success);
                ReportAction(SurgicalActionType.Remove, ctx.Target, ctx.Quality);
            }
            else
            {
                ReportUnnecessary("no sponge to retrieve here");
            }
        }
    }
}
