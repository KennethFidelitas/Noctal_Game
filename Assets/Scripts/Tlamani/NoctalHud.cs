using UnityEngine;
using UnityEngine.UI;
namespace Noctal
{
    public sealed class NoctalHud : MonoBehaviour
    {
        public TlamaniVitals player;
        public NoctalLevel level;
        public Text health, energy, room, hint, boss, overlay;
        private void Update()
        {
            if (player == null || level == null) return;
            var abilities = player.GetComponent<TlamaniAbilities>();
            health.text = "VIDA  " + player.Health + " / " + player.MaximumHealth;
            energy.text = "LUZ  " + Mathf.CeilToInt(abilities.Energy) + " / " + abilities.MaximumEnergy +
                "\n" + (abilities.IsPuma ? "ÁNIMA · PUMA" : abilities.PumaUnlocked ? "Q · TRANSFORMAR" : "ÁNIMA POR DESCUBRIR");
            room.text = level.roomNames[level.Room];
            hint.text = Time.unscaledTime < abilities.HintUntil ? abilities.Hint : level.roomHints[level.Room];
            boss.text = level.Room == 4 && !level.BossDefeated && level.boss != null ? "LLANTHU  " + level.boss.Health + " / " + level.boss.maximumHealth +
                (level.boss.IsPhaseTwo ? "  ·  FASE II" : "  ·  FASE I") + (level.boss.IsExposed ? "\nVULNERABLE A LOS GOLPES" : "\nROMPE SU DEFENSA CON LUZ") : "";
            overlay.text = level.Won ? "TEMPLO PURIFICADO\nTenebris ha sido sellado.\nR: volver a probar" :
                player.IsDead ? "TLAMANI HA CAÍDO\nR: reintentar desde esta sala" : level.Paused ? "PAUSA\nEsc: continuar  ·  R: reintentar" : "";
        }
    }
}
