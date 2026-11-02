using UnityEngine;
using UnityEngine.UI;
namespace Noctal
{
    public sealed class TlamaniHud : MonoBehaviour
    {
        [SerializeField] private TlamaniMotor player;
        [SerializeField] private Text status;
        private void Update()
        {
            if (player == null || status == null) return;
            status.text = player.IsDashing ? "DASH" : player.CanDash ? "DASH LISTO" :
                player.DashCooldownRemaining > 0f ? $"DASH {player.DashCooldownRemaining:0.0}s" : "ATERRIZA PARA RECARGAR";
        }
    }
}
