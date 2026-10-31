using System;
using RogueLikeEngine.Surfaces;
using UnityEngine;

namespace RogueLikeEngine.Systems.Movements
{
    /// <summary>Optional distance-paced surface effects owned by Movement, for 3D ground or top-down 2D surface areas.</summary>
    [Serializable]
    public class MovementFootsteps
    {
        [SerializeField] private bool m_enabled;
        [SerializeField] private SurfaceEffects m_walk;
        [SerializeField] private SurfaceEffects m_run;
        [SerializeField] private SurfaceEffects m_cover;
        [Tooltip("Only ground/surface layers, excluding the character. 2D surface areas may be triggers.")]
        [SerializeField] private LayerMask m_floorLayers;
        [Tooltip("World offset from the body's position to its ground point (or the 2D overlap point).")]
        [SerializeField] private Vector3 m_groundOffset;
        [Tooltip("In 3D, cast from this far above the ground point to this far below it.")]
        [SerializeField, Min(.01f)] private float m_probeHeight = .2f;
        [SerializeField, Min(.05f)] private float m_walkDistance = .9f;
        [SerializeField, Min(.05f)] private float m_runDistance = 1.6f;
        [SerializeField, Min(.05f)] private float m_coverDistance = .65f;
        [SerializeField, Min(0)] private float m_runSpeed = 4;

        [NonSerialized] private Vector3 m_previousPosition;
        [NonSerialized] private float m_distance;
        [NonSerialized] private bool m_hasPosition;
        [NonSerialized] private Collider2D[] m_hits2D;

        internal void Update(IBody body, bool canStep, bool inCover, float deltaTime)
        {
            if (!m_enabled || body == null || deltaTime <= 0 || !canStep || (!m_walk && !m_run && !m_cover))
            {
                Reset();
                return;
            }

            Vector3 position = body.Position;
            Vector3 velocity = body.Flatten(body.Velocity);
            float distance = m_hasPosition ? body.Flatten(position - m_previousPosition).magnitude : 0;
            m_previousPosition = position;
            m_hasPosition = true;
            float speed = velocity.magnitude;
            float maxTravel = Mathf.Max(.25f, speed * Mathf.Max(deltaTime, Time.fixedDeltaTime) * 2);
            if (speed < .2f || distance > maxTravel || !FindSurface(body, out Component collider, out Vector3 point, out Vector3 normal))
            {
                m_distance = 0;
                return;
            }

            m_distance += distance;
            bool running = speed >= m_runSpeed;
            float spacing = Mathf.Max(.05f, inCover ? m_coverDistance : running ? m_runDistance : m_walkDistance);
            if (m_distance < spacing) return;
            m_distance %= spacing;
            SurfaceEffects effects = (inCover ? m_cover : running ? m_run : m_walk) ?? m_walk;
            effects?.Play(Surface.Of(collider), point, normal, velocity);
        }

        internal void Reset()
        {
            m_hasPosition = false;
            m_distance = 0;
        }

        private bool FindSurface(IBody body, out Component collider, out Vector3 point, out Vector3 normal)
        {
            point = body.Position + m_groundOffset;
            normal = Vector3.back;
            if (body is Body2D)
            {
                m_hits2D ??= new Collider2D[1];
                ContactFilter2D filter = new() { useLayerMask = true, layerMask = m_floorLayers, useTriggers = true };
                int count = Physics2D.OverlapPoint(point, filter, m_hits2D);
                collider = count > 0 ? m_hits2D[0] : null;
                m_hits2D[0] = null;
                return collider;
            }

            float height = Mathf.Max(.01f, m_probeHeight);
            bool found = Physics.Raycast(point + Vector3.up * height, Vector3.down, out RaycastHit hit,
                height * 2, m_floorLayers, QueryTriggerInteraction.Ignore);
            collider = hit.collider;
            point = hit.point;
            normal = hit.normal;
            return found;
        }
    }
}
