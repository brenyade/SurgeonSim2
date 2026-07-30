using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.InputSystem;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Surgery;
using TraumaSurgeon.Tools;
using UnityEngine;

namespace TraumaSurgeon.Player
{
    /// <summary>
    /// The surgeon's hand. Applies tremor to the held instrument, resolves what the tip is
    /// touching, builds a <see cref="ToolUseContext"/> and drives the equipped tool.
    ///
    /// Stability model: a baseline tremor, amplified by movement, fatigue and a bloody field,
    /// damped hard by steady-hand mode (which costs stamina). Precision is rewarded, never required.
    /// </summary>
    public class HandController : MonoBehaviour
    {
        [Header("References")]
        public Transform HandAnchor;
        public Camera PlayerCamera;
        public FirstPersonController Movement;
        public ToolController Tools;

        [Header("Tremor")]
        public float baseTremor = 0.55f;
        public float tremorSpeed = 6.5f;
        public float tremorPositionScale = 0.012f;
        public float tremorRotationScale = 2.2f;

        [Header("Steady hand")]
        public float steadyDrainPerSecond = 26f;
        public float steadyRecoveryPerSecond = 14f;

        /// <summary>0..100 steady-hand stamina.</summary>
        public float Stamina { get; private set; } = 100f;

        /// <summary>0..1 current hand steadiness, exposed for the HUD meter.</summary>
        public float Stability { get; private set; } = 1f;

        /// <summary>Last resolved contact - used by the HUD to name what is under the tool.</summary>
        public AnatomyPart HoveredPart { get; private set; }

        private Vector3 _handRestPosition;
        private Quaternion _handRestRotation;
        private float _noiseSeedX;
        private float _noiseSeedY;
        private float _holdSeconds;
        private bool _wasHolding;

        private void Awake()
        {
            if (HandAnchor != null)
            {
                _handRestPosition = HandAnchor.localPosition;
                _handRestRotation = HandAnchor.localRotation;
            }

            _noiseSeedX = Random.value * 100f;
            _noiseSeedY = Random.value * 100f;
        }

        private void Update()
        {
            UpdateStability();
            ApplyTremor();
            HandleInput();
        }

        // ---- Stability --------------------------------------------------------

        private void UpdateStability()
        {
            bool steady = Movement != null && Movement.SteadyHand;

            if (steady && Stamina > 0f)
            {
                Stamina = Mathf.Max(0f, Stamina - steadyDrainPerSecond * Time.deltaTime);
            }
            else
            {
                Stamina = Mathf.Min(100f, Stamina + steadyRecoveryPerSecond * Time.deltaTime);
            }

            float difficultyTremor = GameManager.Exists && GameManager.Instance.Difficulty != null
                ? GameManager.Instance.Difficulty.TremorScale
                : 0.6f;

            float tremor = baseTremor * difficultyTremor;

            // Moving while operating is the biggest source of instability.
            if (Movement != null)
            {
                tremor += Mathf.Clamp01(Movement.CurrentSpeed / 2f) * 0.8f;
            }

            // Fatigue: an empty stamina bar means a shaky hand.
            tremor += (1f - Stamina / 100f) * 0.35f;

            if (steady && Stamina > 0f)
            {
                tremor *= 0.25f;
            }

            Stability = Mathf.Clamp01(1f - tremor * 0.55f);
        }

        private void ApplyTremor()
        {
            if (HandAnchor == null)
            {
                return;
            }

            float t = Time.time * tremorSpeed;
            float amount = (1f - Stability);

            float nx = (Mathf.PerlinNoise(_noiseSeedX, t) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(_noiseSeedY, t) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(_noiseSeedX + _noiseSeedY, t * 0.7f) - 0.5f) * 2f;

            HandAnchor.localPosition = _handRestPosition +
                new Vector3(nx, ny, nz) * (tremorPositionScale * amount);

            HandAnchor.localRotation = _handRestRotation *
                Quaternion.Euler(ny * tremorRotationScale * amount,
                                 nx * tremorRotationScale * amount,
                                 nz * tremorRotationScale * amount * 0.5f);
        }

        // ---- Input ------------------------------------------------------------

        private void HandleInput()
        {
            if (!InputManager.Exists || Tools == null)
            {
                return;
            }

            InputManager input = InputManager.Instance;
            SurgicalToolBase tool = Tools.Equipped;
            if (tool == null)
            {
                return;
            }

            bool holdingPrimary = input.Held(GameAction.UseTool);

            if (holdingPrimary)
            {
                _holdSeconds += Time.deltaTime;
            }

            bool fired = false;

            if (tool.IsContinuous)
            {
                if (holdingPrimary)
                {
                    ToolUseContext ctx = BuildContext(false);
                    tool.UsePrimary(ref ctx);
                    fired = true;
                }
                else if (_wasHolding)
                {
                    tool.OnPrimaryReleased();
                }
            }
            else if (input.Pressed(GameAction.UseTool) && tool.IsReady)
            {
                ToolUseContext ctx = BuildContext(false);
                tool.UsePrimary(ref ctx);
                fired = true;
            }

            if (input.Pressed(GameAction.AltUseTool) && tool.IsReady)
            {
                ToolUseContext ctx = BuildContext(true);
                tool.UseSecondary(ref ctx);
            }

            if (!holdingPrimary)
            {
                _holdSeconds = 0f;
            }

            _wasHolding = holdingPrimary;

            // Mouse wheel dials the syringe dose while it is equipped.
            if (tool is Syringe syringe)
            {
                float scroll = input.ScrollDelta;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    syringe.AdjustDose(Mathf.Sign(scroll));
                }
            }

            if (!fired)
            {
                // Keep the hover target fresh for the HUD even when not using the tool.
                BuildContext(false);
            }
        }

        // ---- Context ----------------------------------------------------------

        /// <summary>Resolves what the instrument is touching and packages the use context.</summary>
        private ToolUseContext BuildContext(bool secondary)
        {
            var ctx = new ToolUseContext
            {
                Patient = SurgeryServices.Patient,
                Stability = Stability,
                Pressure = Mathf.Clamp01(_holdSeconds / 1.5f),
                Secondary = secondary,
                DeltaTime = Time.deltaTime,
                AngleQuality = 1f,
                FieldClarity = 1f
            };

            if (PlayerCamera == null)
            {
                HoveredPart = null;
                return ctx;
            }

            SurgicalToolBase tool = Tools != null ? Tools.Equipped : null;
            float reach = tool != null ? tool.Reach : 1.4f;
            if (SurgeryServices.Laparoscopy != null)
            {
                reach *= SurgeryServices.Laparoscopy.ReachMultiplier;
            }

            // The ray originates at the camera but is aimed through the (trembling) tool tip,
            // so tremor genuinely moves the contact point.
            Vector3 origin = PlayerCamera.transform.position;
            Vector3 direction = PlayerCamera.transform.forward;
            if (tool != null && tool.Tip != null)
            {
                Vector3 aim = tool.Tip.position - origin;
                if (aim.sqrMagnitude > 0.0001f)
                {
                    direction = Vector3.Slerp(direction, aim.normalized, 0.65f);
                }
            }

            ctx.Forward = direction;

            // Triggers are ignored so interaction volumes and bleeding-point trigger spheres never
            // block the instrument; bleeders are resolved by proximity below instead.
            if (Physics.Raycast(origin, direction, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<FirstPersonController>() == null)
            {
                ctx.Point = hit.point;
                ctx.Normal = hit.normal;
                ctx.HitCollider = hit.collider;
                ctx.Target = hit.collider.GetComponentInParent<AnatomyPart>();

                // Approach angle: square to the surface is ideal.
                ctx.AngleQuality = Mathf.Clamp01(Vector3.Dot(-direction.normalized, hit.normal.normalized));
                ctx.AngleQuality = Mathf.Lerp(0.35f, 1f, ctx.AngleQuality);

                BleedingPoint bleeder = hit.collider.GetComponent<BleedingPoint>();
                if (bleeder == null && ctx.Target != null)
                {
                    bleeder = ctx.Target.FindNearestBleeder(hit.point, 0.09f);
                }

                ctx.Bleeder = bleeder;

                // Foreign bodies sit inside a part; grabbing them needs the collider itself.
                if (ctx.Target == null)
                {
                    ctx.Target = hit.collider.GetComponentInParent<AnatomyPart>();
                }
            }
            else
            {
                ctx.Point = origin + direction * reach;
                ctx.Normal = -direction;
            }

            PatientController patient = SurgeryServices.Patient;
            if (patient != null && patient.BloodLoss != null)
            {
                ctx.FieldClarity = patient.BloodLoss.FieldClarity;
            }

            HoveredPart = ctx.Target;
            return ctx;
        }
    }
}
