using TraumaSurgeon.Audio;
using TraumaSurgeon.Core;
using TraumaSurgeon.InputSystem;
using UnityEngine;

namespace TraumaSurgeon.Player
{
    /// <summary>Anything the player can press E on.</summary>
    public interface IInteractable
    {
        /// <summary>Verb shown on the interaction prompt, e.g. "Review chart".</summary>
        string InteractionPrompt { get; }

        /// <summary>False hides the prompt (station not ready yet).</summary>
        bool CanInteract { get; }

        void Interact(GameObject interactor);
    }

    /// <summary>
    /// Simple interactable that raises a callback. Used for scrub sinks, imaging displays,
    /// instrument tables and the anaesthesia machine.
    /// </summary>
    public class InteractableStation : MonoBehaviour, IInteractable
    {
        public string prompt = "Use";
        public bool enabledForInteraction = true;

        /// <summary>Assigned by whatever builds the station.</summary>
        public System.Action<GameObject> OnInteract;

        public string InteractionPrompt => prompt;
        public bool CanInteract => enabledForInteraction;

        public void Interact(GameObject interactor)
        {
            OnInteract?.Invoke(interactor);
        }
    }

    /// <summary>
    /// Raycasts for interactables in front of the camera and handles the E key.
    /// The current prompt is published so the HUD can render it.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Camera PlayerCamera;
        public float range = 3.2f;

        [Tooltip("Radius of the forgiving sweep used when the precise ray misses.")]
        public float assistRadius = 0.45f;

        /// <summary>Prompt for whatever is currently targeted, or empty.</summary>
        public string CurrentPrompt { get; private set; } = string.Empty;

        private IInteractable _current;

        private void Update()
        {
            _current = null;
            CurrentPrompt = string.Empty;

            if (PlayerCamera == null)
            {
                return;
            }

            _current = FindInteractable();
            if (_current != null)
            {
                CurrentPrompt = _current.InteractionPrompt;
            }

            if (_current != null && InputManager.Exists && InputManager.Instance.Pressed(GameAction.Interact))
            {
                if (AudioManager.Exists)
                {
                    AudioManager.Instance.Play(SoundId.UiClick, 0.5f);
                }

                _current.Interact(gameObject);
            }
        }

        /// <summary>
        /// Precise ray first, then a wider sphere sweep. Stations are wall panels and sinks rather
        /// than small props, so demanding pixel-accurate aim only makes them feel broken.
        /// Interaction volumes are triggers, hence QueryTriggerInteraction.Collide.
        /// </summary>
        private IInteractable FindInteractable()
        {
            Vector3 origin = PlayerCamera.transform.position;
            Vector3 direction = PlayerCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0,
                    QueryTriggerInteraction.Collide))
            {
                IInteractable direct = Resolve(hit.collider);
                if (direct != null)
                {
                    return direct;
                }
            }

            RaycastHit[] sweep = Physics.SphereCastAll(origin, assistRadius, direction, range, ~0,
                QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;

            foreach (RaycastHit candidate in sweep)
            {
                IInteractable interactable = Resolve(candidate.collider);
                if (interactable == null)
                {
                    continue;
                }

                // SphereCastAll reports distance 0 for overlaps, so measure from the collider.
                float distance = Vector3.Distance(origin, candidate.collider.bounds.center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = interactable;
                }
            }

            return best;
        }

        private static IInteractable Resolve(Collider collider)
        {
            if (collider == null || collider.GetComponentInParent<FirstPersonController>() != null)
            {
                return null;
            }

            var interactable = collider.GetComponentInParent<IInteractable>();
            return interactable != null && interactable.CanInteract ? interactable : null;
        }
    }
}
