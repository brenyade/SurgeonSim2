using System.Collections.Generic;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Anatomy
{
    /// <summary>
    /// The dotted line the player is supposed to cut along. Doubles as the precision oracle:
    /// the cutting system scores how close the blade stayed to the path.
    /// </summary>
    public class IncisionGuide : MonoBehaviour
    {
        public readonly List<Transform> Points = new List<Transform>();

        /// <summary>Distance in metres beyond which an incision counts as off-path.</summary>
        public float tolerance = 0.045f;

        private bool _visible = true;

        public void SetVisible(bool visible)
        {
            _visible = visible;
            foreach (Transform t in Points)
            {
                if (t != null)
                {
                    t.gameObject.SetActive(visible);
                }
            }
        }

        public bool Visible => _visible;

        /// <summary>Shortest distance from a world point to the guide path.</summary>
        public float DistanceTo(Vector3 worldPoint)
        {
            float best = float.MaxValue;
            for (int i = 0; i < Points.Count; i++)
            {
                if (Points[i] == null)
                {
                    continue;
                }

                float d = Vector3.Distance(Points[i].position, worldPoint);
                if (d < best)
                {
                    best = d;
                }
            }

            return best == float.MaxValue ? 999f : best;
        }

        /// <summary>0..1 accuracy where 1 means dead on the line.</summary>
        public float Accuracy(Vector3 worldPoint)
        {
            float d = DistanceTo(worldPoint);
            return Mathf.Clamp01(1f - d / Mathf.Max(0.001f, tolerance * 2.5f));
        }

        /// <summary>Marks the nearest dot as cut so the player can see progress along the line.</summary>
        public void MarkProgress(Vector3 worldPoint)
        {
            float best = tolerance * 1.5f;
            Transform bestT = null;
            foreach (Transform t in Points)
            {
                if (t == null || !t.gameObject.activeSelf)
                {
                    continue;
                }

                float d = Vector3.Distance(t.position, worldPoint);
                if (d < best)
                {
                    best = d;
                    bestT = t;
                }
            }

            if (bestT != null)
            {
                bestT.gameObject.SetActive(false);
            }
        }

        /// <summary>Builds a straight dotted guide from A to B in the parent's local space.</summary>
        public static IncisionGuide Create(Transform parent, Vector3 localStart, Vector3 localEnd, int dots = 9)
        {
            GameObject root = PrimitiveFactory.Empty("IncisionGuide", parent);
            var guide = root.AddComponent<IncisionGuide>();

            for (int i = 0; i < dots; i++)
            {
                float t = dots <= 1 ? 0f : (float)i / (dots - 1);
                Vector3 pos = Vector3.Lerp(localStart, localEnd, t);
                GameObject dot = PrimitiveFactory.Create(
                    PrimitiveType.Cube,
                    "Dot" + i,
                    root.transform,
                    pos,
                    new Vector3(0.012f, 0.004f, 0.012f),
                    MaterialLibrary.GuideLine,
                    false);
                guide.Points.Add(dot.transform);
            }

            return guide;
        }
    }
}
