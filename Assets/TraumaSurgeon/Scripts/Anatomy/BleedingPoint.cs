using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Anatomy
{
    /// <summary>
    /// A discrete bleeder. Can be clamped (temporary), cauterised or sutured (permanent).
    /// Clamps that are never converted to a permanent fix will re-bleed if disturbed.
    /// </summary>
    public class BleedingPoint : MonoBehaviour
    {
        [Tooltip("Blood loss in millilitres per second while uncontrolled.")]
        public float rate = 2f;

        [Tooltip("Vessel calibre - large vessels cannot be cauterised, they must be clamped/sutured.")]
        public bool majorVessel;

        public bool IsClamped { get; private set; }
        public bool IsCauterised { get; private set; }
        public bool IsSutured { get; private set; }

        /// <summary>Owning anatomy part (may be null for free-floating field bleeders).</summary>
        public AnatomyPart Owner { get; set; }

        private Renderer _renderer;
        private Material _material;
        private float _pulse;

        /// <summary>Effective millilitres per second escaping from this point right now.</summary>
        public float ActiveRate
        {
            get
            {
                if (IsSutured || IsCauterised)
                {
                    return 0f;
                }

                if (IsClamped)
                {
                    return rate * 0.05f;
                }

                return rate;
            }
        }

        public bool IsControlled => IsSutured || IsCauterised || IsClamped;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _material = MaterialLibrary.CreateInstance(new Color(0.55f, 0.05f, 0.08f), 0f, 0.7f);
                _renderer.sharedMaterial = _material;
            }
        }

        private void Update()
        {
            if (_renderer == null)
            {
                return;
            }

            if (IsControlled)
            {
                Color c = IsClamped && !IsSutured && !IsCauterised
                    ? new Color(0.45f, 0.5f, 0.55f)
                    : new Color(0.35f, 0.3f, 0.3f);
                MaterialLibrary.SetColor(_material, c);
                transform.localScale = Vector3.one * 0.012f;
                return;
            }

            // Gentle pulse so uncontrolled bleeders read at a glance.
            _pulse += Time.deltaTime * 4f;
            float s = 0.016f + Mathf.Sin(_pulse) * 0.004f * Mathf.Clamp01(rate / 4f);
            transform.localScale = Vector3.one * s;
            MaterialLibrary.SetColor(_material, new Color(0.65f, 0.06f, 0.09f));
        }

        public bool Clamp()
        {
            if (IsControlled)
            {
                return false;
            }

            IsClamped = true;
            return true;
        }

        public bool Cauterise()
        {
            if (IsSutured || IsCauterised)
            {
                return false;
            }

            // Cautery cannot seal a major vessel - it needs a clamp and a stitch.
            if (majorVessel)
            {
                return false;
            }

            IsCauterised = true;
            return true;
        }

        public bool Suture()
        {
            if (IsSutured)
            {
                return false;
            }

            IsSutured = true;
            IsClamped = false;
            return true;
        }

        /// <summary>Re-opens a bleeder (rough handling, clamp knocked off, blood pressure spike).</summary>
        public void Reopen(float rateMultiplier = 1f)
        {
            IsClamped = false;
            IsCauterised = false;
            IsSutured = false;
            rate *= rateMultiplier;
        }

        /// <summary>Factory used by the anatomy builder and injury systems.</summary>
        public static BleedingPoint Spawn(Transform parent, Vector3 localPosition, float rate, bool majorVessel = false)
        {
            GameObject go = PrimitiveFactory.Create(
                PrimitiveType.Sphere,
                "BleedingPoint",
                parent,
                localPosition,
                Vector3.one * 0.016f,
                null,
                false);

            var bp = go.AddComponent<BleedingPoint>();
            bp.rate = rate;
            bp.majorVessel = majorVessel;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 1.6f;      // generous grab radius in local space
            col.isTrigger = true;

            return bp;
        }
    }
}
