using UnityEngine;
namespace Noctal
{
    [RequireComponent(typeof(TlamaniMotor))]
    public sealed class TlamaniKeyboard : MonoBehaviour
    {
        private TlamaniMotor motor;
        private TlamaniAbilities abilities;
        private TlamaniVitals vitals;
        private void Awake() { motor = GetComponent<TlamaniMotor>(); abilities = GetComponent<TlamaniAbilities>(); vitals = GetComponent<TlamaniVitals>(); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (NoctalLevel.Instance != null) NoctalLevel.Instance.Retry();
                else { vitals?.Restore(); abilities?.ResetForRetry(); motor.Respawn(); }
                return;
            }
            if (NoctalLevel.Instance != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)) NoctalLevel.Instance.TogglePause();
                if (NoctalLevel.Instance.Paused || NoctalLevel.Instance.Won) return;
            }
            if (vitals != null && vitals.IsDead) return;
            float move = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                       - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            if (Mathf.Abs(move) < .01f) move = Input.GetAxisRaw("Horizontal");
            motor.SetInput(move, Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0),
                abilities == null && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)), Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.JoystickButton0));
            if (abilities != null)
            {
                if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton2)) abilities.TryAttack();
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton3)) abilities.TryTogglePuma();
                abilities.SetLightHeld(Input.GetKey(KeyCode.K) || Input.GetMouseButton(1) || Input.GetKey(KeyCode.JoystickButton4));
            }
        }
        private void OnApplicationFocus(bool focused) { if (!focused && motor != null) { motor.ClearInput(); abilities?.CancelActions(); } }
        private void OnDisable() { if (motor != null) { motor.ClearInput(); abilities?.CancelActions(); } }
    }
}
