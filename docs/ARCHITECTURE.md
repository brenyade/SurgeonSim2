# Architecture

## Shape of the thing

One authored scene (`Bootstrap.unity`) containing one component (`Bootstrap`). Everything else is
built at runtime. Systems talk through a static event bus rather than inspector references, so no
system holds a hard reference to another unless it owns it.

```
Bootstrap  (DontDestroyOnLoad)
└── Managers
    ├── InputManager        polls classic Input, owns rebindable bindings
    ├── SaveManager         JSON slots + rolling backups
    ├── SettingsManager     loads settings, pushes them into input/audio/quality
    ├── AudioManager        procedural clips, voice pool, monitor + alarm logic
    ├── CareerManager       XP, rank, money, reputation, unlocks, case history
    ├── HospitalManager     upgrades, staff, research, surgical schedule
    ├── GameManager         phase machine; owns the OR, the player rig and the current surgery
    └── UIManager           canvases + 11 screens, reacts to phase changes
```

Manager creation order in `Bootstrap.CreateManagers()` matters: input before settings (settings
pushes bindings into it), save before career (career reads the active slot), UI last (it queries
everything else).

## Per-case object graph

`GameManager.BeginSurgery()` builds the world, then `SurgeryManager` builds the case:

```
GameManager
├── OperatingRoomBuilder      room shell, table, lights, machines, monitors, staff stations
├── PlayerRigBuilder.Rig      CharacterController + camera + HandAnchor
│   ├── FirstPersonController movement, look, steady-hand mode
│   ├── HandController        tremor, raycast, builds ToolUseContext, drives the tool
│   ├── ToolController        tray, hotkeys, equip/return, lazy tool instantiation
│   ├── PlayerInteractor      E-key stations
│   └── CameraFocus           depth-based focus (FOV approximation)
└── SurgeryManager            one operation, start to report
    ├── PatientController     owns vitals + anatomy + physiological subsystems
    │   ├── BodyAnatomy       registry of AnatomyPart, built by AnatomyBuilder
    │   ├── VitalSignsSystem  computes HR/BP/SpO2/RR/temp each tick
    │   ├── BloodLossSystem   aggregates bleeders, pooling, transfusion
    │   ├── AnesthesiaSystem  depth, too light / too deep consequences
    │   ├── MedicationSystem  drug catalogue, dose windows, effects
    │   └── OrganDamageSystem injury → physiology + score, infection accumulation
    ├── ObjectiveManager      the ordered checklist for the procedure
    ├── ComplicationManager   state-driven complication triggers and escalation
    ├── ScoringManager        tallies score events, builds the SurgeryReport
    ├── SpongeTracker         sponge count in/out, retained-item penalty
    ├── StaffCommandSystem    routes orders to the five StaffAIControllers
    └── LaparoscopySystem     scope camera + RenderTexture feed
```

## The one mechanic everything hangs off

Every meaningful surgical act funnels through a single event:

```csharp
GameEvents.RaiseActionPerformed(SurgicalActionType action, AnatomyPart target, float quality);
```

- **Tools** raise it after a successful application (`SurgicalToolBase.ReportAction`).
- **ObjectiveManager** listens and advances the checklist when the verb, the target and the
  required instrument all match the current step.
- **ScoringManager** listens for the precision value.
- **ComplicationManager** listens (via `SurgeryManager`) so the right treatment clears the right
  complication.

Adding a new surgical verb means adding one `SurgicalActionType` and raising it from a tool. Nothing
else needs to know.

## Tool pipeline

```
HandController.Update
  → UpdateStability()          tremor from difficulty, movement, fatigue; Shift damps it
  → ApplyTremor()              perlin-noise offset/rotation on the HandAnchor
  → BuildContext()             ray from camera aimed through the (trembling) tool tip
      ToolUseContext { Target, Bleeder, Point, Normal, Stability, Pressure,
                       AngleQuality, FieldClarity, DeltaTime }
  → tool.UsePrimary(ref ctx) / UseSecondary(ref ctx)
      → a Surgery.*System applies the rules (CuttingSystem, SuturingSystem, ...)
      → tool reports the action, or reports misuse
```

`ToolUseContext.Quality` blends stability, approach angle and field clarity, with a deliberate
non-zero floor (`Mathf.Lerp(0.25f, 1f, q)`): a shaky hand still makes progress, a steady one makes
better progress. Precision is rewarded, never required.

Misuse is handled uniformly by `SurgicalToolBase.ReportMisuse` — collateral tissue damage scaled by
the tool's `tissueDamageOnMisuse`, a `WrongTool` score event, and a notification quoting the tool's
`misuseConsequence` string from `tools.json`.

## Anatomy

An `AnatomyPart` is an unscaled root object with the visible mesh on a child, so bleeding points,
clamps, stitches and guide dots can be attached without inheriting a non-uniform scale.

Layers gate depth: `blocksAccess` parts (skin, fat, muscle, bone) disable the colliders of everything
in their `Covered` list until they are opened. `Open()` disables the layer's own colliders and
enables the layer beneath, which is what makes "work down through the layers" a real mechanic rather
than a scripted sequence.

`BleedingPoint` is the unit of haemorrhage: `rate` ml/sec, controllable by clamping (temporary,
5% leak), cautery (permanent, refused on major vessels) or a stitch (permanent). `BloodLossSystem`
sums every active bleeder each tick, scales it by mean arterial pressure — a hypotensive patient
bleeds more slowly — and subtracts from blood volume.

## Physiology

`VitalSignsSystem` computes *target* values from state and eases the displayed vitals toward them,
so the monitor moves believably instead of snapping. The causal chains the brief asks for fall out
of this naturally:

| Cause | Path |
| --- | --- |
| Uncontrolled bleeding → BP falls | bleeders → blood volume → perfusion → systolic target |
| Blood loss → HR rises | `hrTarget += (1 - bloodFraction) * 190` |
| Lung injury → SpO2 falls | `BodyAnatomy.LungFunction` → `spo2Target` |
| Excess anaesthesia → respiratory arrest | `AnesthesiaSystem` depth > 92 and RR < 4 |
| Insufficient anaesthesia → pain, tachycardia | depth < 55 raises pain, HR and BP |
| Accidental organ injury → new bleeding | `OrganDamageSystem.ReportInjury` spawns a bleeder |
| Long surgery → infection risk | open cavity accumulates risk; antibiotics halve the rate |

## Complications are consequences, not dice

`ComplicationManager` evaluates every three seconds. Each complication in `complications.json` lists
`triggerTags`; each tag maps to a multiplier computed from live game state
(`highBloodLoss`, `activeBleeding`, `lowOxygen`, `deepAnesthesia`, `openCavity`, `retainedItem`,
`faultyEquipment`, `longSurgery`, ...). A tag whose condition is absent contributes **zero**, so a
complication with no active trigger cannot fire at all. Randomness only decides *when* within a
window the game state has already opened.

Complications that are ignored escalate on a timer (`timeToCriticalSeconds`) into something worse —
uncontrolled haemorrhage becomes an arrest, a tension pneumothorax becomes an arrest.

Player-caused complications (vessel injury, organ perforation, surgical fire, medication error) are
raised directly by the system that detects them, with `baseChancePerMinute: 0` so they never fire
spontaneously.

## Scoring

`ScoringManager` subscribes to `GameEvents.ScoreEvent` for the whole case and builds a
`SurgeryReport` at the end with thirteen scored lines (survival 300, completion 150, stability 120,
blood loss 120, accuracy 110, tissue preservation 90, tool discipline 60, time 60, complication
management 60, infection 50, economy of motion 40, medication 40, instrument count 40). The total is
multiplied by the difficulty's `ScoreMultiplier`, then banded into S/A/B/C/D/F. Death or more than
one retained item is an automatic F.

## Data

`DataLibrary` loads six JSON files from `Resources/TraumaSurgeonData` with `JsonUtility` and fails
soft — a missing or malformed file logs and yields an empty database so the game still boots. Enum
fields are stored as strings in JSON and parsed lazily (`EnumParse.To<T>`), which keeps the data
human-editable and tolerant of unknown values.

## Save format

`SaveManager` writes with `JsonUtility.ToJson(value, true)` to `<path>.tmp`, copies the existing file
to `<path>.bak`, then moves the temp into place. Reads fall back to `.bak` on any exception or empty
payload. Refuses to write an empty payload, so a serialisation bug cannot wipe a career.
