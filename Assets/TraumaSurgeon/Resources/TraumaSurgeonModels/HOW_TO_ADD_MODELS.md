# Dropping real character models in

The theatre team is built from primitives by `HumanoidFigureBuilder` so the project ships with no
binary art. Any figure can be replaced by a real model **without touching code**.

## How

Put a prefab in this folder named after the role:

```
Assets/TraumaSurgeon/Resources/TraumaSurgeonModels/
├── Staff_Anesthesiologist.prefab
├── Staff_ScrubNurse.prefab
├── Staff_CirculatingNurse.prefab
├── Staff_SurgicalAssistant.prefab
├── Staff_ConsultantSurgeon.prefab
└── Staff_Default.prefab          ← fallback used for any role with no specific prefab
```

`HumanoidFigureBuilder.Build` looks for `Staff_<Role>` first, then `Staff_Default`, and only builds
the primitive stand-in when neither exists.

## What the prefab needs

- Its origin at the **feet**, facing **+Z**, roughly **1.75 m** tall. It is instantiated at the
  staff station's position and rotation with no offset.
- No collider required — theatre staff are visual only and must not block the surgeon.
- If it has its own `Animator`, that is left to run; `StaffAIController` only applies a slow weight
  shift on top. Without an `Animator` the figure is static apart from that shift.

Anything humanoid works: Mixamo characters (free, CC-licensed), Unity's Starter Assets armature, or
your own rig. Keep to assets you are licensed to redistribute — this project ships no paid or
proprietary art.

## Going further

To drive a supplied rig properly, extend `HumanoidFigureBuilder.TryBuildFromPrefab` to populate the
`FigureRig` bone references (`Chest`, `Head`, `ShoulderL/R`, `ForearmL/R`) from the model's
`Animator.GetBoneTransform(HumanBodyBones.…)`. `StaffAIController.Animate` then drives a real rig
with the same breathing, head-tracking and chest-compression motion it applies to the placeholder.

The patient's anatomy is deliberately **not** swappable this way: organs are gameplay objects with
ids, layers, damage states and bleeding points. See `AnatomyBuilder` — replace the
`PrimitiveFactory.Create` calls there with prefab instantiation, keeping the ids and flags identical.
