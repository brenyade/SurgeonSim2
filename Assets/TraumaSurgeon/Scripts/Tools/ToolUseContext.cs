using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Patient;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Everything a tool needs to know about one frame of use: what it is pointing at, how steady
    /// the hand is, how hard the player is pressing and at what angle.
    /// </summary>
    public struct ToolUseContext
    {
        public PatientController Patient;

        /// <summary>Anatomy under the tool tip, if any.</summary>
        public AnatomyPart Target;

        /// <summary>Nearest uncontrolled bleeder to the tip, if any.</summary>
        public BleedingPoint Bleeder;

        /// <summary>World space contact point.</summary>
        public Vector3 Point;

        /// <summary>Surface normal at the contact point.</summary>
        public Vector3 Normal;

        /// <summary>Direction the tool is pointing.</summary>
        public Vector3 Forward;

        /// <summary>Collider the tool tip is touching.</summary>
        public Collider HitCollider;

        /// <summary>0..1 hand steadiness (tremor, movement, steady-hand mode, fatigue).</summary>
        public float Stability;

        /// <summary>0..1 how long the button has been held - some tools need sustained pressure.</summary>
        public float Pressure;

        /// <summary>0..1 how square the tool is to the tissue surface. 1 = ideal approach angle.</summary>
        public float AngleQuality;

        /// <summary>0..1 how clear the surgical field is (blood pooling reduces this).</summary>
        public float FieldClarity;

        /// <summary>True when the alternate (right mouse) function was used.</summary>
        public bool Secondary;

        /// <summary>Frame delta for continuous tools.</summary>
        public float DeltaTime;

        public bool HasTarget => Target != null;

        /// <summary>
        /// Combined 0..1 quality of this application. Precision is rewarded but never required:
        /// the floor is deliberately non-zero so a shaky hand still makes progress.
        /// </summary>
        public float Quality
        {
            get
            {
                float q = Stability * 0.5f + AngleQuality * 0.3f + FieldClarity * 0.2f;
                return Mathf.Clamp01(Mathf.Lerp(0.25f, 1f, q));
            }
        }
    }
}
