using UnityEngine;
namespace Noctal
{
    public sealed class NoctalPickup : MonoBehaviour
    {
        public enum PickupKind { Energy, Health, Anima }
        public PickupKind kind;
        public float amount = 35, rechargeSeconds;
        private float availableAt;
        private bool consumed;
        public bool IsAvailable => !consumed;
        private Renderer[] visuals;
        private void Awake() => visuals = GetComponentsInChildren<Renderer>();
        private void Update()
        {
            if (consumed && rechargeSeconds > 0 && Time.time >= availableAt) ResetPickup();
        }
        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other.GetComponent<TlamaniAbilities>());
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other.GetComponent<TlamaniAbilities>());
        public bool TryCollect(TlamaniAbilities player)
        {
            if (consumed || player == null || player.GetComponent<TlamaniVitals>().IsDead) return false;
            if (kind == PickupKind.Energy && player.Energy >= player.MaximumEnergy) return false;
            if (kind == PickupKind.Health && player.GetComponent<TlamaniVitals>().Health >= player.GetComponent<TlamaniVitals>().MaximumHealth) return false;
            consumed = true; availableAt = Time.time + rechargeSeconds;
            if (kind == PickupKind.Anima) player.UnlockPuma();
            else if (kind == PickupKind.Health) player.GetComponent<TlamaniVitals>().Heal(Mathf.RoundToInt(amount));
            else player.AddEnergy(amount);
            foreach (var visual in visuals) visual.enabled = false;
            return true;
        }
        public void ResetPickup()
        {
            consumed = false;
            if (visuals == null) visuals = GetComponentsInChildren<Renderer>();
            foreach (var visual in visuals) visual.enabled = true;
        }
    }
}
