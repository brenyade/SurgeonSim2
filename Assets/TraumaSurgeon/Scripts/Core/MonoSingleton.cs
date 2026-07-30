using UnityEngine;

namespace TraumaSurgeon.Core
{
    /// <summary>
    /// Lightweight singleton base for manager components. Managers are created once by
    /// <see cref="Bootstrap"/> and live on a DontDestroyOnLoad root object.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance => _instance;

        /// <summary>True when a live instance exists (safe to call during shutdown).</summary>
        public static bool Exists => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = (T)this;
            OnSingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>Called once, only for the surviving instance.</summary>
        protected virtual void OnSingletonAwake() { }
    }
}
