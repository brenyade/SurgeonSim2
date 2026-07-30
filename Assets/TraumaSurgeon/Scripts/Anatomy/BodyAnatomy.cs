using System.Collections.Generic;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Anatomy
{
    /// <summary>
    /// Registry and query surface for a patient's anatomy. Built at runtime by
    /// <see cref="AnatomyBuilder"/> and owned by the PatientController.
    /// </summary>
    public class BodyAnatomy : MonoBehaviour
    {
        private readonly Dictionary<string, AnatomyPart> _parts = new Dictionary<string, AnatomyPart>();
        private readonly List<AnatomyPart> _ordered = new List<AnatomyPart>();
        private readonly List<GameObject> _foreignObjects = new List<GameObject>();

        public IReadOnlyList<AnatomyPart> AllParts => _ordered;
        public IReadOnlyList<GameObject> ForeignObjects => _foreignObjects;

        public void Register(AnatomyPart part)
        {
            if (part == null || string.IsNullOrEmpty(part.partId))
            {
                return;
            }

            _parts[part.partId] = part;
            _ordered.Add(part);
        }

        public void RegisterForeignObject(GameObject go)
        {
            if (go != null && !_foreignObjects.Contains(go))
            {
                _foreignObjects.Add(go);
            }
        }

        public void RemoveForeignObject(GameObject go)
        {
            _foreignObjects.Remove(go);
        }

        public AnatomyPart Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return _parts.TryGetValue(id, out AnatomyPart part) ? part : null;
        }

        public bool Has(string id) => !string.IsNullOrEmpty(id) && _parts.ContainsKey(id);

        /// <summary>Sum of every uncontrolled bleeder on the patient (ml/sec).</summary>
        public float TotalBleedRate
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _ordered.Count; i++)
                {
                    total += _ordered[i].BleedRate;
                }

                return total;
            }
        }

        public int UncontrolledBleederCount
        {
            get
            {
                int count = 0;
                foreach (AnatomyPart part in _ordered)
                {
                    foreach (BleedingPoint bp in part.BleedingPoints)
                    {
                        if (bp != null && !bp.IsControlled)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        /// <summary>Average lung integrity 0..1 - drives oxygen saturation.</summary>
        public float LungFunction
        {
            get
            {
                AnatomyPart l = Get(AnatomyIds.LungLeft);
                AnatomyPart r = Get(AnatomyIds.LungRight);
                float total = 0f;
                int count = 0;
                if (l != null && !l.isRemoved) { total += l.integrity; count++; }
                if (r != null && !r.isRemoved) { total += r.integrity; count++; }
                if (count == 0)
                {
                    return 0.2f;
                }

                return Mathf.Clamp01(total / (count * 100f));
            }
        }

        /// <summary>Cardiac integrity 0..1.</summary>
        public float HeartFunction
        {
            get
            {
                AnatomyPart h = Get(AnatomyIds.Heart);
                return h == null ? 1f : Mathf.Clamp01(h.integrity / 100f);
            }
        }

        public void HighlightAll(bool active)
        {
            foreach (AnatomyPart part in _ordered)
            {
                part.SetHighlight(active);
            }
        }

        public void ClearHighlights()
        {
            foreach (AnatomyPart part in _ordered)
            {
                part.SetHighlight(false);
            }
        }

        public void Highlight(string partId, bool active)
        {
            AnatomyPart part = Get(partId);
            if (part != null)
            {
                part.SetHighlight(active);
            }
        }

        public void SetGuidesVisible(bool visible)
        {
            foreach (IncisionGuide guide in GetComponentsInChildren<IncisionGuide>(true))
            {
                guide.SetVisible(visible);
            }
        }

        /// <summary>All parts at or beneath a given layer that are currently reachable.</summary>
        public List<AnatomyPart> GetParts(AnatomyLayer layer)
        {
            var list = new List<AnatomyPart>();
            foreach (AnatomyPart part in _ordered)
            {
                if (part.layer == layer)
                {
                    list.Add(part);
                }
            }

            return list;
        }

        /// <summary>Parts still needing surgical attention - used by the objective/scoring systems.</summary>
        public List<AnatomyPart> GetOutstandingInjuries()
        {
            var list = new List<AnatomyPart>();
            foreach (AnatomyPart part in _ordered)
            {
                if (part.isRemoved)
                {
                    continue;
                }

                bool needsRepair = part.requiresRepair && !part.isRepaired;
                bool needsRemoval = part.requiresRemoval;
                if (needsRepair || needsRemoval || part.HasUncontrolledBleeding)
                {
                    list.Add(part);
                }
            }

            return list;
        }
    }
}
