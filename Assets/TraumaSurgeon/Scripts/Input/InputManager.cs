using System;
using System.Collections.Generic;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.InputSystem
{
    /// <summary>
    /// Thin polling wrapper over Unity's classic Input class. Every gameplay script asks the
    /// InputManager instead of touching KeyCodes directly, which makes rebinding trivial and
    /// keeps the game engine-input agnostic.
    /// </summary>
    public class InputManager : MonoSingleton<InputManager>
    {
        private readonly Dictionary<GameAction, KeyCode> _bindings = new Dictionary<GameAction, KeyCode>();

        /// <summary>When true all gameplay input is ignored (menus are open).</summary>
        public bool GameplayInputBlocked { get; set; }

        /// <summary>Mouse look sensitivity, mirrored from settings.</summary>
        public float MouseSensitivity { get; set; } = 2.2f;

        public bool InvertY { get; set; }

        public static readonly Dictionary<GameAction, KeyCode> Defaults = new Dictionary<GameAction, KeyCode>
        {
            { GameAction.MoveForward, KeyCode.W },
            { GameAction.MoveBack, KeyCode.S },
            { GameAction.MoveLeft, KeyCode.A },
            { GameAction.MoveRight, KeyCode.D },
            { GameAction.UseTool, KeyCode.Mouse0 },
            { GameAction.AltUseTool, KeyCode.Mouse1 },
            { GameAction.Interact, KeyCode.E },
            { GameAction.DropTool, KeyCode.Q },
            { GameAction.SteadyHand, KeyCode.LeftShift },
            { GameAction.PatientChart, KeyCode.Tab },
            { GameAction.RequestAssistance, KeyCode.Space },
            { GameAction.CommandWheel, KeyCode.C },
            { GameAction.Pause, KeyCode.Escape },
            { GameAction.ToggleImaging, KeyCode.F },
            { GameAction.Tool1, KeyCode.Alpha1 },
            { GameAction.Tool2, KeyCode.Alpha2 },
            { GameAction.Tool3, KeyCode.Alpha3 },
            { GameAction.Tool4, KeyCode.Alpha4 },
            { GameAction.Tool5, KeyCode.Alpha5 },
            { GameAction.Tool6, KeyCode.Alpha6 },
            { GameAction.Tool7, KeyCode.Alpha7 },
            { GameAction.Tool8, KeyCode.Alpha8 },
            { GameAction.Tool9, KeyCode.Alpha9 },
            { GameAction.NextToolPage, KeyCode.Alpha0 },
            { GameAction.Sprint, KeyCode.LeftControl }
        };

        protected override void OnSingletonAwake()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            _bindings.Clear();
            foreach (KeyValuePair<GameAction, KeyCode> pair in Defaults)
            {
                _bindings[pair.Key] = pair.Value;
            }
        }

        /// <summary>Applies a persisted binding list (unknown entries are ignored).</summary>
        public void ApplyBindings(List<KeyBindingEntry> entries)
        {
            ResetToDefaults();
            if (entries == null)
            {
                return;
            }

            foreach (KeyBindingEntry entry in entries)
            {
                if (Enum.TryParse(entry.action, true, out GameAction action))
                {
                    _bindings[action] = (KeyCode)entry.keyCode;
                }
            }
        }

        public List<KeyBindingEntry> ExportBindings()
        {
            var list = new List<KeyBindingEntry>();
            foreach (KeyValuePair<GameAction, KeyCode> pair in _bindings)
            {
                list.Add(new KeyBindingEntry(pair.Key.ToString(), (int)pair.Value));
            }

            return list;
        }

        public KeyCode GetKey(GameAction action)
        {
            return _bindings.TryGetValue(action, out KeyCode key) ? key : KeyCode.None;
        }

        public void Rebind(GameAction action, KeyCode key)
        {
            _bindings[action] = key;
        }

        public string GetKeyLabel(GameAction action)
        {
            KeyCode key = GetKey(action);
            switch (key)
            {
                case KeyCode.Mouse0: return "LMB";
                case KeyCode.Mouse1: return "RMB";
                case KeyCode.Mouse2: return "MMB";
                case KeyCode.Alpha0: return "0";
                case KeyCode.Alpha1: return "1";
                case KeyCode.Alpha2: return "2";
                case KeyCode.Alpha3: return "3";
                case KeyCode.Alpha4: return "4";
                case KeyCode.Alpha5: return "5";
                case KeyCode.Alpha6: return "6";
                case KeyCode.Alpha7: return "7";
                case KeyCode.Alpha8: return "8";
                case KeyCode.Alpha9: return "9";
                case KeyCode.LeftShift: return "Shift";
                case KeyCode.LeftControl: return "Ctrl";
                case KeyCode.Escape: return "Esc";
                default: return key.ToString();
            }
        }

        // ---- Polling helpers --------------------------------------------------

        /// <summary>Held this frame. Respects the gameplay block except for Pause.</summary>
        public bool Held(GameAction action)
        {
            if (IsBlocked(action))
            {
                return false;
            }

            return UnityEngine.Input.GetKey(GetKey(action));
        }

        public bool Pressed(GameAction action)
        {
            if (IsBlocked(action))
            {
                return false;
            }

            return UnityEngine.Input.GetKeyDown(GetKey(action));
        }

        public bool Released(GameAction action)
        {
            if (IsBlocked(action))
            {
                return false;
            }

            return UnityEngine.Input.GetKeyUp(GetKey(action));
        }

        /// <summary>Ignores the gameplay block - used by menus for Escape/Tab style toggles.</summary>
        public bool PressedRaw(GameAction action)
        {
            return UnityEngine.Input.GetKeyDown(GetKey(action));
        }

        private bool IsBlocked(GameAction action)
        {
            if (!GameplayInputBlocked)
            {
                return false;
            }

            return action != GameAction.Pause;
        }

        public Vector2 MoveAxis
        {
            get
            {
                if (GameplayInputBlocked)
                {
                    return Vector2.zero;
                }

                float x = 0f;
                float y = 0f;
                if (UnityEngine.Input.GetKey(GetKey(GameAction.MoveForward))) y += 1f;
                if (UnityEngine.Input.GetKey(GetKey(GameAction.MoveBack))) y -= 1f;
                if (UnityEngine.Input.GetKey(GetKey(GameAction.MoveRight))) x += 1f;
                if (UnityEngine.Input.GetKey(GetKey(GameAction.MoveLeft))) x -= 1f;
                Vector2 v = new Vector2(x, y);
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public Vector2 LookDelta
        {
            get
            {
                if (GameplayInputBlocked)
                {
                    return Vector2.zero;
                }

                float mx = UnityEngine.Input.GetAxisRaw("Mouse X") * MouseSensitivity;
                float my = UnityEngine.Input.GetAxisRaw("Mouse Y") * MouseSensitivity;
                if (InvertY)
                {
                    my = -my;
                }

                return new Vector2(mx, my);
            }
        }

        public float ScrollDelta => GameplayInputBlocked ? 0f : UnityEngine.Input.GetAxisRaw("Mouse ScrollWheel");

        /// <summary>Returns 1..9 when a tool hotkey was pressed this frame, otherwise 0.</summary>
        public int PressedToolSlot()
        {
            for (int i = 0; i < 9; i++)
            {
                GameAction action = GameAction.Tool1 + i;
                if (Pressed(action))
                {
                    return i + 1;
                }
            }

            return 0;
        }
    }
}
