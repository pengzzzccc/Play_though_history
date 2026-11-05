using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownTechnology
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class NpcQuizTrigger : MonoBehaviour
    {
        [SerializeField] private CanvasQuizUI quizUI;

        private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

        private bool CanInteract => quizUI != null && quizUI.CanInteractWithJimmy && playerColliders.Count > 0 &&
            Game.Phase == GamePhase.Exploring && !CanvasQuizUI.IsQuizOpen;

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
                quizUI.InteractWithJimmy();
        }

        private void OnGUI()
        {
            if (CanInteract)
                GUI.Box(new Rect((Screen.width - 280f) / 2f, Screen.height - 90f, 280f, 40f),
                    quizUI.HasSeenIntroduction ? "Press T to start the quiz" : "Press T to talk to Jimmy");
        }

        private void OnDisable()
        {
            playerColliders.Clear();
        }
    }
}
