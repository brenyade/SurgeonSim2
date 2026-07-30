using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Surgery;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// Defibrillator paddles. Secondary charges, primary shocks.
    /// Misuse: shocking a non-shockable rhythm, or shocking without charging.
    /// </summary>
    public class Defibrillator : SurgicalToolBase
    {
        public float EnergyJoules = 200f;

        private bool _charged;
        private float _chargeTimer;

        protected override void Update()
        {
            base.Update();

            if (_chargeTimer > 0f)
            {
                _chargeTimer -= Time.deltaTime;
                if (_chargeTimer <= 0f)
                {
                    _charged = true;
                    GameEvents.RaiseNotification($"Defibrillator charged - {EnergyJoules:0} joules. Clear!",
                        NotificationType.Warning);
                }
            }
        }

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (ctx.Patient == null)
            {
                return;
            }

            if (!_charged)
            {
                GameEvents.RaiseNotification("Paddles are not charged - right click to charge.",
                    NotificationType.Warning);
                ReportUnnecessary("shock attempted without charging");
                return;
            }

            if (!DefibrillationSystem.IsFieldSafe(ctx.Patient))
            {
                GameEvents.RaiseNotification("Field is soaked - suction before you shock.",
                    NotificationType.Warning);
            }

            _charged = false;
            PlayUseAnimation();
            PlaySound(SoundId.DefibShock, transform.position, 1f);

            bool converted = DefibrillationSystem.Shock(ctx.Patient, EnergyJoules);
            ReportAction(SurgicalActionType.Defibrillate, ctx.Target, converted ? 1f : 0.5f);
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (_charged || _chargeTimer > 0f)
            {
                // Cycle energy instead of double charging.
                EnergyJoules = EnergyJoules >= 360f ? 120f : EnergyJoules + 80f;
                GameEvents.RaiseNotification($"Energy set to {EnergyJoules:0} joules.", NotificationType.Info);
                return;
            }

            _chargeTimer = DefibrillationSystem.ChargeSeconds;
            PlaySound(SoundId.DefibCharge, transform.position, 0.8f);
            GameEvents.RaiseStaffSpeech(StaffRole.CirculatingNurse, "Charging!");
        }

        public override void OnUnequip()
        {
            _charged = false;
            _chargeTimer = 0f;
            base.OnUnequip();
        }
    }

    /// <summary>
    /// Syringe - draws up and administers the currently selected drug.
    /// Secondary cycles the selected drug. Misuse: wrong drug or wrong dose.
    /// </summary>
    public class Syringe : SurgicalToolBase
    {
        private static readonly string[] Rotation =
        {
            "epinephrine", "atropine", "amiodarone", "norepinephrine",
            "fentanyl", "propofol", "tranexamic", "cefazolin", "heparin"
        };

        private int _index;

        /// <summary>Dose the player has dialled in. Auto-set on easier difficulties.</summary>
        public float Dose { get; private set; }

        public string SelectedDrug => Rotation[_index];

        protected override void Awake()
        {
            base.Awake();
            SyncDoseToDefault();
        }

        public override void OnEquip()
        {
            base.OnEquip();
            SyncDoseToDefault();
            GameEvents.RaiseNotification(
                $"Syringe: {MedicationSystem.Get(SelectedDrug).DisplayName} at {Dose:0.##}", NotificationType.Info);
        }

        private void SyncDoseToDefault()
        {
            MedicationDefinition def = MedicationSystem.Get(SelectedDrug);
            if (def == null)
            {
                return;
            }

            bool manual = GameManager.Exists && GameManager.Instance.Difficulty != null &&
                          GameManager.Instance.Difficulty.ManualMedicationDoses;

            // On manual difficulties the dial starts off-target so the player must set it.
            Dose = manual ? def.DefaultDose * 0.5f : def.DefaultDose;
        }

        /// <summary>Called by the HUD dose dial (mouse wheel while the syringe is equipped).</summary>
        public void AdjustDose(float delta)
        {
            MedicationDefinition def = MedicationSystem.Get(SelectedDrug);
            if (def == null)
            {
                return;
            }

            float step = Mathf.Max(0.05f, def.DefaultDose * 0.1f);
            Dose = Mathf.Max(0f, Dose + delta * step);
        }

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            if (ctx.Patient == null || ctx.Patient.Medications == null)
            {
                return;
            }

            PlayUseAnimation();
            MedicationDefinition def = MedicationSystem.Get(SelectedDrug);
            bool correct = ctx.Patient.Medications.Administer(SelectedDrug, Dose);

            GameEvents.RaiseNotification(
                $"Administered {def.DisplayName} {Dose:0.##} {def.Unit}",
                correct ? NotificationType.Success : NotificationType.Warning);

            ReportAction(SurgicalActionType.Medicate, ctx.Target, correct ? 1f : 0.3f);
            StartCooldown(0.4f);
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            _index = (_index + 1) % Rotation.Length;
            SyncDoseToDefault();
            MedicationDefinition def = MedicationSystem.Get(SelectedDrug);
            GameEvents.RaiseNotification($"Drawn up: {def.DisplayName} ({def.Indication})", NotificationType.Info);
        }
    }

    /// <summary>
    /// Chest tube. Primary inserts into the pleural space, secondary confirms placement and
    /// starts drainage. Misuse: driving it through lung parenchyma.
    /// </summary>
    public class ChestTube : SurgicalToolBase
    {
        private bool _placed;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            PlayUseAnimation();

            if (ChestTubeSystem.Insert(ref ctx, out string message))
            {
                _placed = true;
                PlaySound(SoundId.Suction, ctx.Point, 0.7f);
                GameEvents.RaiseNotification(message, NotificationType.Success);
                ReportAction(SurgicalActionType.InsertChestTube, ctx.Target, ctx.Quality);
            }
            else if (!string.IsNullOrEmpty(message))
            {
                GameEvents.RaiseNotification(message, NotificationType.Warning);
            }
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (ctx.Patient == null)
            {
                return;
            }

            AnatomyPart pleura = ctx.Patient.Body.Get(AnatomyIds.PleuralSpace);
            if (pleura == null || !pleura.isOpen)
            {
                GameEvents.RaiseNotification("No tube in place to confirm.", NotificationType.Warning);
                return;
            }

            float drained = ChestTubeSystem.Drain(ctx.Patient, 300f, 1f);
            GameEvents.RaiseNotification(
                $"Placement confirmed. {Mathf.RoundToInt(drained)} ml drained, lung re-expanding.",
                NotificationType.Success);
            ReportAction(SurgicalActionType.Confirm, pleura, 1f);
        }

        public bool IsPlaced => _placed;
    }

    /// <summary>
    /// Laparoscopic camera. Primary inserts/removes the scope, secondary sweeps the field to
    /// inspect structures. Misuse: pushing the scope into an organ.
    /// </summary>
    public class LaparoscopicCamera : SurgicalToolBase
    {
        public override float Reach => 1.9f;

        public override void UsePrimary(ref ToolUseContext ctx)
        {
            LaparoscopySystem scope = SurgeryServices.Laparoscopy;
            if (scope == null)
            {
                ReportUnnecessary("no laparoscopy stack available");
                return;
            }

            PlayUseAnimation();

            if (scope.IsActive)
            {
                scope.Deactivate();
                GameEvents.RaiseNotification("Scope withdrawn.", NotificationType.Info);
                return;
            }

            if (ctx.HasTarget && ctx.Target.layer == AnatomyLayer.Organ && ctx.Quality < 0.4f)
            {
                ReportMisuse(ref ctx, "scope pushed into an organ");
                return;
            }

            scope.Activate(Tip != null ? Tip : transform);
            ReportAction(SurgicalActionType.Laparoscope, ctx.Target, ctx.Quality);
        }

        public override void UseSecondary(ref ToolUseContext ctx)
        {
            if (!ctx.HasTarget)
            {
                ReportUnnecessary("nothing in view");
                return;
            }

            GameEvents.RaiseNotification($"Scope view: {ctx.Target.StatusLine()}", NotificationType.Info);
            ReportAction(SurgicalActionType.Inspect, ctx.Target, 1f);
        }

        public override void OnUnequip()
        {
            if (SurgeryServices.Laparoscopy != null && SurgeryServices.Laparoscopy.IsActive)
            {
                SurgeryServices.Laparoscopy.Deactivate();
            }

            base.OnUnequip();
        }
    }
}
