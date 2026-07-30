using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using TraumaSurgeon.Scoring;
using TraumaSurgeon.Tools;
using UnityEngine;

namespace TraumaSurgeon.Surgery
{
    /// <summary>Runtime state of a single procedure step.</summary>
    public class ObjectiveState
    {
        public ProcedureStepData Data;
        public int Progress;
        public bool Complete;

        public float Normalised => Data.repetitions <= 0 ? 1f : Mathf.Clamp01((float)Progress / Data.repetitions);
    }

    /// <summary>
    /// Drives the ordered checklist for a procedure. Listens for surgical actions and advances
    /// the current step when the right verb is applied to the right structure with the right
    /// instrument. Optional steps ahead of the pointer can also be satisfied early.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        private readonly List<ObjectiveState> _steps = new List<ObjectiveState>();
        private ToolController _tools;
        private bool _running;

        public IReadOnlyList<ObjectiveState> Steps => _steps;
        public int CurrentIndex { get; private set; }
        public bool AllComplete { get; private set; }

        public ObjectiveState Current =>
            CurrentIndex >= 0 && CurrentIndex < _steps.Count ? _steps[CurrentIndex] : null;

        public int CompletedCount
        {
            get
            {
                int count = 0;
                foreach (ObjectiveState s in _steps)
                {
                    if (s.Complete)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Initialise(ProcedureData procedure, ToolController tools)
        {
            _tools = tools;
            _steps.Clear();
            CurrentIndex = 0;
            AllComplete = false;

            if (procedure != null && procedure.steps != null)
            {
                foreach (ProcedureStepData step in procedure.steps)
                {
                    _steps.Add(new ObjectiveState { Data = step });
                }
            }

            _running = true;
            GameEvents.RaiseStepChanged(CurrentIndex);
            GameEvents.RaiseObjectivesUpdated();
        }

        public void Stop()
        {
            _running = false;
        }

        private void OnEnable()
        {
            GameEvents.ActionPerformed += OnActionPerformed;
        }

        private void OnDisable()
        {
            GameEvents.ActionPerformed -= OnActionPerformed;
        }

        private void OnActionPerformed(SurgicalActionType action, AnatomyPart target, float quality)
        {
            if (!_running || _steps.Count == 0)
            {
                return;
            }

            // Try the current step first, then any optional step further down the list.
            if (TryAdvance(CurrentIndex, action, target, quality))
            {
                return;
            }

            for (int i = CurrentIndex + 1; i < _steps.Count; i++)
            {
                if (_steps[i].Data.optional && TryAdvance(i, action, target, quality))
                {
                    return;
                }
            }
        }

        private bool TryAdvance(int index, SurgicalActionType action, AnatomyPart target, float quality)
        {
            if (index < 0 || index >= _steps.Count)
            {
                return false;
            }

            ObjectiveState state = _steps[index];
            if (state.Complete || !Matches(state.Data, action, target))
            {
                return false;
            }

            state.Progress++;
            if (state.Progress >= Mathf.Max(1, state.Data.repetitions))
            {
                state.Complete = true;
                GameEvents.RaiseScoreEvent(ScoreEventType.StepCompleted, quality, state.Data.title);
                if (AudioManager.Exists)
                {
                    AudioManager.Instance.Play(SoundId.ObjectiveComplete, 0.5f);
                }

                GameEvents.RaiseNotification($"Objective complete: {state.Data.title}", NotificationType.Success);
                AdvancePointer();
            }

            GameEvents.RaiseObjectivesUpdated();
            return true;
        }

        private bool Matches(ProcedureStepData step, SurgicalActionType action, AnatomyPart target)
        {
            if (step.Action != action)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(step.targetPart))
            {
                if (target == null || target.partId != step.targetPart)
                {
                    return false;
                }
            }

            if (step.RequiredTool != ToolType.None && _tools != null &&
                _tools.EquippedType != step.RequiredTool)
            {
                // Right verb, wrong instrument - flag it but do not advance.
                GameEvents.RaiseScoreEvent(ScoreEventType.WrongTool, 1f,
                    $"{step.title} needs the {ToolController.NameOf(step.RequiredTool)}");
                return false;
            }

            return true;
        }

        private void AdvancePointer()
        {
            while (CurrentIndex < _steps.Count && _steps[CurrentIndex].Complete)
            {
                CurrentIndex++;
            }

            if (CurrentIndex >= _steps.Count)
            {
                // Optional steps left unfinished do not block completion.
                bool anyRequiredOutstanding = false;
                foreach (ObjectiveState s in _steps)
                {
                    if (!s.Complete && !s.Data.optional)
                    {
                        anyRequiredOutstanding = true;
                        break;
                    }
                }

                AllComplete = !anyRequiredOutstanding;
            }

            GameEvents.RaiseStepChanged(Mathf.Min(CurrentIndex, _steps.Count - 1));
        }

        /// <summary>Marks the current step complete - used by the "call for help" assist.</summary>
        public void ForceCompleteCurrent(string reason)
        {
            ObjectiveState state = Current;
            if (state == null || state.Complete)
            {
                return;
            }

            state.Progress = Mathf.Max(1, state.Data.repetitions);
            state.Complete = true;
            GameEvents.RaiseNotification($"{state.Data.title} completed with assistance ({reason}).",
                NotificationType.Info);
            AdvancePointer();
            GameEvents.RaiseObjectivesUpdated();
        }

        /// <summary>Hint text for the current step, respecting the difficulty's guidance setting.</summary>
        public string CurrentHint(bool showHints)
        {
            ObjectiveState state = Current;
            if (state == null)
            {
                return string.Empty;
            }

            if (!showHints)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(state.Data.hint) ? state.Data.description : state.Data.hint;
        }

        /// <summary>The anatomy id the current step points at (for highlighting).</summary>
        public string CurrentTargetPart()
        {
            ObjectiveState state = Current;
            return state != null ? state.Data.targetPart : string.Empty;
        }

        /// <summary>Instrument the current step expects, if any.</summary>
        public ToolType CurrentRequiredTool()
        {
            ObjectiveState state = Current;
            return state != null ? state.Data.RequiredTool : ToolType.None;
        }
    }
}
