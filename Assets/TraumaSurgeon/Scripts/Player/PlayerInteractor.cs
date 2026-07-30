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
        public float range = 2.6f;

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

            Ray ray = new Ray(PlayerCamera.transform.position, PlayerCamera.transform.forward);
            // Interaction volumes are triggers, so they must be included here.
            if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide) &&
                hit.collider.GetComponentInParent<FirstPersonController>() == null)
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    _current = interactable;
                    CurrentPrompt = interactable.InteractionPrompt;
                }
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
    }
}
