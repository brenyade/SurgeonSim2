using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Scoring;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>A sponge that has been packed into the patient.</summary>
    public class PackedSponge : MonoBehaviour
    {
        public AnatomyPart Host;
        public float SoakedMl;
    }

    /// <summary>
    /// Counts sponges in and out. Closing with a sponge still inside is one of the harshest
    /// penalties in the scoring system, exactly as it is in real theatre practice.
    /// </summary>
    public class SpongeTracker : MonoBehaviour
    {
        public int SpongesIssued { get; private set; }
        public int SpongesRemoved { get; private set; }

        private readonly List<PackedSponge> _packed = new List<PackedSponge>();

        public int SpongesInside => _packed.Count;

        public bool CountIsCorrect => _packed.Count == 0;

        public PatientController Patient { get; set; }

        /// <summary>Packs a sponge into a part - controls bleeding temporarily.</summary>
        public PackedSponge Pack(AnatomyPart part, Vector3 worldPoint)
        {
            if (part == null)
            {
                return null;
            }

            SpongesIssued++;

            Vector3 local = part.transform.InverseTransformPoint(worldPoint);
            GameObject go = PrimitiveFactory.Create(
                PrimitiveType.Cube,
                "Sponge",
                part.transform,
                local,
                new Vector3(0.05f, 0.012f, 0.05f),
                MaterialLibrary.Get("sponge", new Color(0.95f, 0.95f, 0.92f)),
                true);

            var sponge = go.AddComponent<PackedSponge>();
            sponge.Host = part;
            _packed.Add(sponge);

            // Packing tamponades bleeders in that part.
            foreach (BleedingPoint bp in part.BleedingPoints)
            {
                if (bp != null && !bp.IsControlled)
                {
                    bp.Clamp();
                }
            }

            UpdatePatientCount();
            return sponge;
        }

        /// <summary>Removes a specific packed sponge.</summary>
        public bool Remove(PackedSponge sponge)
        {
            if (sponge == null || !_packed.Contains(sponge))
            {
                return false;
            }

            _packed.Remove(sponge);
            SpongesRemoved++;

            // Pulling packing off can restart the bleeding it was controlling.
            if (sponge.Host != null && Random.value < 0.45f)
            {
                foreach (BleedingPoint bp in sponge.Host.BleedingPoints)
                {
                    if (bp != null && bp.IsClamped && !bp.IsSutured && !bp.IsCauterised)
                    {
                        bp.Reopen();
                        break;
                    }
                }
            }

            Destroy(sponge.gameObject);
            UpdatePatientCount();
            return true;
        }

        /// <summary>Simulates the circulating nurse's sponge count.</summary>
        public string PerformCount()
        {
            if (CountIsCorrect)
            {
                GameEvents.RaiseStaffSpeech(StaffRole.CirculatingNurse,
                    $"Sponge count is correct - {SpongesIssued} in, {SpongesRemoved} out.");
                return "Count correct";
            }

            GameEvents.RaiseStaffSpeech(StaffRole.CirculatingNurse,
                $"Count is off by {_packed.Count}. We are missing a sponge.");
            GameEvents.RaiseComplicationStarted(ComplicationType.RetainedSponge,
                $"{_packed.Count} sponge(s) unaccounted for");
            return $"Count incorrect - {_packed.Count} missing";
        }

        /// <summary>Called at closure to bake retained items into the score.</summary>
        public void FinaliseAtClosure()
        {
            if (_packed.Count > 0)
            {
                GameEvents.RaiseScoreEvent(ScoreEventType.RetainedItem, _packed.Count,
                    $"{_packed.Count} sponge(s) left inside the patient");
            }
        }

        private void UpdatePatientCount()
        {
            if (Patient != null)
            {
                Patient.RetainedItemCount = _packed.Count;
            }
        }
    }
}
