using UnityEngine;
namespace Noctal
{
    [RequireComponent(typeof(SpriteRenderer), typeof(TlamaniMotor))]
    public sealed class TlamaniVisual : MonoBehaviour
    {
        [SerializeField] private Sprite[] idle, walk, jump, dash;
        private SpriteRenderer visual;
        private TlamaniMotor motor;
        private Sprite[] previous;
        private float elapsed;
        private void Awake() { visual = GetComponent<SpriteRenderer>(); motor = GetComponent<TlamaniMotor>(); }
        private void LateUpdate()
        {
            Sprite[] frames = motor.IsDashing ? dash : !motor.IsGrounded ? jump : Mathf.Abs(motor.Velocity.x) > 0.15f ? walk : idle;
            if (frames != previous) { elapsed = 0f; previous = frames; }
            elapsed += Time.deltaTime;
            if (frames != null && frames.Length > 0)
                visual.sprite = frames[Mathf.FloorToInt(elapsed * (motor.IsDashing ? 24f : 10f)) % frames.Length];
            visual.flipX = motor.Facing < 0;
            visual.color = motor.IsDashing ? new Color(0.5f, 1f, 1f) : Color.white;
        }
    }
}
