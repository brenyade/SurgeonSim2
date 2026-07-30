using TraumaSurgeon.Core;
using TraumaSurgeon.InputSystem;
using UnityEngine;

namespace TraumaSurgeon.Player
{
    /// <summary>
    /// First person movement and mouse look. Deliberately slow and heavy: this is a theatre,
    /// not an arena. Holding the steady-hand key slows movement further.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 2.1f;
        public float sprintSpeed = 3.4f;
        public float steadySpeed = 0.9f;
        public float acceleration = 12f;
        public float gravity = -18f;

        [Header("Look")]
        public float pitchMin = -80f;
        public float pitchMax = 80f;

        [Header("Camera")]
        [Tooltip("Height of the surgeon's eyes above the floor, in metres.")]
        public float eyeHeight = 1.62f;

        [Header("Head bob")]
        public float bobFrequency = 7.5f;
        public float bobAmplitude = 0.022f;

        public Transform CameraPivot;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _pitch;
        private float _bobTimer;
        private float _bobPhaseOffsetX;
        private float _bobPhaseOffsetY;

        /// <summary>Current planar speed - used by the hand stability model.</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>True while the player holds the steady-hand key.</summary>
        public bool SteadyHand { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            ApplyEyeHeight();
        }

        /// <summary>
        /// Forces the camera pivot to the surgeon's eye height.
        ///
        /// Eye height is an explicit constant rather than a value captured from the pivot at
        /// startup. An earlier version captured it in Awake, which ran before the rig builder had
        /// assigned <see cref="CameraPivot"/>; the capture read null, the head-bob then eased the
        /// camera toward the origin, and the player ended up looking out from floor level. Driving
        /// the pivot from a constant makes that failure mode impossible regardless of setup order.
        /// </summary>
        public void ApplyEyeHeight()
        {
            if (CameraPivot != null)
            {
                CameraPivot.localPosition = new Vector3(0f, eyeHeight, 0f);
            }
        }

        private void Update()
        {
            if (!InputManager.Exists)
            {
                return;
            }

            InputManager input = InputManager.Instance;
            SteadyHand = input.Held(GameAction.SteadyHand);

            HandleLook(input);
            HandleMove(input);
            HandleBob();
        }

        private void HandleLook(InputManager input)
        {
            Vector2 look = input.LookDelta;

            // Steady-hand mode slows the camera so fine work is possible.
            float scale = SteadyHand ? 0.4f : 1f;

            transform.Rotate(Vector3.up, look.x * scale, Space.World);

            _pitch = Mathf.Clamp(_pitch - look.y * scale, pitchMin, pitchMax);
            if (CameraPivot != null)
            {
                CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void HandleMove(InputManager input)
        {
            Vector2 axis = input.MoveAxis;
            float targetSpeed = SteadyHand
                ? steadySpeed
                : (input.Held(GameAction.Sprint) ? sprintSpeed : walkSpeed);

            Vector3 desired = (transform.forward * axis.y + transform.right * axis.x) * targetSpeed;

            Vector3 planar = new Vector3(_velocity.x, 0f, _velocity.z);
            planar = Vector3.MoveTowards(planar, desired, acceleration * Time.deltaTime);
            _velocity.x = planar.x;
            _velocity.z = planar.z;

            if (_controller.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y += gravity * Time.deltaTime;
            }

            _controller.Move(_velocity * Time.deltaTime);
            CurrentSpeed = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        }

        private void HandleBob()
        {
            if (CameraPivot == null)
            {
                return;
            }

            bool bobEnabled = !SettingsManager.Exists || SettingsManager.Instance.Data.cameraBob;

            if (!bobEnabled || CurrentSpeed < 0.15f)
            {
                // Ease the bob offset out, never the eye height itself.
                _bobPhaseOffsetX = Mathf.Lerp(_bobPhaseOffsetX, 0f, Time.deltaTime * 8f);
                _bobPhaseOffsetY = Mathf.Lerp(_bobPhaseOffsetY, 0f, Time.deltaTime * 8f);
            }
            else
            {
                _bobTimer += Time.deltaTime * bobFrequency * Mathf.Clamp01(CurrentSpeed / walkSpeed);
                _bobPhaseOffsetY = Mathf.Sin(_bobTimer) * bobAmplitude;
                _bobPhaseOffsetX = Mathf.Cos(_bobTimer * 0.5f) * bobAmplitude * 0.5f;
            }

            // Absolute, not relative: the pivot is re-derived from eyeHeight every frame.
            CameraPivot.localPosition =
                new Vector3(_bobPhaseOffsetX, eyeHeight + _bobPhaseOffsetY, 0f);
        }

        /// <summary>
        /// Teleports the player (used when a case is set up).
        /// <paramref name="pitch"/> is degrees to look down: standing beside a 0.95 m table, a
        /// perfectly level view puts the surgical field off the bottom of the screen, so a case
        /// starts with the surgeon already looking at the patient.
        /// </summary>
        public void Warp(Vector3 position, float yaw, float pitch = 0f)
        {
            _controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            ApplyEyeHeight();
            if (CameraPivot != null)
            {
                CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            _controller.enabled = true;
            _velocity = Vector3.zero;
        }
    }
}
