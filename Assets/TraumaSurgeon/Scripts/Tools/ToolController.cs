using System.Collections.Generic;
using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using UnityEngine;

namespace TraumaSurgeon.Tools
{
    /// <summary>
    /// The instrument tray. Owns which tools are available for the current case, instantiates them
    /// lazily and handles equip / return / hotkey selection. Nine slots per page with paging for
    /// cases that need more instruments than that.
    /// </summary>
    public class ToolController : MonoBehaviour
    {
        public const int SlotsPerPage = 9;

        [Tooltip("Transform the equipped instrument is parented to (usually a child of the camera).")]
        public Transform HandAnchor;

        private readonly List<ToolType> _available = new List<ToolType>();
        private readonly Dictionary<ToolType, SurgicalToolBase> _instances =
            new Dictionary<ToolType, SurgicalToolBase>();

        public SurgicalToolBase Equipped { get; private set; }
        public ToolType EquippedType => Equipped != null ? Equipped.Type : ToolType.None;
        public int Page { get; private set; }

        public IReadOnlyList<ToolType> Available => _available;

        public int PageCount => Mathf.Max(1, Mathf.CeilToInt((float)_available.Count / SlotsPerPage));

        /// <summary>Replaces the tray contents. Hands are always present in slot 1.</summary>
        public void SetAvailableTools(IEnumerable<ToolType> tools)
        {
            _available.Clear();
            _available.Add(ToolType.Hands);

            if (tools != null)
            {
                foreach (ToolType type in tools)
                {
                    if (type != ToolType.None && type != ToolType.Hands && !_available.Contains(type))
                    {
                        _available.Add(type);
                    }
                }
            }

            Page = 0;
            EquipSlot(1);
        }

        /// <summary>Tools shown on the current page, in slot order.</summary>
        public List<ToolType> CurrentPageTools()
        {
            var list = new List<ToolType>();
            int start = Page * SlotsPerPage;
            for (int i = start; i < Mathf.Min(start + SlotsPerPage, _available.Count); i++)
            {
                list.Add(_available[i]);
            }

            return list;
        }

        public void NextPage()
        {
            if (PageCount <= 1)
            {
                return;
            }

            Page = (Page + 1) % PageCount;
            GameEvents.RaiseNotification($"Instrument tray page {Page + 1}/{PageCount}", NotificationType.Info);
            GameEvents.RaiseObjectivesUpdated();
        }

        /// <summary>Equips the tool in the given 1-based slot on the current page.</summary>
        public bool EquipSlot(int slot)
        {
            int index = Page * SlotsPerPage + (slot - 1);
            if (index < 0 || index >= _available.Count)
            {
                return false;
            }

            return Equip(_available[index]);
        }

        public bool Equip(ToolType type)
        {
            if (type == ToolType.None)
            {
                return false;
            }

            if (!_available.Contains(type))
            {
                GameEvents.RaiseNotification($"{type} is not on the tray for this case.", NotificationType.Warning);
                return false;
            }

            if (Equipped != null && Equipped.Type == type)
            {
                return true;
            }

            if (Equipped != null)
            {
                Equipped.OnUnequip();
            }

            Equipped = GetOrCreate(type);
            Equipped.OnEquip();
            GameEvents.RaiseToolEquipped(type);
            return true;
        }

        /// <summary>Returns the current instrument to the scrub nurse (Q).</summary>
        public void ReturnTool()
        {
            if (Equipped == null || Equipped.Type == ToolType.Hands)
            {
                return;
            }

            ToolType previous = Equipped.Type;
            Equipped.OnUnequip();
            Equipped = null;
            GameEvents.RaiseToolReturned(previous);

            if (AudioManager.Exists)
            {
                AudioManager.Instance.Play(SoundId.ToolDrop, 0.6f);
            }

            Equip(ToolType.Hands);
        }

        private SurgicalToolBase GetOrCreate(ToolType type)
        {
            if (_instances.TryGetValue(type, out SurgicalToolBase existing) && existing != null)
            {
                return existing;
            }

            Transform parent = HandAnchor != null ? HandAnchor : transform;
            SurgicalToolBase tool = ToolFactory.Create(type, parent);
            tool.transform.localPosition = Vector3.zero;
            tool.transform.localRotation = Quaternion.identity;
            tool.gameObject.SetActive(false);
            _instances[type] = tool;
            return tool;
        }

        /// <summary>Slot number (1-based, current page) for a tool, or 0 when not visible.</summary>
        public int SlotOf(ToolType type)
        {
            int index = _available.IndexOf(type);
            if (index < 0)
            {
                return 0;
            }

            int pageStart = Page * SlotsPerPage;
            if (index < pageStart || index >= pageStart + SlotsPerPage)
            {
                return 0;
            }

            return index - pageStart + 1;
        }

        public bool Has(ToolType type) => _available.Contains(type);

        /// <summary>Display name from the JSON tool database, falling back to the enum name.</summary>
        public static string NameOf(ToolType type)
        {
            ToolData data = DataLibrary.GetTool(type);
            return data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : type.ToString();
        }
    }
}
