using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>
    /// Minimally invasive mode. The laparoscope drops a second camera into the field and routes it
    /// to a picture-in-picture RenderTexture that the HUD displays, while narrowing the working
    /// reach - which is what makes laparoscopic navigation harder than open surgery.
    /// </summary>
    public class LaparoscopySystem : MonoBehaviour
    {
        public static LaparoscopySystem Active { get; private set; }

        public bool IsActive { get; private set; }
        public RenderTexture ScopeTexture { get; private set; }

        private Camera _scopeCamera;
        private Transform _scopeRig;

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            Deactivate();
        }

        /// <summary>Inserts the scope, parented to the tool tip.</summary>
        public void Activate(Transform tip)
        {
            if (IsActive || tip == null)
            {
                return;
            }

            ScopeTexture = new RenderTexture(384, 288, 16) { name = "LaparoscopeRT" };

            var go = new GameObject("LaparoscopeCamera");
            go.transform.SetParent(tip, false);
            go.transform.localPosition = Vector3.forward * 0.02f;
            _scopeRig = go.transform;

            _scopeCamera = go.AddComponent<Camera>();
            _scopeCamera.fieldOfView = 62f;
            _scopeCamera.nearClipPlane = 0.005f;
            _scopeCamera.farClipPlane = 6f;
            _scopeCamera.targetTexture = ScopeTexture;
            _scopeCamera.clearFlags = CameraClearFlags.SolidColor;
            _scopeCamera.backgroundColor = new Color(0.05f, 0.02f, 0.03f);

            // A small light on the scope so the cavity is visible.
            var lightGo = new GameObject("ScopeLight");
            lightGo.transform.SetParent(go.transform, false);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 1.2f;
            light.spotAngle = 70f;
            light.intensity = 3.5f;
            light.color = new Color(1f, 0.97f, 0.92f);

            IsActive = true;
            GameEvents.RaiseNotification("Laparoscope inserted - monitor feed live.", NotificationType.Info);
        }

        public void Deactivate()
        {
            IsActive = false;

            if (_scopeRig != null)
            {
                Destroy(_scopeRig.gameObject);
                _scopeRig = null;
                _scopeCamera = null;
            }

            if (ScopeTexture != null)
            {
                ScopeTexture.Release();
                Destroy(ScopeTexture);
                ScopeTexture = null;
            }
        }

        /// <summary>Reach penalty applied to instruments while working through ports.</summary>
        public float ReachMultiplier => IsActive ? 0.75f : 1f;
    }
}
