using System;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Patient;
using TraumaSurgeon.Scoring;

namespace TraumaSurgeon.Core
{
    /// <summary>
    /// Global, strongly typed event bus. Keeps systems decoupled: gameplay systems raise,
    /// UI/audio/scoring listen. All events are cleared when a surgery ends via <see cref="ResetSessionListeners"/>
    /// is intentionally NOT provided - listeners unsubscribe in OnDisable instead.
    /// </summary>
    public static class GameEvents
    {
        // ---- Application flow -------------------------------------------------
        public static event Action<GamePhase> PhaseChanged;
        public static event Action<string, NotificationType> Notification;

        // ---- Patient ----------------------------------------------------------
        public static event Action<VitalSigns> VitalsUpdated;
        public static event Action PatientDied;
        public static event Action<CardiacRhythm> RhythmChanged;

        // ---- Surgery ----------------------------------------------------------
        public static event Action<SurgicalActionType, AnatomyPart, float> ActionPerformed;
        public static event Action<int> StepChanged;
        public static event Action ObjectivesUpdated;
        public static event Action<SurgeryReport> SurgeryCompleted;

        // ---- Tools ------------------------------------------------------------
        public static event Action<ToolType> ToolEquipped;
        public static event Action<ToolType> ToolReturned;

        // ---- Complications ----------------------------------------------------
        public static event Action<ComplicationType, string> ComplicationStarted;
        public static event Action<ComplicationType> ComplicationResolved;

        // ---- Staff ------------------------------------------------------------
        public static event Action<StaffRole, string> StaffSpeech;
        public static event Action<StaffCommand> CommandIssued;

        // ---- Scoring ----------------------------------------------------------
        public static event Action<ScoreEventType, float, string> ScoreEvent;

        public static void RaisePhaseChanged(GamePhase phase) => PhaseChanged?.Invoke(phase);

        public static void RaiseNotification(string message, NotificationType type = NotificationType.Info)
            => Notification?.Invoke(message, type);

        public static void RaiseVitalsUpdated(VitalSigns vitals) => VitalsUpdated?.Invoke(vitals);

        public static void RaisePatientDied() => PatientDied?.Invoke();

        public static void RaiseRhythmChanged(CardiacRhythm rhythm) => RhythmChanged?.Invoke(rhythm);

        public static void RaiseActionPerformed(SurgicalActionType action, AnatomyPart target, float quality)
            => ActionPerformed?.Invoke(action, target, quality);

        public static void RaiseStepChanged(int index) => StepChanged?.Invoke(index);

        public static void RaiseObjectivesUpdated() => ObjectivesUpdated?.Invoke();

        public static void RaiseSurgeryCompleted(SurgeryReport report) => SurgeryCompleted?.Invoke(report);

        public static void RaiseToolEquipped(ToolType tool) => ToolEquipped?.Invoke(tool);

        public static void RaiseToolReturned(ToolType tool) => ToolReturned?.Invoke(tool);

        public static void RaiseComplicationStarted(ComplicationType type, string message)
            => ComplicationStarted?.Invoke(type, message);

        public static void RaiseComplicationResolved(ComplicationType type) => ComplicationResolved?.Invoke(type);

        public static void RaiseStaffSpeech(StaffRole role, string line) => StaffSpeech?.Invoke(role, line);

        public static void RaiseCommandIssued(StaffCommand command) => CommandIssued?.Invoke(command);

        public static void RaiseScoreEvent(ScoreEventType type, float amount, string reason)
            => ScoreEvent?.Invoke(type, amount, reason);
    }
}
