using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Gloved hands. Primary delivers chest compressions (rate matters - aim for 100-120/min),
    /// secondary performs manual pressure to slow a bleeder.
    /// Misuse: compressions on a beating heart cause injury and are scored as unnecessary.
    /// </summary>
    public class Hands : SurgicalToolBase
    {
        private float _lastCompressionTime = -1f;
        private float _qualityAccumulator;
        private int _compressionCount;

        protected override Vector3 AnimationOffset => new Vector3(0f, -0.06f, 0.02f);

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (ctx.Patient == null)
            {
                return;
            }

            PlayUseAnimation();
            PlaySound(SoundId.Heartbeat, transform.position, 0.5f, 1.2f);

            if (!ctx.Patient.Vitals.InCardiacArrest)
            {
                ReportUnnecessary("compressions on a perfusing rhythm");
                if (ctx.HasTarget)
                {
                    ctx.Patient.OrganDamage.ReportInjury(ctx.Target, 2f, "unnecessary compressions", true);
                }

                return;
            }

            float interval = _lastCompressionTime < 0f ? 0.55f : Time.time - _lastCompressionTime;
            _lastCompressionTime = Time.time;

            float quality = CPRSystem.Compress(ctx.Patient, interval);
            _qualityAccumulator += quality;
            _compressionCount++;

            // Report every few compressions so objectives tick without spamming the score log.
            if (_compressionCount >= 5)
            {
                float avg = _qualityAccumulator / _compressionCount;
                _compressionCount = 0;
                _qualityAccumulator = 0f;
                ReportAction(SurgicalActionType.Compressions, ctx.Target, avg);

                if (avg < 0.45f)
                {
                    GameEvents.RaiseStaffSpeech(StaffRole.SurgicalAssistant,
                        "Watch your rate - aim for a hundred a minute.");
                }
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("nothing to press on");
                return;
            }

            BleedingPoint bleeder = ctx.Bleeder ?? ctx.Target.FindNearestBleeder(ctx.Point, 0.15f);
            if (bleeder != null)
            {
                bleeder.Clamp();   // manual pressure behaves like a temporary clamp
                GameEvents.RaiseNotification("Direct pressure applied - find something to control it properly.",
                    NotificationType.Info);
                ReportAction(SurgicalActionType.Clamp, ctx.Target, ctx.Quality * 0.6f);
            }
            else
            {
                ReportUnnecessary("no bleeding here");
            }
        }

        public override void OnUnequip()
        {
            _lastCompressionTime = -1f;
            _compressionCount = 0;
            _qualityAccumulator = 0f;
            base.OnUnequip();
        }
    }
}
