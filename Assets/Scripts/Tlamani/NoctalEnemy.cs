using UnityEngine;

namespace Noctal
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class NoctalEnemy : MonoBehaviour
    {
        public enum EnemyKind { Apasanka, Pilpintu, Guerrero, Nawi, Llanthu }
        public enum EnemyState { Patrol, Telegraph, Attack, Recover, Dead }
        public EnemyKind kind;
        public int maximumHealth = 3;
        public float alertRange = 6, patrolRadius = 2;
        public TlamaniVitals target;
        public LineRenderer aim;
        public TextMesh label;
        [Tooltip("Permite suspender la IA sin ocultar el estado visual del enemigo.")]
        public bool behaviorEnabled = true;
        private Rigidbody2D body;
        private CapsuleCollider2D shape;
        private NoctalGlyph glyph;
        private Vector2 home, lockedTarget, diveDirection;
        private float stateRemaining, exposedUntil;
        private int facing = -1, attackNumber;
        private bool initialized;
        public int Health { get; private set; }
        public EnemyState State { get; private set; }
        public bool IsDead => State == EnemyState.Dead;
        public bool IsExposed => Time.time < exposedUntil;
        public bool IsPhaseTwo => kind == EnemyKind.Llanthu && Health <= maximumHealth / 2;
        public int Facing => facing;
        public bool Guarding => !IsExposed && (kind == EnemyKind.Llanthu || (kind == EnemyKind.Guerrero && State == EnemyState.Patrol));
        public string DisplayName => kind == EnemyKind.Apasanka ? "APASANKA" : kind == EnemyKind.Pilpintu ? "PILPINTU" :
            kind == EnemyKind.Guerrero ? "GUERRERO CORRUPTO" : kind == EnemyKind.Nawi ? "ÑAWI" : "LLANTHU";
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); shape = GetComponent<CapsuleCollider2D>(); glyph = GetComponentInChildren<NoctalGlyph>();
            home = transform.position; initialized = true;
            body.freezeRotation = true; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.gravityScale = kind == EnemyKind.Pilpintu || kind == EnemyKind.Nawi ? 0 : 3;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            ResetActor();
        }
        private void Start() { if (target == null) target = FindObjectOfType<TlamaniVitals>(); }
        public void ExposeToLight(float seconds) { if (!IsDead) exposedUntil = Time.time + Mathf.Max(0, seconds); }
        public bool ReceiveHit(int damage, Vector2 source)
        {
            if (IsDead || damage <= 0) return false;
            bool frontal = (source.x - transform.position.x) * facing >= 0;
            if (Guarding && (kind == EnemyKind.Llanthu || frontal)) return false;
            Health = Mathf.Max(0, Health - damage);
            if (Health == 0)
            {
                State = EnemyState.Dead; body.velocity = Vector2.zero; body.simulated = false; shape.enabled = false;
                foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                if (kind == EnemyKind.Llanthu) NoctalLevel.Instance?.DefeatBoss();
            }
            return true;
        }
        public void ResetActor()
        {
            if (!initialized) return;
            Health = maximumHealth; State = EnemyState.Patrol; stateRemaining = 0; exposedUntil = -1; attackNumber = 0;
            body.simulated = true; shape.enabled = true; body.position = home; body.velocity = Vector2.zero; facing = -1;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = true;
            if (aim != null) aim.enabled = false;
        }
        private void FixedUpdate()
        {
            if (IsDead || !behaviorEnabled) return;
            float dt = Time.fixedDeltaTime; stateRemaining -= dt;
            if (target == null || target.IsDead)
            { body.velocity = new Vector2(0, body.gravityScale == 0 ? 0 : body.velocity.y); return; }
            Vector2 delta = (Vector2)target.transform.position - body.position;
            float distance = delta.magnitude;
            if (State == EnemyState.Telegraph && stateRemaining <= 0) BeginAttack();
            else if (State == EnemyState.Attack && stateRemaining <= 0)
            { State = EnemyState.Recover; stateRemaining = (kind == EnemyKind.Pilpintu ? 1.2f : IsPhaseTwo ? 0.55f : 0.95f) * (IsExposed ? 1.5f : 1); }
            else if (State == EnemyState.Recover && stateRemaining <= 0) State = EnemyState.Patrol;

            if (State == EnemyState.Patrol)
            {
                bool seesPlayer = distance < alertRange && Physics2D.Linecast(body.position, target.transform.position, NoctalLayers.WorldMask).collider == null;
                if (seesPlayer) facing = delta.x >= 0 ? 1 : -1;
                if (kind == EnemyKind.Pilpintu)
                {
                    Vector2 hover = new Vector2(seesPlayer ? target.transform.position.x : home.x, home.y + Mathf.Sin(Time.time * 2) * .3f);
                    body.velocity = Vector2.ClampMagnitude((hover - body.position) * 2, IsExposed ? 1.25f : 2.5f);
                    if (seesPlayer && Mathf.Abs(delta.x) < 1.8f) WindUp();
                }
                else if (kind == EnemyKind.Nawi)
                { body.velocity = Vector2.zero; if (seesPlayer) WindUp(); }
                else if (kind == EnemyKind.Llanthu)
                { if (seesPlayer) WindUp(); else MoveGround(0); }
                else if (kind == EnemyKind.Guerrero)
                { if (seesPlayer && distance < 1.8f) WindUp(); else MoveGround(seesPlayer ? facing * 1.7f : PatrolVelocity()); }
                else
                { if (seesPlayer) WindUp(); else MoveGround(PatrolVelocity()); }
            }
            else if (State == EnemyState.Telegraph)
            {
                body.velocity = new Vector2(0, body.gravityScale == 0 ? 0 : body.velocity.y);
                if (kind == EnemyKind.Nawi && aim != null) DrawAim(new Color(1,.5f,.25f,.6f), 0.025f);
            }
            else if (State == EnemyState.Attack)
            {
                if (kind == EnemyKind.Apasanka) MoveGround(facing * 6);
                else if (kind == EnemyKind.Pilpintu) body.velocity = diveDirection * (IsExposed ? 4 : 8);
                else if (kind == EnemyKind.Llanthu) MoveGround(facing * (IsPhaseTwo ? 4 : 2.4f));
                else MoveGround(0);
                if ((kind == EnemyKind.Guerrero || kind == EnemyKind.Llanthu) && distance < (kind == EnemyKind.Llanthu ? 2.1f : 1.7f) && delta.x * facing > -0.1f
                    && Physics2D.Linecast(body.position, target.transform.position, NoctalLayers.WorldMask).collider == null)
                    target.TakeDamage(kind == EnemyKind.Llanthu ? 2 : 1);
            }
            else
            {
                body.velocity = new Vector2(0, body.gravityScale == 0 ? 0 : body.velocity.y);
                if (aim != null) aim.enabled = false;
            }
            if ((kind == EnemyKind.Apasanka && State != EnemyState.Recover || kind == EnemyKind.Pilpintu && State == EnemyState.Attack)
                && shape.bounds.Intersects(target.GetComponent<Collider2D>().bounds)) target.TakeDamage(1);
        }
        private float PatrolVelocity()
        {
            if (body.position.x < home.x - patrolRadius) facing = 1;
            if (body.position.x > home.x + patrolRadius) facing = -1;
            return facing * 1.1f;
        }
        private void MoveGround(float speed)
        {
            if (body.gravityScale == 0) { body.velocity = Vector2.zero; return; }
            if (Mathf.Abs(speed) > 0)
            {
                Vector2 foot = body.position + new Vector2(Mathf.Sign(speed) * (shape.size.x * .5f + .15f), -shape.size.y * .5f + .15f);
                if (Physics2D.Raycast(foot, Vector2.down, .55f, NoctalLayers.WorldMask).collider == null)
                { speed = 0; if (State == EnemyState.Patrol) facing = -facing; }
            }
            body.velocity = new Vector2(speed * (IsExposed ? .5f : 1), body.velocity.y);
        }
        private void WindUp()
        {
            if (kind == EnemyKind.Llanthu && IsPhaseTwo && attackNumber > 0 && attackNumber % 3 == 0)
                body.position = new Vector2(home.x + (target.transform.position.x < home.x ? 4 : -4), home.y);
            lockedTarget = target.transform.position;
            diveDirection = (lockedTarget - body.position).normalized;
            facing = lockedTarget.x >= body.position.x ? 1 : -1;
            State = EnemyState.Telegraph;
            stateRemaining = (kind == EnemyKind.Apasanka ? .45f : kind == EnemyKind.Nawi ? .9f : IsPhaseTwo ? .55f : .75f) * (IsExposed ? 1.4f : 1);
        }
        private void BeginAttack()
        {
            State = EnemyState.Attack; stateRemaining = kind == EnemyKind.Pilpintu ? .5f : .38f; attackNumber++;
            if (kind == EnemyKind.Nawi)
            {
                DrawAim(new Color(1,.15f,.5f), .12f);
                var hit = Physics2D.Raycast(body.position, (lockedTarget - body.position).normalized, alertRange,
                    NoctalLayers.WorldMask | (1 << NoctalLayers.Player));
                if (hit.collider != null) hit.collider.GetComponent<TlamaniVitals>()?.TakeDamage(1);
            }
            if (kind == EnemyKind.Llanthu)
            {
                Vector2 delta = lockedTarget - body.position;
                if (delta.magnitude > 2.0f || attackNumber % 2 == 0)
                    NoctalProjectile.Launch(body.position, delta, IsPhaseTwo ? 7 : 5, 1);
                if (IsPhaseTwo && attackNumber % 2 == 1)
                {
                    Vector2 feet = body.position + Vector2.down * (shape.size.y * .5f - .3f);
                    NoctalProjectile.Launch(feet, Vector2.left, 6, 1);
                    NoctalProjectile.Launch(feet, Vector2.right, 6, 1);
                }
            }
        }
        private void DrawAim(Color color, float width)
        {
            if (aim == null) return;
            Vector2 direction = (lockedTarget - body.position).normalized;
            var hit = Physics2D.Raycast(body.position, direction, alertRange, NoctalLayers.WorldMask | (1 << NoctalLayers.Player));
            aim.enabled = true; aim.startColor = aim.endColor = color; aim.startWidth = aim.endWidth = width;
            aim.SetPosition(0, body.position); aim.SetPosition(1, hit.collider != null ? hit.point : body.position + direction * alertRange);
        }
        private void LateUpdate()
        {
            if (IsDead) return;
            if (glyph != null)
            {
                glyph.transform.localScale = new Vector3(facing, 1, 1);
                glyph.Tint(IsExposed ? new Color(.6f,1,1) : State == EnemyState.Telegraph ? new Color(1.6f,.5f,.5f) : Color.white);
            }
            if (label != null) label.text = DisplayName + "  " + Health + "/" + maximumHealth +
                (IsExposed ? "\nVULNERABLE" : Guarding ? "\nGUARDIA" : State == EnemyState.Telegraph ? "\n¡PREPARA ATAQUE!" : "");
        }
    }
}
