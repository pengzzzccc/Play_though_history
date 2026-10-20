using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UnknownTechnology
{
    public class ArtefactInteraction : MonoBehaviour
    {
        [SerializeField] private UnityEvent onInteract = new UnityEvent();

        private readonly HashSet<Collider> playerColliders = new();

        private bool CanInteract =>
            playerColliders.Count > 0 &&
            Game.Phase == GamePhase.Exploring &&
            !CanvasQuizUI.IsQuizOpen;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonPlayer>() != null)
                playerColliders.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            playerColliders.Remove(other);
        }

        private void Update()
        {
            if (CanInteract && Keyboard.current != null &&
                Keyboard.current.tKey.wasPressedThisFrame)
            {
                onInteract.Invoke();
            }
        }

        private void OnGUI()
        {
            if (CanInteract)
            {
                GUI.Box(
                    new Rect((Screen.width - 320f) / 2f,
                        Screen.height - 90f, 320f, 40f),
                    "Press T to interact with Artefact");
            }
        }

        private void OnDisable()
        {
            playerColliders.Clear();
        }
    }
}