using UnityEngine;
namespace Noctal
{
    public sealed class NoctalProjectile : MonoBehaviour
    {
        private Vector2 velocity;
        private float life = 4;
        private int damage;
        public static void Launch(Vector2 from, Vector2 direction, float speed, int damage)
        {
            var go = new GameObject("Onda de sombra"); go.transform.position = from;
            var glyph = go.AddComponent<NoctalGlyph>(); glyph.shape = NoctalGlyph.Shape.Crystal; glyph.pixelSize = 0.04f; glyph.Rebuild();
            glyph.Tint(new Color(1,0.25f,0.8f));
            var shot = go.AddComponent<NoctalProjectile>(); shot.velocity = direction.normalized * speed; shot.damage = damage;
        }
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime; life -= dt;
            var hit = Physics2D.Raycast(transform.position, velocity.normalized, velocity.magnitude * dt,
                NoctalLayers.WorldMask | (1 << NoctalLayers.Player));
            if (hit.collider != null)
            {
                hit.collider.GetComponent<TlamaniVitals>()?.TakeDamage(damage);
                Destroy(gameObject); return;
            }
            transform.position += (Vector3)velocity * dt;
            if (life <= 0) Destroy(gameObject);
        }
    }
}
