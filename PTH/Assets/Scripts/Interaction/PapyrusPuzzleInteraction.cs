using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownTechnology
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class PapyrusPuzzleInteraction : MonoBehaviour
    {
        [SerializeField] private PapyrusWallPuzzle puzzle;

        private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

        private bool CanInteract => puzzle != null && puzzle.CanBegin && playerColliders.Count > 0;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

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
            if (CanInteract && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                puzzle.BeginMinigame();
        }

        private void OnGUI()
        {
            if (CanInteract)
                GUI.Box(new Rect((Screen.width - 320f) / 2f, Screen.height - 90f, 320f, 40f),
                    "Press T to fix the papyrus");
        }

        private void OnDisable()
        {
            playerColliders.Clear();
        }
    }
}
