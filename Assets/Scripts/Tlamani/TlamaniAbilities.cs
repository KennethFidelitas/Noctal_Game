using System.Collections.Generic;
using UnityEngine;

namespace Noctal
{
    [DisallowMultipleComponent, RequireComponent(typeof(TlamaniMotor), typeof(TlamaniVitals))]
    public sealed class TlamaniAbilities : MonoBehaviour
    {
        [Header("Arma de la orden Inti")]
        [SerializeField] private float attackDuration = 0.36f, attackCooldown = 0.45f;
        [SerializeField] private Vector2 attackSize = new Vector2(1.35f, 1.2f);
        [Header("Artefacto de luz")]
        [SerializeField] private float maximumEnergy = 100, lightCost = 18, lightRange = 5.5f;
        [SerializeField] private LineRenderer beam;
        [Header("Anima")]
        [SerializeField] private GameObject pumaVisual;
        private TlamaniMotor motor;
        private TlamaniVitals vitals;
        private CapsuleCollider2D capsule;
        private Vector2 humanSize, humanOffset;
        private readonly Collider2D[] overlaps = new Collider2D[24];
        private readonly HashSet<NoctalEnemy> struck = new HashSet<NoctalEnemy>();
        private float attackRemaining, cooldown;
        private bool lightHeld;
        public float Energy { get; private set; }
        public float MaximumEnergy => maximumEnergy;
        public bool IsAttacking => attackRemaining > 0;
        public float AttackProgress => 1 - attackRemaining / attackDuration;
        public bool IsLighting { get; private set; }
        public bool PumaUnlocked { get; private set; }
        public bool IsPuma { get; private set; }
        public Vector2 BeamEnd { get; private set; }
        public string Hint { get; private set; }
        public float HintUntil { get; private set; }
        private void Awake()
        {
            motor = GetComponent<TlamaniMotor>(); vitals = GetComponent<TlamaniVitals>();
            capsule = GetComponent<CapsuleCollider2D>(); humanSize = capsule.size; humanOffset = capsule.offset;
            Energy = maximumEnergy; vitals.Died += CancelActions;
            if (beam != null) beam.enabled = false;
            if (pumaVisual != null) pumaVisual.SetActive(false);
        }
        private void OnDestroy() { if (vitals != null) vitals.Died -= CancelActions; }
        public void Notify(string message) { Hint = message; HintUntil = Time.unscaledTime + 3.5f; }
        public bool TryAttack()
        {
            if (vitals.IsDead || cooldown > 0 || Time.timeScale == 0 || motor.IsDashing) return false;
            lightHeld = false; IsLighting = false;
            attackRemaining = attackDuration; cooldown = attackCooldown; struck.Clear();
            return true;
        }
        public void SetLightHeld(bool held) => lightHeld = held;
        public void AddEnergy(float amount) => Energy = Mathf.Clamp(Energy + Mathf.Max(0, amount), 0, maximumEnergy);
        public void UnlockPuma()
        {
            PumaUnlocked = true;
            Notify("ANIMA DESPERTADA  ·  Q: forma de puma. Más velocidad, salto y garras.");
        }
        public bool TryTogglePuma()
        {
            if (vitals.IsDead || IsAttacking || Time.timeScale == 0) return false;
            if (!PumaUnlocked) { Notify("Busca el altar de ánima en el Santuario Corrompido."); return false; }
            if (IsPuma && Physics2D.OverlapCapsule((Vector2)transform.position + humanOffset + Vector2.up * 0.02f,
                humanSize - Vector2.one * 0.04f, CapsuleDirection2D.Vertical, 0, NoctalLayers.WorldMask) != null)
            { Notify("No hay espacio para recuperar la forma humana."); return false; }
            SetForm(!IsPuma); return true;
        }
        private void SetForm(bool puma)
        {
            IsPuma = puma;
            capsule.size = puma ? new Vector2(0.95f, 0.65f) : humanSize;
            capsule.offset = puma ? new Vector2(-0.10f, -0.295f) : humanOffset;
            capsule.direction = puma ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            motor.MoveMultiplier = puma ? 1.45f : 1f; motor.JumpMultiplier = puma ? 1.15f : 1f;
            if (pumaVisual != null) pumaVisual.SetActive(puma);
        }
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            cooldown = Mathf.Max(0, cooldown - dt);
            if (vitals.IsDead) { CancelActions(); return; }
            if (attackRemaining > 0)
            {
                attackRemaining = Mathf.Max(0, attackRemaining - dt);
                if (AttackProgress >= 0.22f && AttackProgress <= 0.72f) Strike();
            }
            IsLighting = lightHeld && !IsAttacking && Energy > 0;
            if (IsLighting) Illuminate(dt);
            if (beam != null) beam.enabled = IsLighting;
        }
        private void LateUpdate()
        {
            if (pumaVisual == null || !IsPuma) return;
            pumaVisual.transform.localScale = new Vector3(motor.Facing, 1, 1);
            pumaVisual.transform.localPosition = new Vector3(-0.10f, -0.62f + (motor.IsGrounded ? Mathf.Abs(Mathf.Sin(Time.time * 18)) * 0.025f : 0), 0);
            var glyph = pumaVisual.GetComponent<NoctalGlyph>();
            if (glyph != null) glyph.Tint(vitals.IsInvulnerable && Mathf.FloorToInt(Time.time * 14) % 2 == 0 ? new Color(1,0.4f,0.4f) : Color.white);
        }
        private void Strike()
        {
            Vector2 origin = transform.position;
            Vector2 center = origin + new Vector2(motor.Facing * 0.80f, IsPuma ? -0.20f : 0.05f);
            int count = Physics2D.OverlapBoxNonAlloc(center, attackSize, 0, overlaps, 1 << NoctalLayers.Enemy);
            for (int i = 0; i < count; i++)
            {
                var enemy = overlaps[i].GetComponentInParent<NoctalEnemy>();
                if (enemy == null || struck.Contains(enemy)) continue;
                if (Physics2D.Linecast(origin, overlaps[i].bounds.center, NoctalLayers.WorldMask).collider != null) continue;
                struck.Add(enemy);
                if (enemy.ReceiveHit(IsPuma ? 2 : 1, origin)) AddEnergy(enemy.IsDead ? 20 : 4);
            }
        }
        private void Illuminate(float dt)
        {
            Energy = Mathf.Max(0, Energy - lightCost * dt);
            Vector2 origin = (Vector2)transform.position + new Vector2(motor.Facing * 0.20f, IsPuma ? -0.15f : 0.10f);
            Vector2 direction = Vector2.right * motor.Facing;
            BeamEnd = origin + direction * lightRange;
            var hits = Physics2D.RaycastAll(origin, direction, lightRange, NoctalLayers.LightMask);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                var enemy = hit.collider.GetComponentInParent<NoctalEnemy>();
                var receiver = hit.collider.GetComponent<NoctalLightTarget>();
                if (enemy != null && enemy.IsDead) continue;
                if (enemy == null && receiver == null && hit.collider.isTrigger) continue;
                BeamEnd = hit.point;
                if (enemy != null) enemy.ExposeToLight(3f);
                if (receiver != null) receiver.Illuminate(dt);
                break;
            }
            if (beam != null) { beam.SetPosition(0, origin); beam.SetPosition(1, BeamEnd); }
        }
        public void CancelActions()
        {
            lightHeld = false; IsLighting = false; attackRemaining = cooldown = 0;
            struck.Clear(); if (beam != null) beam.enabled = false;
        }
        public void ResetForRetry()
        {
            CancelActions(); Energy = maximumEnergy; SetForm(false);
        }
        private void OnDisable() => CancelActions();
    }
}
