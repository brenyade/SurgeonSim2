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

        [Header("Head bob")]
        public float bobFrequency = 7.5f;
        public float bobAmplitude = 0.022f;

        public Transform CameraPivot;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _pitch;
        private float _bobTimer;
        private Vector3 _cameraRestPosition;
        private bool _restCaptured;

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
            CaptureCameraRest();
        }

        /// <summary>
        /// Records the camera pivot's eye-height offset for the head-bob to return to.
        ///
        /// This deliberately does NOT happen in Awake: the rig builder adds this component before
        /// it assigns <see cref="CameraPivot"/>, so an Awake capture reads a null pivot, leaves the
        /// rest position at the origin, and the bob then eases the camera down to the surgeon's
        /// feet - which reads in game as spawning under the floor.
        /// </summary>
        private void CaptureCameraRest()
        {
            if (_restCaptured || CameraPivot == null)
            {
                return;
            }

            _cameraRestPosition = CameraPivot.localPosition;
            _restCaptured = true;
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

            // Belt and braces: if the pivot was wired up after Start, capture it the first time
            // we actually need it rather than bobbing toward a bogus origin.
            CaptureCameraRest();

            bool bobEnabled = !SettingsManager.Exists || SettingsManager.Instance.Data.cameraBob;
            if (!bobEnabled || CurrentSpeed < 0.15f)
            {
                CameraPivot.localPosition = Vector3.Lerp(CameraPivot.localPosition, _cameraRestPosition,
                    Time.deltaTime * 8f);
                return;
            }

            _bobTimer += Time.deltaTime * bobFrequency * Mathf.Clamp01(CurrentSpeed / walkSpeed);
            float offsetY = Mathf.Sin(_bobTimer) * bobAmplitude;
            float offsetX = Mathf.Cos(_bobTimer * 0.5f) * bobAmplitude * 0.5f;
            CameraPivot.localPosition = _cameraRestPosition + new Vector3(offsetX, offsetY, 0f);
        }

        /// <summary>Teleports the player (used when a case is set up).</summary>
        public void Warp(Vector3 position, float yaw)
        {
            _controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _pitch = 0f;
            if (CameraPivot != null)
            {
                CameraPivot.localRotation = Quaternion.identity;
            }

            _controller.enabled = true;
            _velocity = Vector3.zero;
        }
    }
}
