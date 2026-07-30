using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Tools;
using TraumaSurgeon.Visuals;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>Tracks holes drilled into a specific bone.</summary>
    public class BoneWorkState : MonoBehaviour
    {
        public readonly List<Vector3> PilotHoles = new List<Vector3>();
        public float SawProgress;
        public float DrillProgress;
        public float AlignProgress;
        public bool IsAligned;
        public bool PlateInstalled;
        public int ScrewsInstalled;
        public int ScrewsRequired = 4;
        public float BurrHoleProgress;
    }

    /// <summary>
    /// Orthopaedic and neurosurgical bone work: drilling pilot holes and burr holes, sawing,
    /// aligning fractures and installing fixation hardware.
    /// </summary>
    public static class BoneWorkSystem
    {
        private const float PilotHoleSpacing = 0.03f;

        public static BoneWorkState GetState(AnatomyPart part)
        {
            if (part == null)
            {
                return null;
            }

            BoneWorkState state = part.GetComponent<BoneWorkState>();
            if (state == null)
            {
                state = part.gameObject.AddComponent<BoneWorkState>();
            }

            return state;
        }

        /// <summary>
        /// Drills into bone. Returns true when a hole is completed.
        /// Over-drilling past the far cortex damages what is underneath - critical for burr holes.
        /// </summary>
        public static bool Drill(ref ToolUseContext ctx, float ratePerSecond, out bool plunged)
        {
            plunged = false;
            if (!ctx.HasTarget || ctx.Target.layer != AnatomyLayer.Bone)
            {
                return false;
            }

            AnatomyPart bone = ctx.Target;
            BoneWorkState state = GetState(bone);

            bool isSkull = bone.partId == AnatomyIds.Skull;
            float rate = ratePerSecond * ctx.DeltaTime * Mathf.Lerp(0.45f, 1.15f, ctx.Quality);

            if (isSkull)
            {
                state.BurrHoleProgress = Mathf.Clamp01(state.BurrHoleProgress + rate);

                // Pressing on after the hole is through plunges into the brain.
                if (state.BurrHoleProgress >= 1f)
                {
                    if (!bone.isOpen)
                    {
                        bone.Open();
                        return true;
                    }

                    if (ctx.Pressure > 0.6f && Random.value < 0.5f * ctx.DeltaTime)
                    {
                        plunged = true;
                        AnatomyPart brain = ctx.Patient != null ? ctx.Patient.Body.Get(AnatomyIds.Brain) : null;
                        if (brain != null)
                        {
                            ctx.Patient.OrganDamage.ReportInjury(brain, 14f, "drill plunged through the skull", true);
                        }
                    }
                }

                return false;
            }

            // Long bone pilot holes: each new hole must be spaced from the last.
            foreach (Vector3 hole in state.PilotHoles)
            {
                if (Vector3.Distance(hole, ctx.Point) < PilotHoleSpacing)
                {
                    return false;   // already drilled here
                }
            }

            state.DrillProgress = Mathf.Clamp01(state.DrillProgress + rate);
            if (state.DrillProgress >= 1f)
            {
                state.DrillProgress = 0f;
                state.PilotHoles.Add(ctx.Point);
                SpawnHoleVisual(bone, bone.transform.InverseTransformPoint(ctx.Point));
                return true;
            }

            return false;
        }

        /// <summary>Sawing bone (sternotomy, debriding a shattered fragment).</summary>
        public static bool Saw(ref ToolUseContext ctx, float ratePerSecond, out bool overshoot)
        {
            overshoot = false;
            if (!ctx.HasTarget || ctx.Target.layer != AnatomyLayer.Bone)
            {
                return false;
            }

            AnatomyPart bone = ctx.Target;
            BoneWorkState state = GetState(bone);
            state.SawProgress += ratePerSecond * ctx.DeltaTime * Mathf.Lerp(0.4f, 1.2f, ctx.Quality);

            // A wandering saw hits whatever is behind the bone.
            if (ctx.Stability < 0.3f && Random.value < 0.5f * ctx.DeltaTime && ctx.Patient != null)
            {
                overshoot = true;
                foreach (AnatomyPart covered in bone.Covered)
                {
                    ctx.Patient.OrganDamage.ReportInjury(covered, 10f, "saw blade overshoot", true);
                    break;
                }
            }

            if (state.SawProgress >= 1f && !bone.isOpen)
            {
                bone.Open();
                return true;
            }

            return false;
        }

        /// <summary>Manual reduction: bringing fracture ends back into alignment.</summary>
        public static bool Align(ref ToolUseContext ctx, float ratePerSecond)
        {
            if (!ctx.HasTarget || ctx.Target.layer != AnatomyLayer.Bone)
            {
                return false;
            }

            BoneWorkState state = GetState(ctx.Target);
            if (state.IsAligned)
            {
                return false;
            }

            state.AlignProgress = Mathf.Clamp01(
                state.AlignProgress + ratePerSecond * ctx.DeltaTime * ctx.Quality);

            if (state.AlignProgress >= 1f)
            {
                state.IsAligned = true;
                ctx.Target.state = DamageState.Repaired;
                ctx.Target.RefreshVisual();
                return true;
            }

            return false;
        }

        /// <summary>Places the fixation plate. Requires the fracture to be aligned first.</summary>
        public static bool InstallPlate(ref ToolUseContext ctx, out string failReason)
        {
            failReason = string.Empty;
            if (!ctx.HasTarget || ctx.Target.layer != AnatomyLayer.Bone)
            {
                failReason = "not a bone";
                return false;
            }

            BoneWorkState state = GetState(ctx.Target);
            if (!state.IsAligned)
            {
                failReason = "fracture is not reduced";
                return false;
            }

            if (state.PlateInstalled)
            {
                failReason = "plate already installed";
                return false;
            }

            state.PlateInstalled = true;
            PrimitiveFactory.Create(
                PrimitiveType.Cube,
                "FixationPlate",
                ctx.Target.transform,
                ctx.Target.MeshRoot != null ? ctx.Target.MeshRoot.localPosition + Vector3.up * 0.02f : Vector3.zero,
                new Vector3(0.02f, 0.006f, 0.16f),
                MaterialLibrary.Steel,
                false);
            return true;
        }

        /// <summary>Drives one screw through the plate into a pilot hole.</summary>
        public static bool InstallScrew(ref ToolUseContext ctx, out string failReason)
        {
            failReason = string.Empty;
            if (!ctx.HasTarget)
            {
                failReason = "no target";
                return false;
            }

            BoneWorkState state = GetState(ctx.Target);
            if (!state.PlateInstalled)
            {
                failReason = "no plate to screw into";
                return false;
            }

            if (state.ScrewsInstalled >= state.ScrewsRequired)
            {
                failReason = "all screws placed";
                return false;
            }

            if (state.PilotHoles.Count <= state.ScrewsInstalled)
            {
                failReason = "drill a pilot hole first";
                return false;
            }

            state.ScrewsInstalled++;
            Vector3 local = ctx.Target.transform.InverseTransformPoint(state.PilotHoles[state.ScrewsInstalled - 1]);
            PrimitiveFactory.Create(
                PrimitiveType.Cylinder,
                "Screw" + state.ScrewsInstalled,
                ctx.Target.transform,
                local + Vector3.up * 0.012f,
                new Vector3(0.005f, 0.012f, 0.005f),
                MaterialLibrary.DarkSteel,
                false);

            if (state.ScrewsInstalled >= state.ScrewsRequired)
            {
                ctx.Target.isRepaired = true;
                ctx.Target.state = DamageState.Repaired;
                ctx.Target.RefreshVisual();
            }

            return true;
        }

        private static void SpawnHoleVisual(AnatomyPart bone, Vector3 localPosition)
        {
            PrimitiveFactory.Create(
                PrimitiveType.Cylinder,
                "PilotHole",
                bone.transform,
                localPosition,
                new Vector3(0.006f, 0.004f, 0.006f),
                MaterialLibrary.DarkSteel,
                false);
        }
    }
}
