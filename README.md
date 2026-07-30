# Trauma Surgeon

A first-person surgical simulation for PC, built in **Unity 6** with C#.

You play a surgeon in a hospital operating theatre. You review the chart and imaging, scrub in,
prep the patient, pick your instruments, cut, control bleeding, find and fix the damage, keep the
patient alive while their physiology fights you, close up, and get graded on all of it.

Eight full operations, a career ladder from medical student to chief of surgery, a training gym,
and challenge runs. Everything is built from Unity primitives and procedurally generated audio, so
the project has **zero paid or proprietary dependencies** and nothing to download beyond Unity itself.

---

## Quick start

1. Install **Unity 6** (6000.0.x or newer) via Unity Hub. The project also opens and builds on
   **Unity 2021.3 / 2022.3 LTS** — version-specific engine APIs are behind `#if` guards.
2. `git clone` this repository and add the folder to Unity Hub as an existing project, or use
   **Open → Add project from disk** and select the repository root.
3. Open the project. Unity will import the scripts and generate its own `Library/` folder
   (first import takes a minute or two).
4. Open `Assets/TraumaSurgeon/Scenes/Bootstrap.unity` — or use the menu
   **Trauma Surgeon → Open Bootstrap Scene**.
5. Press **Play**.

That's it. The main menu builds itself at runtime; there is nothing to wire up in the inspector.

> **Input backend.** The game uses Unity's classic `Input` API. If your Unity install defaults to
> *Input System Package (New)*, the console will warn you at load. Run
> **Trauma Surgeon → Fix Project Setup** (or set *Project Settings → Player → Active Input Handling*
> to **Both**) and let Unity restart.

> **Starting from another scene.** If you press Play from an empty or default scene, the game
> bootstraps itself anyway and stands down that scene's Main Camera and AudioListener so they don't
> fight with the surgeon rig. Opening `Bootstrap.unity` is still the clean path.

### Building for Windows

1. **File → Build Profiles** (or *Build Settings* on older UI), platform **Windows**.
2. Confirm `Assets/TraumaSurgeon/Scenes/Bootstrap.unity` is scene **0** in the build list —
   the editor script adds it automatically, and **Trauma Surgeon → Fix Project Setup** re-adds it.
3. **Build**, pick an output folder, run the `.exe`.

Target: Windows 10/11, x86-64. The game is deliberately light — primitive geometry, no baked
lighting, no post-processing stack — so it runs comfortably on integrated graphics at the Low preset.

---

## Controls

| Action | Default | Notes |
| --- | --- | --- |
| Move | `W` `A` `S` `D` | Slow, deliberate movement — this is a theatre |
| Look | Mouse | Sensitivity and invert-Y in Settings |
| Use tool | `Left Mouse` | Hold for continuous tools (scalpel, drill, suction, cautery) |
| Alternate function | `Right Mouse` | Every tool has a second mode |
| Interact | `E` | Scrub sink, prep zone, imaging display |
| Return tool | `Q` | Hands the instrument back to the scrub nurse |
| Select tool | `1` – `9` | `0` cycles tray pages when a case needs more than nine |
| Steady hand | `Left Shift` | Cuts tremor hard, drains stamina, slows you down |
| Patient chart | `Tab` | Live vitals, imaging, problem list, drug record |
| Command wheel | `C` | Orders to the OR team |
| Team shortcuts | `F1` – `F12` | Same orders without opening the wheel |
| Request assistance | `Space` | Consultant hints at the current step |
| Pause | `Escape` | |

Every binding is rebindable in **Settings → Key Bindings** (click an action, press the new key).

---

## The core loop

1. **Pick a case** — Career schedule, Play Now, Training or a Challenge.
2. **Briefing** — read the chart, look at the imaging, read the operative plan.
3. **Scrub in** (`E` at the sink) and **prep the patient** (`E` at the foot of the table).
   Prepping induces anaesthesia.
4. **Operate.** Follow the objective list top-left. Watch the monitor top-right.
5. **React.** Bleeding, arrests, pneumothoraces and equipment failures happen because of the state
   you have created, not on a timer.
6. **Close**, then read your **post-operation report** and rank (S through F).

### Difficulties

| | Student | Resident | Attending | Impossible Shift |
| --- | --- | --- | --- | --- |
| Guided incision paths | yes | yes | no | no |
| Highlighted anatomy | yes | yes | no | no |
| Hints on objectives | yes | yes | no | no |
| Assistance uses | unlimited | 6 | 3 | 1 |
| Deterioration rate | ×0.45 | ×1.0 | ×1.5 | ×2.2 |
| Complication rate | ×0.25 | ×0.7 | ×1.1 | ×1.8 |
| Manual drug dosing | no | no | yes | yes |
| Patient can die | no | yes | yes | yes |
| Score multiplier | ×0.7 | ×1.0 | ×1.35 | ×1.8 |

---

## What is in the box

**Eight operations**

| Case | Region | Unlocks at |
| --- | --- | --- |
| Acute Appendicitis | Abdomen | Medical Student |
| Collapsed Lung (chest tube) | Chest | Medical Student |
| Gunshot Wound to the Abdomen | Abdomen | Junior Resident |
| Ruptured Spleen | Abdomen | Junior Resident |
| Open Femur Fracture | Limb | Senior Resident |
| Coronary Artery Bypass | Chest | Attending Surgeon |
| Acute Subdural Hematoma | Head | Trauma Director |
| Multi-System Trauma | Whole body | Chief of Surgery |

**Twenty-one instruments**, each with a primary action, an alternate function, a distinct sound, a
use animation and a specific consequence for misuse: scalpel, trauma shears, forceps, hemostat,
retractor, suction, electrocautery, needle holder and sutures, skin stapler, rib spreader, bone saw,
surgical drill, fixation plate and screws, laparoscope, laparoscopic grasper, irrigation,
defibrillator, syringe, chest tube, surgical sponge, and your gloved hands (CPR).

**Fifteen complications**, all state-driven — uncontrolled bleeding, arrest, VF, respiratory arrest,
tension pneumothorax, anaphylaxis, equipment failure, anaesthetic overdose, pressure collapse,
iatrogenic vessel and organ injury, surgical fire, retained sponge, contamination and dosing errors.

**A five-person OR team** — anaesthetist, scrub nurse, circulating nurse, surgical assistant and a
consultant surgeon — who talk back and actually do what you ask (compressions, suction, retraction,
transfusion, sponge counts, damage-control closure).

**Ten training stations**, **six challenge runs**, career progression with money, reputation,
experience, malpractice risk and hospital ratings, plus hospital management (schedule, equipment,
staff training, research).

---

## Project layout

```
Assets/TraumaSurgeon/
├── Editor/                     Editor menu items, project setup, JSON validator
├── Resources/TraumaSurgeonData/
│   ├── tools.json              21 instruments
│   ├── procedures.json         8 operations with full step lists and patients
│   ├── complications.json      15 complications with their triggers
│   ├── training.json           10 training stations
│   ├── challenges.json         6 challenge runs
│   └── upgrades.json           Equipment / research / room upgrades
├── Scenes/Bootstrap.unity      The only authored scene
└── Scripts/
    ├── Anatomy/                AnatomyPart, BleedingPoint, BodyAnatomy, AnatomyBuilder, IncisionGuide
    ├── Audio/                  AudioManager, ProceduralAudio (all clips generated in code)
    ├── Career/                 CareerManager, HospitalManager
    ├── Complications/          ComplicationManager
    ├── Core/                   Bootstrap, GameManager, GameEvents, Settings, difficulty profiles
    ├── Data/                   JSON models and the DataLibrary loader
    ├── Environment/            OperatingRoomBuilder
    ├── Input/                  InputManager, GameAction
    ├── Modes/                  TrainingManager
    ├── Patient/                PatientController, VitalSignsSystem, BloodLossSystem,
    │                           AnesthesiaSystem, MedicationSystem, OrganDamageSystem
    ├── Player/                 FirstPersonController, HandController, PlayerInteractor, CameraFocus
    ├── Save/                   SaveManager, SaveData (JSON + rolling backups)
    ├── Scoring/                ScoringManager, SurgeryReport
    ├── Staff/                  StaffAIController, StaffCommandSystem
    ├── Surgery/                SurgeryManager, ObjectiveManager, CuttingSystem, SuturingSystem,
    │                           BoneWorkSystem, hemostasis/critical-care systems, SpongeTracker
    ├── Tools/                  SurgicalToolBase, ToolController, ToolFactory, 21 implementations
    ├── UI/                     UIManager, UIFactory, UITheme and 11 screens
    └── Visuals/                MaterialLibrary, PrimitiveFactory, ImagingGenerator
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for how the systems fit together and
[`docs/TESTING.md`](docs/TESTING.md) for a per-phase test script.

### Everything is built at runtime

There is exactly one authored scene, containing one GameObject with `Bootstrap` on it. The operating
theatre, the patient's anatomy, the instruments, the staff and the entire UI are constructed in code
at runtime from Unity primitives and generated materials. That means:

- no binary assets to merge or diff — the whole game is reviewable as text;
- no missing-reference breakage when scripts are renamed;
- swapping in real art is a localised change (replace the `PrimitiveFactory` call sites in
  `AnatomyBuilder`, `ToolFactory` and `OperatingRoomBuilder` with prefab instantiation — the ids,
  layers and gameplay flags stay identical).

Audio works the same way: `ProceduralAudio` synthesises every cue (monitor beeps, alarms, drills,
saws, defibrillator charge, ambience) as a float buffer. Drop a real `.wav` into
`Resources/TraumaSurgeonAudio/<SoundId>.wav` and `AudioManager` will use it instead.

---

## Editing the content

All game content is plain JSON in `Assets/TraumaSurgeon/Resources/TraumaSurgeonData/`. After editing,
run **Trauma Surgeon → Validate Data Files** — it checks that every anatomy id, tool name, action
name and complication referenced by a procedure actually exists, and reports the exact offender.

Adding a new operation is one JSON object in `procedures.json`:

```jsonc
{
  "id": "my_case",
  "displayName": "My Operation",
  "bodyRegion": "Abdomen",              // Abdomen | Chest | Head | Limb
  "unlockLevel": 1,                     // CareerRank index, 0-6
  "requiredTools": ["Scalpel", "Hemostat", "NeedleHolder"],
  "possibleComplications": ["SuddenHemorrhage"],
  "patient": { "name": "...", "startHeartRate": 96, "...": "..." },
  "injuries": [
    { "partId": "spleen", "damageState": "Ruptured", "severity": 80,
      "bleedRate": 6, "requiresRemoval": true }
  ],
  "steps": [
    { "id": "skin", "title": "Open the skin", "action": "Incise",
      "targetPart": "skin_abdomen", "requiredTool": "Scalpel", "repetitions": 1 }
  ]
}
```

Valid `partId` values are the constants in `Scripts/Anatomy/AnatomyIds.cs`; valid `action` values are
`SurgicalActionType` and valid `requiredTool` values are `ToolType`, both in `Scripts/Core/GameEnums.cs`.

---

## Saving

Saves are JSON in `%USERPROFILE%\AppData\LocalLow\<Company>\Trauma Surgeon\TraumaSurgeon\`
(**Trauma Surgeon → Open Save Folder** jumps there):

- `career_0.json`, `career_1.json`, `career_2.json` — three career slots
- `settings.json` — controls, audio, graphics
- `*.json.bak` — rolling backup written before each save

Writes go to a temp file first and only then replace the real one, and a corrupt or truncated file
falls back to its `.bak` automatically. Career progress, unlocked procedures and tools, purchased
upgrades, staff, best scores per procedure, completed training modules and challenges, case history
and incident reports are all persisted.

---

## Content note

Anatomy and blood are rendered in a clinical style — flat clinical colours, small marker spheres for
bleeding points, no wound detail, no gore effects. The intent is a medical simulation, not horror.

---

## License

Original code and content. Uses only Unity's built-in modules and uGUI; no third-party or paid assets.
