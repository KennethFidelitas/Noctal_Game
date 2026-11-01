using UnityEngine;
namespace Noctal
{
    [RequireComponent(typeof(SpriteRenderer), typeof(TlamaniMotor))]
    public sealed class TlamaniVisual : MonoBehaviour
    {
        [SerializeField] private Sprite[] idle, walk, jump, dash, attack;
        private SpriteRenderer visual;
        private TlamaniMotor motor;
        private Sprite[] previous;
        private float elapsed;
        private TlamaniAbilities abilities;
        private TlamaniVitals vitals;
        private void Awake() { visual = GetComponent<SpriteRenderer>(); motor = GetComponent<TlamaniMotor>(); abilities = GetComponent<TlamaniAbilities>(); vitals = GetComponent<TlamaniVitals>(); }
        private void LateUpdate()
        {
            visual.enabled = abilities == null || !abilities.IsPuma;
            Sprite[] frames = abilities != null && abilities.IsAttacking && attack != null && attack.Length > 0 ? attack : motor.IsDashing ? dash : !motor.IsGrounded ? jump : Mathf.Abs(motor.Velocity.x) > 0.15f ? walk : idle;
            if (frames != previous) { elapsed = 0f; previous = frames; }
            elapsed += Time.deltaTime;
            if (frames != null && frames.Length > 0)
                visual.sprite = frames == attack && abilities != null ? frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(abilities.AttackProgress * frames.Length))] : frames[Mathf.FloorToInt(elapsed * (motor.IsDashing ? 24f : 10f)) % frames.Length];
            visual.flipX = motor.Facing < 0;
            visual.color = motor.IsDashing ? new Color(0.5f, 1f, 1f) : Color.white;
            if (vitals != null && vitals.IsInvulnerable && Mathf.FloorToInt(Time.time * 14) % 2 == 0) visual.color = new Color(1,0.3f,0.4f);
        }
    }
}
