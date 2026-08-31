using UnknownTechnology.Core.Settings;
using UnityEngine;

namespace UnknownTechnology.Gameplay.Player
{
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform presentationRoot;
        [SerializeField] private float bobAmplitude = 0.015f;
        [SerializeField] private float bobFrequency = 7f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private float bobTime;
        private int speedParameter;
        private int toolParameter;

        private void Awake()
        {
            if (presentationRoot != null)
            {
                baseLocalPosition = presentationRoot.localPosition;
                baseLocalRotation = presentationRoot.localRotation;
            }

            speedParameter = Animator.StringToHash("MoveSpeed");
            toolParameter = Animator.StringToHash("Tool");
        }

        public void Tick(float normalizedSpeed, bool toolHeld, GameSettingsSnapshot settings, float deltaTime)
        {
            if (animator != null)
            {
                animator.SetFloat(speedParameter, normalizedSpeed);
                animator.SetBool(toolParameter, toolHeld);
            }

            if (presentationRoot == null)
            {
                return;
            }

            var targetRotation = baseLocalRotation * Quaternion.Euler(toolHeld ? -12f : 0f, 0f, 0f);
            presentationRoot.localRotation = Quaternion.Slerp(presentationRoot.localRotation, targetRotation, deltaTime * 14f);

            if (settings.ReducedMotion || normalizedSpeed <= 0.01f)
            {
                bobTime = 0f;
                presentationRoot.localPosition = Vector3.Lerp(presentationRoot.localPosition, baseLocalPosition, deltaTime * 12f);
                return;
            }

            bobTime += deltaTime * bobFrequency;
            var offset = new Vector3(Mathf.Cos(bobTime * 0.5f), Mathf.Sin(bobTime), 0f) * (bobAmplitude * normalizedSpeed);
            presentationRoot.localPosition = baseLocalPosition + offset;
        }

#if UNITY_EDITOR
        public void Configure(Animator configuredAnimator, Transform configuredPresentationRoot)
        {
            animator = configuredAnimator;
            presentationRoot = configuredPresentationRoot;
        }
#endif
    }
}
