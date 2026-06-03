using System;
using UnityEngine;

namespace Controller
{
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class CharacterMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float m_WalkSpeed = 1f;
        [SerializeField]
        private float m_RunSpeed = 4f;
        [SerializeField, Range(0f, 360f)]
        private float m_RotateSpeed = 90f;
        [SerializeField]
        private Space m_Space = Space.Self;
        [SerializeField]
        private float m_JumpHeight = 5f;

        [Header("Animator")]
        [SerializeField]
        private string m_HorizontalID = "Hor";
        [SerializeField]
        private string m_VerticalID = "Vert";
        [SerializeField]
        private string m_StateID = "State";
        [SerializeField]
        private string m_JumpID = "IsJump";
        [SerializeField]
        private LookWeight m_LookWeight = new(1f, 0.3f, 0.7f, 1f);

        private Transform m_Transform;
        private CharacterController m_Controller;
        private Animator m_Animator;

        private MovementHandler m_Movement;
        private AnimationHandler m_Animation;

        private Vector2 m_Axis;
        private Vector3 m_Target;
        private bool m_IsRun;
        private bool m_IsJump;

        private bool m_IsMoving;

        public Vector2 Axis => m_Axis;
        public Vector3 Target => m_Target;
        public bool IsRun => m_IsRun;

        private void OnValidate()
        {
            m_WalkSpeed = Mathf.Max(m_WalkSpeed, 0f);
            m_RunSpeed = Mathf.Max(m_RunSpeed, m_WalkSpeed);

            m_Movement?.SetStats(m_WalkSpeed / 3.6f, m_RunSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);
        }

        private void Awake()
        {
            m_Transform = transform;
            m_Controller = GetComponent<CharacterController>();
            BindAnimator();

            m_Movement = new MovementHandler(m_Controller, m_Transform, m_WalkSpeed / 3.6f, m_RunSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);
        }

        private void Start()
        {
            if (m_Animator == null || !m_Animator.isActiveAndEnabled)
                BindAnimator();
        }

        void BindAnimator()
        {
            PlayerAnimatorSetup.Apply(gameObject, RecomecoGameplaySettings.Instance);
            m_Animator = ResolveAnimator();
            m_Animation = m_Animator != null
                ? new AnimationHandler(m_Animator, m_HorizontalID, m_VerticalID, m_StateID, m_JumpID)
                : null;
        }

        public void RebindAnimator()
        {
            BindAnimator();
        }

        public void SetLocomotionSpeeds(float walkSpeed, float runSpeed, float rotateSpeed)
        {
            m_WalkSpeed = walkSpeed;
            m_RunSpeed = runSpeed;
            m_RotateSpeed = rotateSpeed;
            m_Movement?.SetStats(m_WalkSpeed / 3.6f, m_RunSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);
        }

        public void SetJumpHeight(float jumpHeight)
        {
            m_JumpHeight = Mathf.Max(0f, jumpHeight);
            m_Movement?.SetStats(m_WalkSpeed / 3.6f, m_RunSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);
        }

        /// <summary>
        /// Animator no filho com SkinnedMeshRenderer + Character_Movement (evita Animator extra na raiz).
        /// </summary>
        private Animator ResolveAnimator()
        {
            Animator best = null;
            var bestScore = -1;

            foreach (var a in GetComponentsInChildren<Animator>(true))
            {
                var score = 0;
                if (a.isActiveAndEnabled)
                    score += 8;

                var ctrl = a.runtimeAnimatorController;
                if (ctrl != null && ctrl.name.IndexOf("Character_Movement", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 40;
                else if (a.avatar != null && a.avatar.isHuman && a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    score += 10;
                if (a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    score += 25;
                if (a.avatar != null && a.avatar.isHuman)
                    score += 15;
                if (a.transform != m_Transform)
                    score += 5;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }

            if (best != null)
            {
                if (!best.isActiveAndEnabled)
                {
                    best.gameObject.SetActive(true);
                    best.enabled = true;
                }

                return best;
            }

            var onRoot = GetComponent<Animator>();
            if (onRoot != null && onRoot.runtimeAnimatorController != null)
                return onRoot;

            UnityEngine.Debug.LogError(
                "CharacterMover: nenhum Animator com Character_Movement. " +
                "Use Recomeco → Player → Corrigir locomoção (ithappy) na cena.");
            return null;
        }

        private void Update()
        {
            m_Movement.Move(Time.deltaTime, in m_Axis, in m_Target, m_IsRun, m_IsJump, m_IsMoving, out var animAxis, out var isAir);

            // State 0 = andar, State 1 = correr (Character_Movement). Parado = 0.
            var locomotionState = m_IsMoving ? (m_IsRun ? 1f : 0f) : 0f;

            // WASD → Hor/Vert no blend tree (direcional andar/correr).
            var driveAxis = m_IsMoving && m_Axis.sqrMagnitude > 0.01f ? m_Axis : animAxis;

            ApplyLocomotionParameters(driveAxis, locomotionState, isAir, Time.deltaTime);
        }

        void ApplyLocomotionParameters(Vector2 driveAxis, float locomotionState, bool isJump, float deltaTime)
        {
            if (m_Animator == null)
                return;

            const float blendSpeed = 10f;
            var dt = blendSpeed * deltaTime;

            var hor = Mathf.MoveTowards(m_Animator.GetFloat(m_HorizontalID), driveAxis.x, dt);
            var vert = Mathf.MoveTowards(m_Animator.GetFloat(m_VerticalID), driveAxis.y, dt);
            var state = Mathf.MoveTowards(m_Animator.GetFloat(m_StateID), locomotionState, dt);

            m_Animator.SetFloat(m_HorizontalID, hor);
            m_Animator.SetFloat(m_VerticalID, vert);
            m_Animator.SetFloat(m_StateID, state);
            m_Animator.SetBool(m_JumpID, isJump);
        }

        private void OnAnimatorIK()
        {
            if (m_Animator == null || m_Target.sqrMagnitude < 0.01f)
                return;
            m_Animation?.AnimateIK(in m_Target, m_LookWeight);
        }

        public void SetInput(in Vector2 axis, in Vector3 target, in bool isRun, in bool isJump)
        {
            m_Axis = axis;
            m_Target = target;
            m_IsRun = isRun;
            m_IsJump = isJump;

            if (m_Axis.sqrMagnitude < Mathf.Epsilon)
            {
                m_Axis = Vector2.zero;
                m_IsMoving = false;
            }
            else
            {
                m_Axis = Vector3.ClampMagnitude(m_Axis, 1f);
                m_IsMoving = true;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.normal.y > m_Controller.stepOffset)
            {
                m_Movement.SetSurface(hit.normal);
            }
        }

        [Serializable]
        private struct LookWeight
        {
            public float weight;
            public float body;
            public float head;
            public float eyes;

            public LookWeight(float weight, float body, float head, float eyes)
            {
                this.weight = weight;
                this.body = body;
                this.head = head;
                this.eyes = eyes;
            }
        }

        #region Handlers
        private class MovementHandler
        {
            private readonly CharacterController m_Controller;
            private readonly Transform m_Transform;

            private float m_WalkSpeed;
            private float m_RunSpeed;
            private float m_RotateSpeed;
            private float m_JumpHeight;

            private Space m_Space;

            private readonly float m_Luft = 75f;
            private readonly float m_JumpReload = 1f;

            private float m_TargetAngle;
            private bool m_IsRotating = false;

            private Vector3 m_Normal;
            private Vector3 m_GravityAcelleration = Physics.gravity;

            private float m_jumpTimer;

            public MovementHandler(CharacterController controller, Transform transform, float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_Controller = controller;
                m_Transform = transform;

                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;
                m_JumpHeight = jumpHeight;

                m_Space = space;
            }

            public void SetStats(float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;
                m_JumpHeight = jumpHeight;

                m_Space = space;
            }

            public void SetSurface(in Vector3 normal)
            {
                m_Normal = normal;
            }

            public void Move(float deltaTime, in Vector2 axis, in Vector3 target, bool isRun, bool isJump, bool isMoving, out Vector2 animAxis, out bool isAir)
            {
                var targetForward = Vector3.Normalize(target - m_Transform.position);

                ConvertMovement(in axis, in targetForward, out var movement);
                CaculateGravity(isJump, deltaTime, out isAir);
                Displace(deltaTime, in movement, isRun);
                Turn(in targetForward, isMoving);
                UpdateRotation(deltaTime);

                GenAnimationAxis(in movement, out animAxis);
            }

            private void ConvertMovement(in Vector2 axis, in Vector3 targetForward, out Vector3 movement)
            {
                Vector3 forward;
                Vector3 right;

                if (m_Space == Space.Self)
                {
                    forward = new Vector3(targetForward.x, 0f, targetForward.z).normalized;
                    right = Vector3.Cross(Vector3.up, forward).normalized;
                }
                else
                {
                    forward = Vector3.forward;
                    right = Vector3.right;
                }

                movement = axis.x * right + axis.y * forward;
                movement = Vector3.ProjectOnPlane(movement, m_Normal);
            }

            private void Displace(float deltaTime, in Vector3 movement, bool isRun)
            {
                Vector3 displacement = (isRun ? m_RunSpeed : m_WalkSpeed) * movement;
                displacement += m_GravityAcelleration;
                displacement *= deltaTime;

                m_Controller.Move(displacement);
            }

            private void CaculateGravity(bool isJump, float deltaTime, out bool isAir)
            {
                m_jumpTimer = Mathf.Max(m_jumpTimer - deltaTime, 0f);

                if (m_Controller.isGrounded)
                {
                    if (isJump && m_jumpTimer <= 0)
                    {
                        var gravity = Physics.gravity;
                        var length = gravity.magnitude;

                        m_GravityAcelleration += -(gravity / length) * Mathf.Sqrt(m_JumpHeight * 6f * length);
                        m_jumpTimer = m_JumpReload;
                        isAir = true;

                        return;
                    }

                    m_GravityAcelleration = Physics.gravity;
                    isAir = false;

                    return;
                }

                isAir = true;

                m_GravityAcelleration += Physics.gravity * deltaTime;
                return;
            }

            private void GenAnimationAxis(in Vector3 movement, out Vector2 animAxis)
            {
                if(m_Space == Space.Self)
                {
                    animAxis = new Vector2(Vector3.Dot(movement, m_Transform.right), Vector3.Dot(movement, m_Transform.forward));
                }
                else
                {
                    animAxis = new Vector2(Vector3.Dot(movement, Vector3.right), Vector3.Dot(movement, Vector3.forward));
                }
            }

            private void Turn(in Vector3 targetForward, bool isMoving)
            {
                var angle = Vector3.SignedAngle(m_Transform.forward, Vector3.ProjectOnPlane(targetForward, Vector3.up), Vector3.up);

                if (!m_IsRotating)
                {
                    if (!isMoving && Mathf.Abs(angle) < m_Luft)
                    {
                        m_IsRotating = false;
                        return;
                    }

                    m_IsRotating = true;
                }

                m_TargetAngle = angle;
            }

            private void UpdateRotation(float deltaTime)
            {
                if(!m_IsRotating)
                {
                    return;
                }

                var rotDelta = m_RotateSpeed * deltaTime;
                if (rotDelta + Mathf.PI * 2f + Mathf.Epsilon >= Mathf.Abs(m_TargetAngle))
                {
                    rotDelta = m_TargetAngle;
                    m_IsRotating = false;
                }
                else
                {
                    rotDelta *= Mathf.Sign(m_TargetAngle);
                }

                m_Transform.Rotate(Vector3.up, rotDelta);
            }
        }

        private class AnimationHandler
        {
            private readonly Animator m_Animator;

            private readonly string m_HorizontalID;
            private readonly string m_VerticalID;
            private readonly string m_StateID;
            private readonly string m_JumpID;

            private readonly float k_InputFlow = 4.5f;

            private float m_FlowState;
            private Vector2 m_FlowAxis;

            public AnimationHandler(Animator animator, string horizontalID, string verticalID, string stateID, string jumpID)
            {
                m_Animator = animator;

                m_HorizontalID = horizontalID;
                m_VerticalID = verticalID;
                m_StateID = stateID;
                m_JumpID = jumpID;
            }

            public void Animate(in Vector2 axis, float state, bool isJump, float deltaTime)
            {

                m_Animator.SetFloat(m_HorizontalID, m_FlowAxis.x);
                m_Animator.SetFloat(m_VerticalID, m_FlowAxis.y);

                m_Animator.SetFloat(m_StateID, Mathf.Clamp01(m_FlowState));
                m_Animator.SetBool(m_JumpID, isJump);

                m_FlowAxis = Vector2.MoveTowards(m_FlowAxis, axis, k_InputFlow * deltaTime);
                m_FlowAxis = Vector2.ClampMagnitude(m_FlowAxis, 1f);
                m_FlowState = Mathf.MoveTowards(m_FlowState, Mathf.Clamp01(state), k_InputFlow * deltaTime);
            }

            public void AnimateIK(in Vector3 target, in LookWeight lookWeight)
            {
                m_Animator.SetLookAtPosition(target);
                m_Animator.SetLookAtWeight(lookWeight.weight, lookWeight.body, lookWeight.head, lookWeight.eyes);
            }
        }
        #endregion
    }
}