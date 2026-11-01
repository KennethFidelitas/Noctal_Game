using System;
using UnityEngine;

namespace Noctal
{
    [DisallowMultipleComponent, RequireComponent(typeof(TlamaniMotor))]
    public sealed class TlamaniVitals : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 6;
        [SerializeField, Min(0f)] private float damageGrace = 0.9f;
        private float graceRemaining;
        private TlamaniMotor motor;
        public int Health { get; private set; }
        public int MaximumHealth => maximumHealth;
        public bool IsDead => Health <= 0;
        public bool IsInvulnerable => graceRemaining > 0;
        public event Action Died;
        private void Awake() { motor = GetComponent<TlamaniMotor>(); Health = maximumHealth; }
        private void Update() => graceRemaining = Mathf.Max(0, graceRemaining - Time.deltaTime);
        public bool TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead || IsInvulnerable) return false;
            Health = Mathf.Max(0, Health - amount);
            graceRemaining = damageGrace;
            if (Camera.main != null) Camera.main.GetComponent<TlamaniCamera>()?.Shake();
            if (IsDead) { motor.enabled = false; Died?.Invoke(); }
            return true;
        }
        public void Heal(int amount) { if (!IsDead) Health = Mathf.Clamp(Health + Mathf.Max(0, amount), 0, maximumHealth); }
        public void Restore()
        {
            Health = maximumHealth; graceRemaining = damageGrace;
            motor.enabled = true;
        }
    }
}
