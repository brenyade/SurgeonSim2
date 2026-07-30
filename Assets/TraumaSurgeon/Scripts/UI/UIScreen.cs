using UnityEngine;
using UnityEngine.UI;

namespace TraumaSurgeon.UI
{
    /// <summary>
    /// Base class for every full screen or overlay panel. Subclasses implement <see cref="Build"/>
    /// once and <see cref="Refresh"/> whenever the screen is shown.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public RectTransform Root { get; private set; }

        /// <summary>Set true for overlays that keep the game visible behind them (HUD, wheel).</summary>
        protected virtual bool Transparent => false;

        /// <summary>Screens that should pause gameplay input while visible.</summary>
        public virtual bool BlocksGameplay => !Transparent;

        public bool IsVisible => Root != null && Root.gameObject.activeSelf;

        /// <summary>
        /// Full-screen background image, created on demand. Using this instead of adding a second
        /// Image keeps exactly one background graphic per screen.
        /// </summary>
        protected Image RootBackground
        {
            get
            {
                Image image = Root.GetComponent<Image>();
                if (image == null)
                {
                    image = Root.gameObject.AddComponent<Image>();
                }

                return image;
            }
        }

        /// <summary>Creates the screen under a canvas. Called once by the UIManager.</summary>
        public void Initialise(Transform canvas)
        {
            Root = UIFactory.CreateRect(GetType().Name, canvas);
            UIFactory.Stretch(Root);

            if (!Transparent)
            {
                Image bg = Root.gameObject.AddComponent<Image>();
                bg.color = UITheme.Background;
            }

            Build();
            Root.gameObject.SetActive(false);
        }

        /// <summary>Builds the static hierarchy.</summary>
        protected abstract void Build();

        /// <summary>Repopulates dynamic content. Called every time the screen is shown.</summary>
        public virtual void Refresh() { }

        public virtual void Show()
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(true);
                Root.SetAsLastSibling();
            }

            Refresh();
        }

        public virtual void Hide()
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(false);
            }
        }

        /// <summary>Destroys and rebuilds a dynamic container's children.</summary>
        protected static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
