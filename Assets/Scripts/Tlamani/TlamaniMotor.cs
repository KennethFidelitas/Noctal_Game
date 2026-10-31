using UnityEngine;

namespace Noctal
{
    /// <summary>Movimiento por velocidad; Physics2D resuelve las colisiones.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class TlamaniMotor : MonoBehaviour
    {
        [Header("Movimiento (unidades/segundo)")]
        [SerializeField, Min(0.1f)] private float speed = 6f;
        [SerializeField, Min(0.1f)] private float acceleration = 60f;
        [SerializeField, Min(0.1f)] private float jumpSpeed = 12f;
        [SerializeField, Min(0.1f)] private float gravityScale = 3f;
        [SerializeField, Min(0.1f)] private float maxFallSpeed = 22f;
        [Header("Tolerancia del salto (segundos)")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBuffer = 0.12f;
        [Header("Dash horizontal")]
        [SerializeField, Min(0.1f)] private float dashSpeed = 18f;
        [SerializeField, Min(0.02f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.55f;
        [Header("Colisiones y reinicio")]
        [SerializeField] private LayerMask solidLayers = ~0;
        [SerializeField] private float killY = -20f;
        [SerializeField] private Transform spawnPoint;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private Rigidbody2D body;
        private ContactFilter2D filter;
        private Vector2 initialPosition;
        private float horizontal, coyoteRemaining, jumpRemaining, dashRemaining, cooldownRemaining;
        private int dashDirection = 1;
        private bool dashRequested, dashSpentInAir, jumpReleased, dashActive;
        private const float Skin = 0.015f;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => dashActive;
        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => body != null ? body.velocity : Vector2.zero;
        public float DashCooldownRemaining => cooldownRemaining;
        public bool CanDash => !IsDashing && cooldownRemaining <= 0f && !dashSpentInAir;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            initialPosition = body.position;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.gravityScale = gravityScale;
            body.drag = 0f;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = solidLayers, useTriggers = false };
        }
        // Update recoge pulsaciones; FixedUpdate las consume una sola vez.
        public void SetInput(float move, bool jumpPressed, bool dashPressed, bool releasedJump = false)
        {
            horizontal = Mathf.Clamp(move, -1f, 1f);
            if (jumpPressed) jumpRemaining = jumpBuffer;
            dashRequested |= dashPressed;
            jumpReleased |= releasedJump;
        }
        public void ClearInput()
        {
            horizontal = jumpRemaining = 0f;
            dashRequested = jumpReleased = false;
        }
        private void FixedUpdate()
        {
            if (body.position.y < killY) { Respawn(); return; }
            float dt = Time.fixedDeltaTime;
            // El ultimo paso de dash ya fue simulado. Cambiar estado y velocidad juntos
            // evita mostrar caminata mientras todavia se conserva el impulso del dash.
            if (dashActive && dashRemaining <= 0.000001f)
            {
                dashActive = false;
                dashRemaining = 0f;
                body.gravityScale = gravityScale;
                body.velocity = new Vector2(horizontal * speed, body.velocity.y);
            }
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
            IsGrounded = CheckGround();
            if (IsGrounded && !IsDashing) { coyoteRemaining = coyoteTime; dashSpentInAir = false; }
            else coyoteRemaining = Mathf.Max(0f, coyoteRemaining - dt);
            if (!IsDashing && Mathf.Abs(horizontal) > 0.01f) Facing = horizontal > 0f ? 1 : -1;
            if (dashRequested && CanDash)
            {
                dashActive = true;
                dashRemaining = dashDuration;
                cooldownRemaining = dashCooldown;
                dashSpentInAir = true;
                dashDirection = Facing;
                jumpRemaining = coyoteRemaining = 0f;
            }
            dashRequested = false;
            if (IsDashing)
            {
                body.gravityScale = 0f;
                Vector2 direction = Vector2.right * dashDirection;
                float distance = dashSpeed * Mathf.Min(dt, dashRemaining);
                int count = body.Cast(direction, filter, hits, distance + Skin);
                bool blocked = false;
                for (int i = 0; i < count; i++)
                {
                    if (Vector2.Dot(hits[i].normal, direction) > -0.5f) continue;
                    distance = Mathf.Min(distance, Mathf.Max(0f, hits[i].distance - Skin));
                    blocked = true;
                }
                body.velocity = direction * (distance / dt);
                dashRemaining = blocked ? 0f : Mathf.Max(0f, dashRemaining - dt);
                // La gravedad se restaura al siguiente paso, después de este desplazamiento.
            }
            else
            {
                body.gravityScale = gravityScale;
                Vector2 velocity = body.velocity;
                velocity.x = Mathf.MoveTowards(velocity.x, horizontal * speed, acceleration * dt);
                if (jumpRemaining > 0f && coyoteRemaining > 0f)
                {
                    velocity.y = jumpSpeed;
                    jumpRemaining = coyoteRemaining = 0f;
                    IsGrounded = false;
                }
                if (jumpReleased && velocity.y > 0f) velocity.y *= 0.5f;
                velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
                body.velocity = velocity;
            }
            jumpReleased = false;
            jumpRemaining = Mathf.Max(0f, jumpRemaining - dt);
        }
        private bool CheckGround()
        {
            if (body.velocity.y > 0.1f) return false;
            int count = body.Cast(Vector2.down, filter, hits, 0.045f);
            for (int i = 0; i < count; i++) if (hits[i].normal.y >= 0.65f) return true;
            return false;
        }
        public void Respawn() => RespawnAt(spawnPoint != null ? (Vector2)spawnPoint.position : initialPosition);
        public void RespawnAt(Vector2 position)
        {
            ClearInput();
            dashRemaining = cooldownRemaining = coyoteRemaining = 0f;
            dashActive = false;
            dashSpentInAir = IsGrounded = false;
            body.gravityScale = gravityScale;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = position;
            body.WakeUp();
        }
        private void OnDisable()
        {
            ClearInput();
            dashRemaining = cooldownRemaining = coyoteRemaining = 0f;
            dashActive = false;
            dashSpentInAir = IsGrounded = false;
            if (body == null) return;
            body.gravityScale = gravityScale;
            body.velocity = Vector2.zero;
        }
    }
}
