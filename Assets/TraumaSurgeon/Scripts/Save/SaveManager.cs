using System;
using System.IO;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Save
{
    /// <summary>
    /// JSON persistence with defensive error handling and one rolling backup per file.
    /// Files live in <c>Application.persistentDataPath/TraumaSurgeon/</c>.
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        public const int SlotCount = 3;
        private const string FolderName = "TraumaSurgeon";
        private const string SettingsFile = "settings.json";

        public SaveData Current { get; private set; }
        public int CurrentSlot { get; private set; } = -1;

        public static string RootFolder
        {
            get
            {
                string path = Path.Combine(Application.persistentDataPath, FolderName);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        private static string SlotPath(int slot) => Path.Combine(RootFolder, $"career_{slot}.json");

        private static string BackupPath(string path) => path + ".bak";

        // ---- Generic IO -------------------------------------------------------

        /// <summary>Writes JSON, rotating the previous file into a .bak first.</summary>
        public static bool WriteJson<T>(string path, T value)
        {
            try
            {
                string json = JsonUtility.ToJson(value, true);
                if (string.IsNullOrEmpty(json) || json == "{}")
                {
                    Debug.LogWarning($"[SaveManager] Refusing to write empty payload to {path}");
                    return false;
                }

                if (File.Exists(path))
                {
                    File.Copy(path, BackupPath(path), true);
                }

                string temp = path + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Write failed for {path}: {e.Message}");
                return false;
            }
        }

        /// <summary>Reads JSON, silently falling back to the .bak copy when the primary is corrupt.</summary>
        public static T ReadJson<T>(string path) where T : class
        {
            T result = TryRead<T>(path);
            if (result != null)
            {
                return result;
            }

            string backup = BackupPath(path);
            if (File.Exists(backup))
            {
                Debug.LogWarning($"[SaveManager] Restoring from backup: {backup}");
                result = TryRead<T>(backup);
                if (result != null)
                {
                    try
                    {
                        File.Copy(backup, path, true);
                    }
                    catch (Exception)
                    {
                        // Non fatal - we still have the data in memory.
                    }
                }
            }

            return result;
        }

        private static T TryRead<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Read failed for {path}: {e.Message}");
                return null;
            }
        }

        // ---- Career slots -----------------------------------------------------

        public SaveData LoadSlot(int slot)
        {
            SaveData data = ReadJson<SaveData>(SlotPath(slot));
            if (data == null)
            {
                return null;
            }

            if (data.career == null)
            {
                data.career = new CareerData();
            }

            Current = data;
            CurrentSlot = slot;
            return data;
        }

        public SaveData CreateNewCareer(int slot, string surgeonName)
        {
            var data = new SaveData
            {
                slotName = string.IsNullOrEmpty(surgeonName) ? "New Career" : surgeonName,
                lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            data.career.surgeonName = string.IsNullOrEmpty(surgeonName) ? "Dr. A. Vance" : surgeonName;
            Current = data;
            CurrentSlot = slot;
            return data;
        }

        /// <summary>Ensures a career exists in memory (used by Play Now / Training).</summary>
        public SaveData EnsureCareer()
        {
            if (Current == null)
            {
                Current = new SaveData();
            }

            return Current;
        }

        /// <summary>Points the in-memory career at a slot so the next save writes there.</summary>
        public void BindToSlot(int slot)
        {
            EnsureCareer();
            CurrentSlot = Mathf.Clamp(slot, 0, SlotCount - 1);
        }

        public bool SaveCurrent()
        {
            if (Current == null || CurrentSlot < 0)
            {
                return false;
            }

            Current.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            bool ok = WriteJson(SlotPath(CurrentSlot), Current);
            if (ok)
            {
                GameEvents.RaiseNotification("Progress saved.", NotificationType.Success);
            }

            return ok;
        }

        public bool SlotExists(int slot) => File.Exists(SlotPath(slot));

        public SaveData PeekSlot(int slot) => ReadJson<SaveData>(SlotPath(slot));

        public void DeleteSlot(int slot)
        {
            try
            {
                string path = SlotPath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (File.Exists(BackupPath(path)))
                {
                    File.Delete(BackupPath(path));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Delete failed: {e.Message}");
            }

            if (CurrentSlot == slot)
            {
                Current = null;
                CurrentSlot = -1;
            }
        }

        // ---- Settings ---------------------------------------------------------

        public static SettingsData LoadSettings()
        {
            SettingsData data = ReadJson<SettingsData>(Path.Combine(RootFolder, SettingsFile));
            return data ?? new SettingsData();
        }

        public static void SaveSettings(SettingsData data)
        {
            WriteJson(Path.Combine(RootFolder, SettingsFile), data);
        }
    }
}
