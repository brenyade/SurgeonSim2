using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Scoring;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Base class for every instrument. Subclasses implement the surgical behaviour; the base
    /// handles identity, reach, the "wrong tool" penalty, animation and audio.
    /// </summary>
    public abstract class SurgicalToolBase : MonoBehaviour
    {
        public ToolType Type = ToolType.None;

        /// <summary>Tip transform used for contact tests and effects.</summary>
        public Transform Tip;

        protected ToolData Data;

        /// <summary>True for tools that act every frame while held (scalpel, suction, drill).</summary>
        public virtual bool IsContinuous => false;

        /// <summary>Reach from the camera in metres.</summary>
        public virtual float Reach => 1.4f;

        /// <summary>Primary action reported to the objective system.</summary>
        public virtual SurgicalActionType PrimaryAction => Data != null ? Data.Primary : SurgicalActionType.None;

        public virtual SurgicalActionType SecondaryAction => Data != null ? Data.Secondary : SurgicalActionType.None;

        public string DisplayName => Data != null ? Data.displayName : Type.ToString();

        /// <summary>Consequence text shown in the tool codex / used when misused.</summary>
        public string MisuseConsequence => Data != null ? Data.misuseConsequence : "Tissue damage";

        private float _cooldownTimer;
        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private float _animTimer;

        protected virtual void Awake()
        {
            Data = DataLibrary.GetTool(Type);
            if (Tip == null)
            {
                Tip = transform;
            }

            _restLocalPosition = transform.localPosition;
            _restLocalRotation = transform.localRotation;
        }

        protected virtual void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            // Simple use animation: a short jab forward that eases back to rest.
            if (_animTimer > 0f)
            {
                _animTimer -= Time.deltaTime * 4f;
                float k = Mathf.Clamp01(_animTimer);
                transform.localPosition = _restLocalPosition + AnimationOffset * Mathf.Sin(k * Mathf.PI);
                transform.localRotation = _restLocalRotation * Quaternion.Euler(AnimationRotation * Mathf.Sin(k * Mathf.PI));
            }
        }

        /// <summary>Local space movement of the use animation.</summary>
        protected virtual Vector3 AnimationOffset => new Vector3(0f, -0.02f, 0.04f);

        /// <summary>Local euler rotation of the use animation.</summary>
        protected virtual Vector3 AnimationRotation => new Vector3(-12f, 0f, 0f);

        public bool IsReady => _cooldownTimer <= 0f;

        protected void StartCooldown(float seconds)
        {
            _cooldownTimer = seconds;
        }

        protected void PlayUseAnimation()
        {
            _animTimer = 1f;
        }

        // ---- Lifecycle --------------------------------------------------------

        public virtual void OnEquip()
        {
            gameObject.SetActive(true);
            if (AudioManager.Exists)
            {
                AudioManager.Instance.Play(SoundId.ToolPickup, 0.6f);
            }
        }

        public virtual void OnUnequip()
        {
            StopContinuousAudio();
            gameObject.SetActive(false);
        }

        // ---- Use --------------------------------------------------------------

        /// <summary>Called on the frame the primary button goes down (or every frame if continuous).</summary>
        public abstract void UsePrimary(ref ToolUseContext ctx);

        /// <summary>Called on the frame the secondary button goes down.</summary>
        public virtual void UseSecondary(ref ToolUseContext ctx)
        {
            ReportUnnecessary("no alternate function");
        }

        /// <summary>Called every frame the primary button is released for continuous tools.</summary>
        public virtual void OnPrimaryReleased()
        {
            StopContinuousAudio();
        }

        protected virtual void StopContinuousAudio() { }

        // ---- Shared helpers ---------------------------------------------------

        /// <summary>
        /// Applies the standard misuse penalty: collateral tissue damage plus a score event.
        /// Every tool routes invalid targets through this so the rules stay consistent.
        /// </summary>
        protected void ReportMisuse(ref ToolUseContext ctx, string reason)
        {
            GameEvents.RaiseScoreEvent(ScoreEventType.WrongTool, 1f, $"{DisplayName}: {reason}");

            if (ctx.HasTarget && ctx.Patient != null && ctx.Patient.OrganDamage != null)
            {
                float damage = (Data != null ? Data.tissueDamageOnMisuse : 6f) * (1.2f - ctx.Stability * 0.5f);
                ctx.Patient.OrganDamage.ReportInjury(ctx.Target, damage, $"{DisplayName} misuse", true);
            }

            GameEvents.RaiseNotification($"{DisplayName}: {MisuseConsequence}", NotificationType.Warning);
        }

        /// <summary>Used when a tool fires at nothing useful - costs economy-of-motion points.</summary>
        protected void ReportUnnecessary(string reason)
        {
            GameEvents.RaiseScoreEvent(ScoreEventType.UnnecessaryAction, 1f, $"{DisplayName}: {reason}");
        }

        /// <summary>Reports a successful action to the objective and scoring systems.</summary>
        protected void ReportAction(SurgicalActionType action, AnatomyPart target, float quality)
        {
            GameEvents.RaiseActionPerformed(action, target, quality);
            GameEvents.RaiseScoreEvent(
                quality >= 0.6f ? ScoreEventType.PrecisionGood : ScoreEventType.PrecisionPoor,
                quality,
                DisplayName);
        }

        protected void PlaySound(SoundId id, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (AudioManager.Exists)
            {
                AudioManager.Instance.PlayAt(id, position, volume, pitch);
            }
        }

        /// <summary>True when this part is a sensible target for the given layers.</summary>
        protected static bool IsLayer(AnatomyPart part, params AnatomyLayer[] layers)
        {
            if (part == null)
            {
                return false;
            }

            foreach (AnatomyLayer layer in layers)
            {
                if (part.layer == layer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
