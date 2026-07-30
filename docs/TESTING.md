# Testing guide

How to verify each part of the game by hand. Every phase below is playable on its own.

Before anything else: open `Assets/TraumaSurgeon/Scenes/Bootstrap.unity`, press **Play**, and check
the console is clean. Then run **Trauma Surgeon → Validate Data Files** — it should report
8 procedures, 21 tools, 15 complications, 10 training stations, 6 challenges and no errors.

---

## Phase 1 — core, input, controller, operating room, vitals

**Files:** `Core/*`, `Input/*`, `Player/*`, `Environment/OperatingRoomBuilder`, `Patient/*`,
`Anatomy/*`, `Visuals/*`, `Tools/*`, `Audio/*`.

1. Play → main menu appears with the career summary panel.
2. **Play Now → Acute Appendicitis → Student → Scrub in and begin.**
3. You spawn beside the table facing the patient. Check:
   - `W A S D` moves, mouse looks, movement is slow and heavy.
   - The room has a table, two overhead lights, an anaesthesia machine, monitors, instrument
     tables, blood bags, a medication cart, a scrub sink and five staff figures.
   - Top-right monitor shows HR / BP / SpO2 / RR / temp, updating continuously.
   - Bottom-centre tool bar shows the instrument tray; `1`–`9` switch tools and the equipped tool
     is visible in the lower right of the view.
   - Holding `Shift` visibly steadies the instrument and drains the stamina bar.
   - `Q` returns the tool and re-equips your hands.
4. Open **Settings** from the pause menu (`Escape`): change mouse sensitivity, rebind a key, switch
   the graphics preset. Changes apply immediately and survive a restart.

**Expect:** vitals drift realistically; HR climbs if you leave the patient bleeding.

## Phase 2 — incision, bleeding, suction, clamping, cautery, suturing

**Files:** `Surgery/CuttingSystem`, `Surgery/HemostasisSystems`, `Surgery/SuturingSystem`,
`Anatomy/BleedingPoint`, `Anatomy/IncisionGuide`, tool implementations.

Easiest route: **Training** mode, which drills each system in isolation.

| Station | What to check |
| --- | --- |
| Incision Control | The dotted guide line is visible; holding LMB on it opens the layer. Cutting off the line damages the layer beneath and the accuracy score drops. |
| Bleeding Control | Red pulsing markers are bleeders. Clamping one turns it grey and stops the loss. Blood volume on the monitor stops falling. |
| Cautery Practice | Cautery seals small bleeders but refuses major vessels ("Too big to cauterise"). Holding it in one place for several seconds risks a surgical fire. |
| Suturing | Each click places a visible stitch; stitches placed on top of each other are rejected with "Stitches are bunching up". Four stitches close the wound. |

Suction: equip the suction and hold LMB in an open abdomen — pooled blood falls (visible in the chart
under *Theatre → Pooled blood in field*) and the field-clarity component of precision improves.

## Phase 3 — the appendectomy end to end

1. **Play Now → Acute Appendicitis → Resident.**
2. Follow the objective list in order:
   `Tab` (chart) → `E` at the imaging display → `E` at the scrub sink → `E` at the prep zone →
   scalpel through skin, fat, muscle → forceps RMB on the appendix → hemostat LMB on the appendix →
   scalpel RMB to divide → forceps LMB to remove → suction → needle holder ×4 to close.
3. Each completed objective ticks green with a chime.
4. On the final stitch the case ends and the post-operation report appears with a rank.

**Things worth deliberately getting wrong:**
- Divide the appendix *without* clamping first → a bleeder opens and you have to control it.
- Cut the liver with the scalpel → "wrong instrument" penalty, tissue damage, a new bleeder.
- Staple deep tissue → misuse penalty.
- Pack a sponge and close without retrieving it → retained-item penalty and an automatic F.

## Phase 4 — staff, complications, scoring, reports

1. Start any case and press `C` for the command wheel (or `F1`–`F12`).
2. Verify each order does something observable:
   - *Increase/lighten anaesthesia* moves the anaesthetic depth bar.
   - *Give blood* raises blood volume and decrements the unit count in the wheel centre.
   - *Start compressions* only works during an actual arrest, and the assistant visibly compresses.
   - *Suction* has the assistant clear pooled blood continuously.
   - *Count sponges* reports correct/incorrect and flags a retained sponge.
   - *Call another surgeon* completes the current step and consumes an assistance use.
3. Complications: play **Ruptured Spleen** on Attending and leave the bleeding uncontrolled for
   ~60 s. Expect, in order: haemorrhage warning → pressure collapse → cardiac arrest. Each is
   announced in the feed and by the team.
4. Resolve an arrest: `Hands` + LMB at ~110/min, `F3` for blood, syringe (RMB to pick epinephrine,
   wheel to set 1 mg, LMB to give), then defibrillator RMB to charge and LMB to shock. The rhythm
   converts and the complication clears.
5. The post-op report should show a scored breakdown, raw metrics and the incident timeline.

## Phase 5 — remaining operations, career, training, challenges, saves

1. **Career Mode** → pick a slot → hospital hub. Check the schedule, accept a case, complete it, and
   confirm money, XP, reputation and the case history all update.
2. Buy an upgrade, train a staff member, read an incident report.
3. Quit to the menu and re-enter career mode — everything persists. Delete
   `career_0.json` (leave `career_0.json.bak`) and reload: the backup is restored automatically.
4. Play each remaining operation once on Student to confirm every step is completable:
   collapsed lung, gunshot abdomen, ruptured spleen, femur fracture, CABG, brain hematoma,
   multi-system trauma.
5. **Challenges** → *The Golden Hour* (timer counts down in the objectives panel), *Blackout*
   (theatre lights are off), *Empty Blood Bank* (two units only).

---

## Known constraints of the placeholder build

- Geometry is Unity primitives; the anatomy is anatomically *arranged* but not anatomically modelled.
- Staff are primitive figures with a procedural idle/compression animation, not skeletal animation.
- Depth of field is approximated by easing the camera FOV, since no post-processing package is used.
- All audio is synthesised at boot; it is functional and distinct, not production sound design.

Each of these is isolated behind a factory (`PrimitiveFactory`, `ProceduralAudio`, `CameraFocus`) so
it can be replaced without touching gameplay code.
