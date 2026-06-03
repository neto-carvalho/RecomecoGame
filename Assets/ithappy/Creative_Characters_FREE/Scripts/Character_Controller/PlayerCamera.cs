using UnityEngine;

namespace Controller
{
    public abstract class PlayerCamera : MonoBehaviour
    {
        private const float MIN_DISTANCE = 1f;
        private const float MAX_DISTANCE = 10f;

        private const float TARGET_DISTANCE = MAX_DISTANCE * 2f;

        protected Transform m_Player;

        [SerializeField, Range(0f, 1f)]
        private float m_SensitivityX = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float m_SensitivityY = 0.1f;

        [SerializeField, Range(0f, 1f)]
        private float m_Zoom = 0.5f;
        [SerializeField, Range(0f, 1f)]
        private float m_SensetivityZoom = 0.1f;

        [SerializeField, Range(0, 90f)]
        private float m_MinAngle = 0f;
        [SerializeField, Range(0, 90f)]
        private float m_MaxAngle = 50f;

        protected Transform m_Target;
        protected Transform m_Transform;

        protected Vector2 m_Angles;
        protected float m_Distance;

        public Vector3 Target
        {
            get
            {
                EnsureTarget();
                if (m_Target != null)
                    return m_Target.position;
                if (m_Player != null)
                    return m_Player.position + m_Player.forward * TargetDistance;
                return m_Transform.position + m_Transform.forward * 5f;
            }
        }

        /// <summary>Distância do alvo de movimento; acompanha a escala do jogador (ex.: scale 0.3).</summary>
        public float TargetDistance => TARGET_DISTANCE * GetPlayerScaleFactor();

        protected float GetPlayerScaleFactor()
        {
            if (m_Player == null)
                return 1f;
            return Mathf.Max(0.15f, m_Player.lossyScale.y);
        }

        protected virtual void Awake()
        {
            m_Transform = transform;
            EnsureTarget();
        }

        public void SetPlayer(Transform player)
        {
            m_Player = player;
            EnsureTarget();
        }

        /// <summary>
        /// Alvo da câmara fica como filho da câmara (não do parent da cena antiga) para sobreviver a LoadScene.
        /// </summary>
        protected void EnsureTarget()
        {
            if (m_Target != null)
                return;

            m_Transform = transform;
            var go = new GameObject($"Target_{gameObject.name}");
            go.hideFlags = HideFlags.HideInHierarchy;
            m_Target = go.transform;
            m_Target.SetParent(m_Transform, false);
        }

        public virtual void SetInput(in Vector2 delta, float scroll)
        {
            m_Angles += new Vector2(delta.y * m_SensitivityY, delta.x * m_SensitivityX) * 360f;
            m_Angles.x = Mathf.Clamp(m_Angles.x, m_MinAngle, m_MaxAngle);

            m_Zoom += scroll * m_SensetivityZoom;
            m_Zoom = Mathf.Clamp01(m_Zoom);

            var baseDistance = (1f - m_Zoom) * (MAX_DISTANCE - MIN_DISTANCE) + MIN_DISTANCE;
            m_Distance = baseDistance * GetPlayerScaleFactor();
        }
    }
}