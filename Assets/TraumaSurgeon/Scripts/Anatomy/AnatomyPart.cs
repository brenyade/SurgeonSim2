using System.Collections.Generic;
using TraumaSurgeon.Core;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Anatomy
{
    /// <summary>
    /// One recognisable piece of anatomy (skin flap, liver, rib, femur...).
    /// The root object is unscaled; the visible mesh lives on a child so bleeding points and
    /// guide markers can be attached without inheriting a non-uniform scale.
    /// </summary>
    public class AnatomyPart : MonoBehaviour
    {
        [Header("Identity")]
        public string partId = "part";
        public string displayName = "Tissue";
        public AnatomyLayer layer = AnatomyLayer.Organ;

        [Header("State")]
        public DamageState state = DamageState.Healthy;
        [Range(0f, 100f)] public float integrity = 100f;
        public bool canBeRemoved;
        public bool requiresRepair;
        public bool requiresRemoval;
        public bool isRemoved;
        public bool isRepaired;

        [Header("Access")]
        [Tooltip("Layers like skin/fat/muscle hide what is underneath until they are opened.")]
        public bool blocksAccess;
        [Range(0f, 1f)] public float incisionProgress;
        public bool isOpen;

        [Tooltip("Tissue resistance 0..1 - bone is hard, fat is soft. Affects cutting speed.")]
        [Range(0.05f, 1f)] public float resistance = 0.4f;

        [Tooltip("How much score/vitals damage an accidental hit on this part causes.")]
        public float injurySensitivity = 1f;

        [Tooltip("Explicit highlight box size. Leave zero to derive it from the mesh scale.")]
        public Vector3 highlightSize = Vector3.zero;

        [Tooltip("Explicit highlight centre. Needed for parts whose mesh root is an empty group.")]
        public Vector3 highlightOffset = Vector3.zero;

        [Header("Repair")]
        [Range(0f, 100f)] public float repairProgress;
        public int suturesRequired = 4;
        public int suturesPlaced;

        public readonly List<BleedingPoint> BleedingPoints = new List<BleedingPoint>();

        /// <summary>Parts hidden behind this one - revealed once it is opened or retracted.</summary>
        public readonly List<AnatomyPart> Covered = new List<AnatomyPart>();

        public Transform MeshRoot { get; private set; }

        private Renderer[] _renderers;
        private Material _material;
        private Color _baseColor;
        private GameObject _highlight;
        private bool _highlightActive;
        private float _retraction;

        public bool IsRetracted => _retraction > 0.5f;

        /// <summary>Total uncontrolled blood loss from this part in ml/sec.</summary>
        public float BleedRate
        {
            get
            {
                if (isRemoved)
                {
                    return 0f;
                }

                float total = 0f;
                for (int i = 0; i < BleedingPoints.Count; i++)
                {
                    if (BleedingPoints[i] != null)
                    {
                        total += BleedingPoints[i].ActiveRate;
                    }
                }

                return total;
            }
        }

        public bool HasUncontrolledBleeding
        {
            get
            {
                for (int i = 0; i < BleedingPoints.Count; i++)
                {
                    if (BleedingPoints[i] != null && !BleedingPoints[i].IsControlled)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Called by the builder once the mesh child exists.</summary>
        public void Initialise(Transform meshRoot, Color baseColor)
        {
            MeshRoot = meshRoot;
            _baseColor = baseColor;
            _renderers = GetComponentsInChildren<Renderer>();
            _material = MaterialLibrary.CreateInstance(baseColor, 0f, 0.35f);
            foreach (Renderer r in _renderers)
            {
                r.sharedMaterial = _material;
            }

            RefreshVisual();
        }

        // ---- Interaction ------------------------------------------------------

        /// <summary>
        /// Cuts into the part. Returns true on the frame the part becomes open.
        /// <paramref name="quality"/> is 0..1 hand steadiness - poor quality causes collateral damage.
        /// </summary>
        public bool ApplyIncision(float amount, float quality)
        {
            if (isOpen || isRemoved)
            {
                return false;
            }

            float effective = amount * Mathf.Lerp(0.35f, 1f, quality) / Mathf.Max(0.05f, resistance);
            incisionProgress = Mathf.Clamp01(incisionProgress + effective);

            if (quality < 0.35f && Random.value < 0.02f)
            {
                // Ragged incision - a small bleeder opens up.
                SpawnBleeder(Random.insideUnitSphere * 0.05f, Random.Range(0.6f, 1.6f));
            }

            RefreshVisual();

            if (incisionProgress >= 1f)
            {
                Open();
                return true;
            }

            return false;
        }

        /// <summary>Opens the layer, revealing everything it covers.</summary>
        public void Open()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            incisionProgress = 1f;

            if (blocksAccess && MeshRoot != null)
            {
                // Placeholder "flap" effect: split and swing the layer aside.
                foreach (Collider c in GetComponentsInChildren<Collider>())
                {
                    if (c.GetComponent<BleedingPoint>() == null)
                    {
                        c.enabled = false;
                    }
                }

                MeshRoot.localScale = new Vector3(MeshRoot.localScale.x * 0.55f,
                    MeshRoot.localScale.y, MeshRoot.localScale.z);
                MeshRoot.localPosition += new Vector3(-0.09f, 0.005f, 0f);
            }

            foreach (AnatomyPart part in Covered)
            {
                if (part != null)
                {
                    part.SetAccessible(true);
                }
            }

            RefreshVisual();
        }

        /// <summary>Enables/disables colliders and renderers for the covered layer reveal.</summary>
        public void SetAccessible(bool accessible)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                c.enabled = accessible;
            }
        }

        /// <summary>Retraction holds a layer open, giving access to deeper structures.</summary>
        public void SetRetraction(float amount)
        {
            _retraction = Mathf.Clamp01(amount);
            if (MeshRoot != null)
            {
                MeshRoot.localPosition = new Vector3(
                    MeshRoot.localPosition.x,
                    MeshRoot.localPosition.y,
                    Mathf.Lerp(0f, 0.08f, _retraction));
            }
        }

        public void ApplyDamage(float amount, string cause = "")
        {
            if (isRemoved)
            {
                return;
            }

            integrity = Mathf.Clamp(integrity - amount, 0f, 100f);

            if (integrity < 25f)
            {
                state = DamageState.Ruptured;
            }
            else if (integrity < 55f)
            {
                state = DamageState.Perforated;
            }
            else if (integrity < 85f)
            {
                state = DamageState.Lacerated;
            }
            else if (integrity < 98f)
            {
                state = DamageState.Bruised;
            }

            isRepaired = false;
            RefreshVisual();
        }

        /// <summary>Applies repair progress. Returns true when the part becomes fully repaired.</summary>
        public bool ApplyRepair(float amount)
        {
            if (isRemoved || isRepaired)
            {
                return false;
            }

            repairProgress = Mathf.Clamp(repairProgress + amount, 0f, 100f);
            integrity = Mathf.Clamp(integrity + amount * 0.5f, 0f, 100f);

            if (repairProgress >= 100f)
            {
                isRepaired = true;
                state = DamageState.Repaired;
                StopAllBleeding();
                RefreshVisual();
                return true;
            }

            RefreshVisual();
            return false;
        }

        /// <summary>Places one stitch. Returns true when the closure is complete.</summary>
        public bool PlaceSuture()
        {
            suturesPlaced++;
            repairProgress = Mathf.Clamp01((float)suturesPlaced / Mathf.Max(1, suturesRequired)) * 100f;
            if (suturesPlaced >= suturesRequired)
            {
                isRepaired = true;
                state = DamageState.Repaired;
                StopAllBleeding();
                RefreshVisual();
                return true;
            }

            RefreshVisual();
            return false;
        }

        public void Remove()
        {
            if (isRemoved)
            {
                return;
            }

            isRemoved = true;
            state = DamageState.Removed;
            StopAllBleeding();

            if (MeshRoot != null)
            {
                MeshRoot.gameObject.SetActive(false);
            }

            foreach (Collider c in GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }

            SetHighlight(false);
        }

        public void StopAllBleeding()
        {
            foreach (BleedingPoint bp in BleedingPoints)
            {
                if (bp != null)
                {
                    bp.Suture();
                }
            }
        }

        /// <summary>
        /// Adds a bleeder. <paramref name="offset"/> is relative to the centre of the part's mesh,
        /// so callers never need to know where the part sits inside the body.
        /// </summary>
        public BleedingPoint SpawnBleeder(Vector3 offset, float rate, bool major = false)
        {
            Vector3 basePosition = highlightOffset.sqrMagnitude > 0.0001f
                ? highlightOffset
                : (MeshRoot != null ? MeshRoot.localPosition : Vector3.zero);
            Vector3 localPosition = basePosition + offset;
            BleedingPoint bp = BleedingPoint.Spawn(transform, localPosition, rate, major);
            bp.Owner = this;
            BleedingPoints.Add(bp);
            return bp;
        }

        /// <summary>
        /// Nearest bleeder to a world point, or null. By default only uncontrolled bleeders are
        /// returned, so a hemostat moves on to the next one instead of re-targeting a clamped
        /// vessel. Pass <paramref name="includeControlled"/> when converting a clamp into a stitch.
        /// </summary>
        public BleedingPoint FindNearestBleeder(Vector3 worldPoint, float maxDistance = 0.25f,
            bool includeControlled = false)
        {
            BleedingPoint best = null;
            float bestDist = maxDistance;
            foreach (BleedingPoint bp in BleedingPoints)
            {
                if (bp == null || bp.IsSutured || bp.IsCauterised)
                {
                    continue;
                }

                if (!includeControlled && bp.IsControlled)
                {
                    continue;
                }

                float d = Vector3.Distance(bp.transform.position, worldPoint);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = bp;
                }
            }

            return best;
        }

        // ---- Presentation -----------------------------------------------------

        public void SetHighlight(bool active)
        {
            if (_highlightActive == active)
            {
                return;
            }

            _highlightActive = active;

            if (active && _highlight == null && MeshRoot != null)
            {
                Vector3 size = highlightSize.sqrMagnitude > 0.0001f
                    ? highlightSize
                    : MeshRoot.localScale * 1.12f;

                Vector3 centre = highlightOffset.sqrMagnitude > 0.0001f
                    ? highlightOffset
                    : MeshRoot.localPosition;

                _highlight = PrimitiveFactory.Create(
                    PrimitiveType.Cube,
                    "Highlight",
                    transform,
                    centre,
                    size,
                    MaterialLibrary.Highlight,
                    false);
                _highlight.transform.localRotation = MeshRoot.localRotation;
            }

            if (_highlight != null)
            {
                _highlight.SetActive(active && !isRemoved);
            }
        }

        /// <summary>Recolours the placeholder mesh to communicate damage state.</summary>
        public void RefreshVisual()
        {
            if (_material == null)
            {
                return;
            }

            Color color = _baseColor;

            switch (state)
            {
                case DamageState.Bruised:
                    color = Color.Lerp(_baseColor, new Color(0.35f, 0.2f, 0.42f), 0.35f);
                    break;
                case DamageState.Lacerated:
                    color = Color.Lerp(_baseColor, new Color(0.5f, 0.08f, 0.1f), 0.4f);
                    break;
                case DamageState.Perforated:
                    color = Color.Lerp(_baseColor, new Color(0.42f, 0.05f, 0.07f), 0.6f);
                    break;
                case DamageState.Ruptured:
                    color = Color.Lerp(_baseColor, new Color(0.32f, 0.03f, 0.05f), 0.8f);
                    break;
                case DamageState.Repaired:
                    color = Color.Lerp(_baseColor, new Color(0.75f, 0.72f, 0.68f), 0.3f);
                    break;
            }

            if (incisionProgress > 0f && !isOpen)
            {
                color = Color.Lerp(color, new Color(0.6f, 0.1f, 0.12f), incisionProgress * 0.5f);
            }

            MaterialLibrary.SetColor(_material, color);
        }

        public string StatusLine()
        {
            if (isRemoved)
            {
                return $"{displayName}: removed";
            }

            if (isRepaired)
            {
                return $"{displayName}: repaired";
            }

            return $"{displayName}: {state} ({Mathf.RoundToInt(integrity)}%)";
        }
    }
}
