using UnityEngine;

namespace Controller
{
    public class ThirdPersonCamera : PlayerCamera
    {
        [SerializeField, Range(0f, 2f)]
        private float m_Offset = 1.5f;
        [SerializeField, Range(0f, 360f)]
        private float m_CameraSpeed = 90f;

        private Vector3 m_LookPoint;
        private Vector3 m_TargetPos;
        private Vector3 m_RawTargetPos;

        private void LateUpdate()
        {
            Move(Time.deltaTime);
        }

        public override void SetInput(in Vector2 delta, float scroll)
        {
            base.SetInput(delta, scroll);

            var dir = new Vector3(0, 0, -m_Distance);
            var rot = Quaternion.Euler(m_Angles.x, m_Angles.y, 0f);

            var scale = GetPlayerScaleFactor();
            var playerPos = (m_Player == null) ? Vector3.zero : m_Player.position;
            m_LookPoint = playerPos + m_Offset * scale * Vector3.up;
            m_RawTargetPos = m_LookPoint + rot * dir;
            m_TargetPos = ResolveCameraCollision(m_LookPoint, m_RawTargetPos);
        }

        private void Move(float deltaTime)
        {
            camera();
            target();

            void camera()
            {
                var desiredPos = ResolveCameraCollision(m_LookPoint, m_TargetPos);
                var direction = desiredPos - m_Transform.position;
                var moveSpeed = GetCollisionMoveSpeed(m_CameraSpeed, m_RawTargetPos, desiredPos);
                var delta = moveSpeed * deltaTime;

                if(delta * delta > direction.sqrMagnitude)
                {
                    m_Transform.position = desiredPos;
                }
                else
                {
                    m_Transform.position += delta * direction.normalized;
                }

                m_Transform.LookAt(m_LookPoint);
            }

            void target()
            {
                EnsureTarget();
                if (m_Target == null)
                    return;

                m_Target.position = m_Transform.position + m_Transform.forward * TargetDistance;
            }
        }
    }
}