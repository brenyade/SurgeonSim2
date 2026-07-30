using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Scoring;
using UnityEngine;

namespace TraumaSurgeon.Patient
{
    /// <summary>
    /// Bridges anatomy damage into physiology and score. Every accidental injury routed through
    /// <see cref="ReportInjury"/> creates bleeding, damages the organ and notifies scoring.
    /// Also accumulates infection risk while the body cavity is open.
    /// </summary>
    public class OrganDamageSystem : MonoBehaviour
    {
        private PatientController _patient;

        /// <summary>Injuries the player caused (as opposed to the presenting trauma).</summary>
        public readonly List<string> IatrogenicInjuries = new List<string>();

        /// <summary>Total tissue damage the player inflicted - used directly in scoring.</summary>
        public float AccidentalDamageTotal { get; private set; }

        public void Initialise(PatientController patient)
        {
            _patient = patient;
        }

        public void Tick(float dt, float difficultyScale)
        {
            VitalSigns v = _patient.Vitals;
            if (!v.isAlive)
            {
                return;
            }

            // Open cavity + time = contamination.
            if (_patient.IsCavityOpen)
            {
                float rate = 0.32f * difficultyScale;
                if (_patient.Medications != null && _patient.Medications.AntibioticsGiven)
                {
                    rate *= 0.45f;
                }

                if (_patient.RetainedItemCount > 0)
                {
                    rate *= 1.8f;
                }

                v.infectionRisk = Mathf.Min(100f, v.infectionRisk + rate * dt);
            }
        }

        /// <summary>
        /// Registers damage to a part. <paramref name="accidental"/> separates surgeon error from
        /// the injuries the patient arrived with.
        /// </summary>
        public void ReportInjury(AnatomyPart part, float amount, string cause, bool accidental)
        {
            if (part == null || part.isRemoved)
            {
                return;
            }

            float scaled = amount * part.injurySensitivity;
            part.ApplyDamage(scaled, cause);

            // Meaningful trauma opens a bleeder.
            if (scaled > 3f)
            {
                bool major = part.layer == AnatomyLayer.Vessel;
                part.SpawnBleeder(Random.insideUnitSphere * 0.04f + Vector3.up * 0.02f,
                    Mathf.Clamp(scaled * 0.18f, 0.4f, 9f), major);
            }

            if (accidental)
            {
                AccidentalDamageTotal += scaled;
                IatrogenicInjuries.Add($"{part.displayName}: {cause} ({Mathf.RoundToInt(scaled)} dmg)");
                GameEvents.RaiseScoreEvent(ScoreEventType.TissueDamage, scaled, $"{part.displayName} - {cause}");

                if (scaled > 12f)
                {
                    GameEvents.RaiseNotification($"Accidental injury: {part.displayName}", NotificationType.Warning);
                    if (part.layer == AnatomyLayer.Vessel)
                    {
                        GameEvents.RaiseComplicationStarted(ComplicationType.AccidentalVesselInjury,
                            $"Vessel injury - {part.displayName}");
                    }
                    else if (part.layer == AnatomyLayer.Organ)
                    {
                        GameEvents.RaiseComplicationStarted(ComplicationType.AccidentalOrganPerforation,
                            $"Organ perforation - {part.displayName}");
                    }
                }
            }

            // Critical organ damage has immediate physiological consequences.
            switch (part.partId)
            {
                case AnatomyIds.Heart:
                    if (part.integrity < 40f)
                    {
                        _patient.TriggerCardiacArrest("Cardiac injury");
                    }

                    break;
                case AnatomyIds.LungLeft:
                case AnatomyIds.LungRight:
                    _patient.AddPneumothorax(scaled * 0.01f);
                    break;
                case AnatomyIds.Brain:
                    _patient.Vitals.consciousness = ConsciousnessLevel.Unconscious;
                    break;
            }
        }

        /// <summary>Applies a scripted injury from a procedure's JSON definition.</summary>
        public void ApplyPresentingInjury(AnatomyPart part, DamageState state, float severity, float bleedRate,
            bool requiresRepair, bool requiresRemoval)
        {
            if (part == null)
            {
                return;
            }

            part.state = state;
            part.integrity = Mathf.Clamp(100f - severity, 0f, 100f);
            part.requiresRepair = requiresRepair;
            part.requiresRemoval = requiresRemoval;
            part.RefreshVisual();

            if (bleedRate > 0f)
            {
                bool major = part.layer == AnatomyLayer.Vessel;
                int points = bleedRate > 5f ? 2 : 1;
                for (int i = 0; i < points; i++)
                {
                    part.SpawnBleeder(Random.insideUnitSphere * 0.03f + Vector3.up * 0.02f,
                        bleedRate / points, major);
                }
            }
        }
    }
}
