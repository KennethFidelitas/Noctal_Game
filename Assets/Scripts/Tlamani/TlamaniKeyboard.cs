using UnityEngine;
namespace Noctal
{
    [RequireComponent(typeof(TlamaniMotor))]
    public sealed class TlamaniKeyboard : MonoBehaviour
    {
        private TlamaniMotor motor;
        private void Awake() => motor = GetComponent<TlamaniMotor>();
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { motor.Respawn(); return; }
            float move = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                       - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            motor.SetInput(move, Input.GetKeyDown(KeyCode.Space),
                Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift), Input.GetKeyUp(KeyCode.Space));
        }
        private void OnApplicationFocus(bool focused) { if (!focused && motor != null) motor.ClearInput(); }
        private void OnDisable() { if (motor != null) motor.ClearInput(); }
    }
}
