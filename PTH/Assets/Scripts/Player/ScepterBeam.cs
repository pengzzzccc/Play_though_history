using UnityEngine;
using UnityEngine.Rendering;

namespace UnknownTechnology
{
    /// <summary>
    /// Draws a wavy energy beam from the sceptre tip to the centre of the item
    /// the sceptre is carrying. The beam fades in on grab and fades out on
    /// release (still following the item while it flies into a slot). The line
    /// is displaced by a travelling sine wave whose amplitude is pinned to zero
    /// at both ends; the ScepterBeam shader adds pulses running along it.
    /// Reduced Motion flattens the wave and stops the pulses.
    /// </summary>
    [RequireComponent(typeof(Scepter))]
    public class ScepterBeam : MonoBehaviour
    {
        private static readonly int MotionScaleId = Shader.PropertyToID("_MotionScale");

        [SerializeField] private Transform sceptre;
        [SerializeField] private Vector3 tipLocalOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private Material beamMaterial;
        [SerializeField] private int segments = 40;
        [SerializeField] private float width = 0.06f;
        [SerializeField] private float waveAmplitude = 0.05f;
        [SerializeField] private float waveLength = 0.7f;
        [SerializeField] private float waveSpeed = 2.5f;
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private Scepter scepter;
        private LineRenderer line;
        private Vector3[] points;
        private MaterialPropertyBlock block;
        private CarryableItem target;
        private float visibility;
        private float wavePhase;

        private void Awake()
        {
            scepter = GetComponent<Scepter>();
            if (sceptre == null)
            {
                sceptre = FindChildRecursive(transform, "Sceptre");
            }

            var beamObject = new GameObject("Sceptre Beam");
            beamObject.transform.SetParent(transform, false);
            line = beamObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthMultiplier = width;
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.8f));
            line.sharedMaterial = beamMaterial != null ? beamMaterial : new Material(Shader.Find("Custom/ScepterBeam"));
            segments = Mathf.Max(2, segments);
            points = new Vector3[segments + 1];
            line.positionCount = points.Length;
            line.enabled = false;
        }

        private void LateUpdate()
        {
            var carried = scepter.Carried;
            if (carried != null)
            {
                target = carried;
            }

            var wanted = carried != null ? 1f : 0f;
            var duration = wanted > visibility ? fadeInDuration : fadeOutDuration;
            visibility = duration > 0f
                ? Mathf.MoveTowards(visibility, wanted, Time.deltaTime / duration)
                : wanted;

            if (visibility <= 0f || target == null || sceptre == null)
            {
                visibility = 0f;
                target = null;
                line.enabled = false;
                return;
            }

            var reducedMotion = Game.Settings.reducedMotion;
            if (!reducedMotion)
            {
                wavePhase += Time.deltaTime * waveSpeed * Mathf.PI * 2f;
            }

            UpdatePoints(
                sceptre.TransformPoint(tipLocalOffset),
                target.ItemRenderer != null ? target.ItemRenderer.bounds.center : target.transform.position,
                reducedMotion ? 0f : waveAmplitude);

            var alpha = Mathf.SmoothStep(0f, 1f, visibility);
            var color = new Color(1f, 1f, 1f, alpha);
            line.startColor = color;
            line.endColor = color;

            block ??= new MaterialPropertyBlock();
            line.GetPropertyBlock(block);
            block.SetFloat(MotionScaleId, reducedMotion ? 0f : 1f);
            line.SetPropertyBlock(block);
            line.enabled = true;
        }

        private void UpdatePoints(Vector3 start, Vector3 end, float amplitude)
        {
            var span = end - start;
            var length = span.magnitude;
            var direction = length > 0.0001f ? span / length : Vector3.forward;

            // Wave mostly across the screen (so it reads from first person),
            // plus a smaller out-of-plane component for a twisting look.
            var cam = Camera.main;
            var viewForward = cam != null ? cam.transform.forward : Vector3.forward;
            var side = Vector3.Cross(direction, viewForward);
            if (side.sqrMagnitude < 0.0001f)
            {
                side = Vector3.Cross(direction, Vector3.up);
            }

            side.Normalize();
            var lift = Vector3.Cross(side, direction);
            var waveNumber = Mathf.PI * 2f / Mathf.Max(0.05f, waveLength);

            for (var index = 0; index < points.Length; index++)
            {
                var t = index / (float)segments;
                var distance = t * length;
                var envelope = Mathf.Sin(t * Mathf.PI);
                var primary = Mathf.Sin(distance * waveNumber - wavePhase);
                var secondary = Mathf.Sin(distance * waveNumber * 1.7f - wavePhase * 1.3f + 1.1f);
                var offset = (side * primary + lift * (0.5f * secondary)) * (amplitude * envelope);
                points[index] = start + span * t + offset;
            }

            line.SetPositions(points);
        }

        private void OnDisable()
        {
            visibility = 0f;
            target = null;
            if (line != null)
            {
                line.enabled = false;
            }
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                var found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
