using UnityEngine;

namespace Controller
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CharacterMover))]
    public class MovePlayerInput : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";
        [SerializeField]
        private string m_VerticalAxis = "Vertical";
        [SerializeField]
        private string m_JumpButton = "Jump";
        [SerializeField]
        private KeyCode m_RunKey = KeyCode.LeftShift;

        [Header("Camera")]
        [SerializeField]
        private PlayerCamera m_Camera;
        [SerializeField]
        private string m_MouseX = "Mouse X";
        [SerializeField]
        private string m_MouseY = "Mouse Y";
        [SerializeField]
        private string m_MouseScroll = "Mouse ScrollWheel";

        private CharacterMover m_Mover;

        private Vector2 m_Axis;
        private bool m_IsRun;
        private bool m_IsJump;

        private Vector3 m_Target;
        private Vector2 m_MouseDelta;
        private float m_Scroll;
        private bool m_CameraWarningLogged;

        private void Awake()
        {
            m_Mover = GetComponent<CharacterMover>();
            ResolveCamera(silent: true);
        }

        private void LateUpdate()
        {
            if (m_Camera != null)
                return;
            ResolveCamera(silent: true);
        }

        void ResolveCamera(bool silent = false)
        {
            SnapFeetToGround(transform);

            if (m_Camera != null)
            {
                m_Camera.SetPlayer(transform);
                return;
            }

            if (Camera.main != null)
                m_Camera = Camera.main.GetComponent<PlayerCamera>();

            if (m_Camera == null)
                m_Camera = FindFirstObjectByType<PlayerCamera>();

            if (m_Camera != null)
            {
                m_Camera.SetPlayer(transform);
                m_CameraWarningLogged = false;
                return;
            }

            if (!silent && !m_CameraWarningLogged)
            {
                m_CameraWarningLogged = true;
                Debug.LogWarning(
                    "MovePlayerInput: nenhuma câmara com PlayerCamera/ThirdPersonCamera. " +
                    "Aguardando GameplayCamera ou use Recomeco → Cenas → Completar gameplay na cena.");
            }
        }

        static void SnapFeetToGround(Transform character)
        {
            var cc = character.GetComponent<CharacterController>();
            if (cc == null)
                return;

            var snap = character.GetComponent<CharacterGroundSnap>();
            if (snap != null)
            {
                snap.SnapNow();
                return;
            }

            CharacterGroundSnap.FitControllerToWorldScale(cc);
            CharacterGroundSnap.TrySnap(character, cc);
        }

        private void Update()
        {
            GatherInput();
            SetInput();
        }

        public void GatherInput()
        {
            m_Axis = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
            m_IsRun = Input.GetKey(m_RunKey);
            m_IsJump = Input.GetButton(m_JumpButton);

            m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;
            m_MouseDelta = new Vector2(Input.GetAxis(m_MouseX), Input.GetAxis(m_MouseY));
            m_Scroll = Input.GetAxis(m_MouseScroll);
        }

        public void BindMover(CharacterMover mover)
        {
            m_Mover = mover;
        }

        /// <summary>Re-liga a câmera (útil após criar GameplayCamera em runtime).</summary>
        public void RefreshCameraBinding()
        {
            m_CameraWarningLogged = false;
            ResolveCamera(silent: false);
        }

        /// <summary>Atribui a câmera third-person diretamente (setup de cena).</summary>
        public void BindPlayerCamera(PlayerCamera camera)
        {
            m_Camera = camera;
            if (m_Camera != null)
                m_Camera.SetPlayer(transform);
            m_CameraWarningLogged = false;
        }

        public void SetInput()
        {
            if (m_Mover != null)
            {
                var target = m_Target;
                if ((target - transform.position).sqrMagnitude < 0.25f)
                    target = transform.position + transform.forward * 8f;
                m_Mover.SetInput(in m_Axis, in target, in m_IsRun, m_IsJump);
            }

            if (m_Camera != null)
            {
                m_Camera.SetInput(in m_MouseDelta, m_Scroll);
            }
        }
    }
}