using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Player
{
    /// <summary>
    /// Depth-based focus. Without a post-processing package available we approximate it by easing
    /// the camera's field of view toward whatever the surgeon is looking at: close work narrows the
    /// FOV (a "lean in" effect), distant looks widen it back out.
    ///
    /// If you add a post-processing stack later, drive its depth-of-field focus distance from
    /// <see cref="FocusDistance"/> and turn <see cref="useFovFallback"/> off.
    /// </summary>
    public class CameraFocus : MonoBehaviour
    {
        public Camera TargetCamera;
        public float maxFocusDistance = 6f;
        public float nearFov = 55f;
        public float focusSpeed = 3.5f;
        public bool useFovFallback = true;

        /// <summary>Distance to whatever is under the crosshair, in metres.</summary>
        public float FocusDistance { get; private set; } = 3f;

        private float _baseFov = 72f;

        private void Start()
        {
            if (TargetCamera == null)
            {
                TargetCamera = GetComponent<Camera>();
            }

            _baseFov = SettingsManager.Exists ? SettingsManager.Instance.Data.fieldOfView : 72f;
            SettingsManager.SettingsChanged += OnSettingsChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.SettingsChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged(SettingsData data)
        {
            _baseFov = data.fieldOfView;
        }

        private void Update()
        {
            if (TargetCamera == null)
            {
                return;
            }

            Ray ray = new Ray(TargetCamera.transform.position, TargetCamera.transform.forward);
            FocusDistance = Physics.Raycast(ray, out RaycastHit hit, maxFocusDistance)
                ? hit.distance
                : maxFocusDistance;

            bool enabledInSettings = !SettingsManager.Exists || SettingsManager.Instance.Data.depthOfField;
            if (!useFovFallback || !enabledInSettings)
            {
                TargetCamera.fieldOfView = Mathf.Lerp(TargetCamera.fieldOfView, _baseFov, Time.deltaTime * focusSpeed);
                return;
            }

            float t = Mathf.Clamp01(FocusDistance / maxFocusDistance);
            float targetFov = Mathf.Lerp(nearFov, _baseFov, t);
            TargetCamera.fieldOfView = Mathf.Lerp(TargetCamera.fieldOfView, targetFov, Time.deltaTime * focusSpeed);
        }
    }
}
