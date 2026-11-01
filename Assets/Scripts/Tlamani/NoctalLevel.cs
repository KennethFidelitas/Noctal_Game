using UnityEngine;
namespace Noctal
{
    public sealed class NoctalLevel : MonoBehaviour
    {
        public static NoctalLevel Instance { get; private set; }
        public TlamaniMotor player;
        public NoctalEnemy boss;
        public string[] roomNames = { "ENTRADA DEL TEMPLO", "GALERÍA DE LA LUZ", "SALA DE ENEMIGOS", "SANTUARIO CORROMPIDO", "CÁMARA DE TENEBRIS" };
        public float[] roomStarts = { 0, 20, 40, 62, 84 };
        public string[] roomHints = {
            "A/D: moverse · Espacio: saltar. Avanza por las plataformas hacia la derecha.",
            "J / clic: atacar, también en el aire. K / clic derecho: mantener luz. Los cristales recuperan energía.",
            "Pilpintu ataca en picada. El guerrero bloquea de frente: rodéalo o debilítalo con luz.",
            "Recoge el ánima del altar. Q: puma. Disipa sombras y activa el sello con luz para abrir la puerta.",
            "Llanthu resiste los golpes: ilumínalo y ataca. Tras vencerlo, sella Tenebris con la luz." };
        private NoctalEnemy[] enemies;
        private NoctalPickup[] pickups;
        private NoctalLightTarget[] lightTargets;
        private int checkpointRoom;
        public int Room { get; private set; }
        public bool BossDefeated { get; private set; }
        public bool Won { get; private set; }
        public bool Paused { get; private set; }
        private void Awake()
        {
            Instance = this; Time.timeScale = 1;
            Physics2D.IgnoreLayerCollision(NoctalLayers.Enemy,NoctalLayers.Player,true);
            Physics2D.IgnoreLayerCollision(NoctalLayers.Enemy,NoctalLayers.Enemy,true);
            enemies = FindObjectsOfType<NoctalEnemy>(true); pickups = FindObjectsOfType<NoctalPickup>(true);
            lightTargets = FindObjectsOfType<NoctalLightTarget>(true);
        }
        private void Update()
        {
            if (player == null) return;
            Room = 0; for (int i = 1; i < roomStarts.Length; i++) if (player.transform.position.x >= roomStarts[i]) Room = i;
            checkpointRoom = Mathf.Max(checkpointRoom, Room);
            if (player.transform.position.y < -4 && !player.GetComponent<TlamaniVitals>().IsDead)
            {
                player.GetComponent<TlamaniVitals>().TakeDamage(99);
                if (!player.GetComponent<TlamaniVitals>().IsDead) player.RespawnAt(new Vector2(roomStarts[checkpointRoom]+1.5f,1));
            }
        }
        public void TogglePause()
        {
            if (Won) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1;
            // Sin interpolación durante la pausa, la imagen conserva la posición
            // del último paso de físicas en lugar de retroceder al anterior.
            var body = player.GetComponent<Rigidbody2D>();
            body.interpolation = Paused ? RigidbodyInterpolation2D.None : RigidbodyInterpolation2D.Interpolate;
            if (Paused) player.transform.position = body.position;
            player.ClearInput(); player.GetComponent<TlamaniAbilities>().CancelActions();
        }
        public void DefeatBoss()
        {
            BossDefeated = true;
            player.GetComponent<TlamaniAbilities>().AddEnergy(100);
            player.GetComponent<TlamaniAbilities>().Notify("Llanthu derrotado. Usa la luz sobre Tenebris para purificar el templo.");
        }
        public void Complete()
        {
            Won = true; player.ClearInput(); player.GetComponent<TlamaniAbilities>().CancelActions();
            foreach (var projectile in FindObjectsOfType<NoctalProjectile>()) Destroy(projectile.gameObject);
            player.enabled = false;
            Time.timeScale = 0;
        }
        public void Retry()
        {
            Time.timeScale = 1; Paused = Won = BossDefeated = false;
            player.GetComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;
            foreach (var projectile in FindObjectsOfType<NoctalProjectile>()) Destroy(projectile.gameObject);
            foreach (var enemy in enemies) enemy.ResetActor();
            foreach (var pickup in pickups) pickup.ResetPickup();
            foreach (var receiver in lightTargets) receiver.ResetTarget();
            player.GetComponent<TlamaniVitals>().Restore(); player.GetComponent<TlamaniAbilities>().ResetForRetry();
            player.RespawnAt(new Vector2(roomStarts[checkpointRoom] + 1.5f,1));
        }
        private void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; } }
    }
}
