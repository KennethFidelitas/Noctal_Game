using System.Collections.Generic;
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
        private Transform lightPose;
        private SpriteRenderer upperBody, lowerBody;
        private sealed class Slice
        {
            public Sprite upper, lower;
            public Vector2 upperOffset, lowerOffset;
        }
        private readonly Dictionary<Sprite, Slice> slices = new Dictionary<Sprite, Slice>();
        private void Awake() { visual = GetComponent<SpriteRenderer>(); motor = GetComponent<TlamaniMotor>(); abilities = GetComponent<TlamaniAbilities>(); vitals = GetComponent<TlamaniVitals>(); }
        private void LateUpdate()
        {
            bool channelPose = abilities != null && !abilities.IsPuma && abilities.LightPoseProgress > 0
                && attack != null && attack.Length >= 5 && !abilities.IsAttacking;
            visual.enabled = !channelPose && (abilities == null || !abilities.IsPuma);
            Sprite[] frames = abilities != null && abilities.IsAttacking && attack != null && attack.Length > 0 ? attack : motor.IsDashing ? dash : !motor.IsGrounded ? jump : Mathf.Abs(motor.Velocity.x) > 0.15f ? walk : idle;
            if (frames != previous) { elapsed = 0f; previous = frames; }
            elapsed += Time.deltaTime;
            if (frames != null && frames.Length > 0)
                visual.sprite = frames == attack && abilities != null ? frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(abilities.AttackProgress * frames.Length))] : frames[Mathf.FloorToInt(elapsed * (motor.IsDashing ? 24f : 10f)) % frames.Length];
            visual.flipX = motor.Facing < 0;
            visual.color = motor.IsDashing ? new Color(0.5f, 1f, 1f) : Color.white;
            if (vitals != null && vitals.IsInvulnerable && Mathf.FloorToInt(Time.time * 14) % 2 == 0) visual.color = new Color(1,0.3f,0.4f);
            if (channelPose)
            {
                EnsureLightPose(); lightPose.gameObject.SetActive(true);
                lightPose.localScale = new Vector3(motor.Facing, 1, 1);
                float progress = abilities.LightPoseProgress;
                // Preparar -> elevar -> apuntar. Se omite el arco de impacto
                // de la animación de ataque: canalizar luz no ejecuta un golpe.
                int frame = progress < .18f ? 0 : progress < .45f ? 1 : progress < .72f ? 2 : 4;
                SetSlice(upperBody, attack[frame], true);
                // La hoja original dibuja también el arma junto a las piernas.
                // Usar su recorte de piernas evita mostrar una segunda espada.
                Sprite legs = walk != null && walk.Length > 0
                    ? walk[motor.IsGrounded ? (Mathf.Abs(motor.Velocity.x) > .15f ? Mathf.FloorToInt(elapsed * 10) % walk.Length : 0) : Mathf.Min(4,walk.Length-1)]
                    : visual.sprite;
                SetSlice(lowerBody, legs, false);
                upperBody.color = lowerBody.color = visual.color;
            }
            else if (lightPose != null) lightPose.gameObject.SetActive(false);
        }
        private void EnsureLightPose()
        {
            if (lightPose != null) return;
            lightPose = new GameObject("Postura de canalización").transform; lightPose.SetParent(transform, false);
            upperBody = BodyPart("Torso y espada"); lowerBody = BodyPart("Piernas en movimiento");
        }
        private SpriteRenderer BodyPart(string label)
        {
            var go = new GameObject(label); go.transform.SetParent(lightPose, false);
            var part = go.AddComponent<SpriteRenderer>(); part.sharedMaterial = visual.sharedMaterial;
            part.sortingLayerID = visual.sortingLayerID; part.sortingOrder = visual.sortingOrder;
            return part;
        }
        private void SetSlice(SpriteRenderer part, Sprite sprite, bool upper)
        {
            if (sprite == null) return;
            if (!slices.TryGetValue(sprite, out Slice slice))
            {
                // Recortes de sprites originales: no se modifica la textura.
                // La misma cintura en unidades de mundo permite animar las
                // piernas al caminar o saltar sin soltar la espada horizontal.
                Rect rect = sprite.rect; float ppu = sprite.pixelsPerUnit;
                int cut = Mathf.Clamp(Mathf.RoundToInt(sprite.pivot.y + .005f * ppu), 1, (int)rect.height - 1);
                slice = new Slice();
                slice.lower = Sprite.Create(sprite.texture, new Rect(rect.x, rect.y, rect.width, cut), Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
                if (sprite.name.StartsWith("Tlamani-walk")) TrimCarriedWeapon(slice.lower, cut);
                slice.upper = Sprite.Create(sprite.texture, new Rect(rect.x, rect.y + cut, rect.width, rect.height - cut), Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
                slice.lowerOffset = -sprite.pivot / ppu;
                slice.upperOffset = new Vector2(-sprite.pivot.x, cut - sprite.pivot.y) / ppu;
                slices.Add(sprite, slice);
            }
            part.sprite = upper ? slice.upper : slice.lower;
            part.transform.localPosition = upper ? (Vector3)slice.upperOffset : (Vector3)slice.lowerOffset;
        }
        private static void TrimCarriedWeapon(Sprite sprite, int height)
        {
            // Contorno escalonado entre el arma apoyada a la izquierda y las
            // piernas de las celdas originales de 64 px. Solo cambia la malla.
            var points = new List<Vector2>(); var triangles = new List<ushort>();
            for (int y=0; y<height; y++)
            {
                float left = Mathf.Max(22, Mathf.Floor(22 + (y-10) * .7f));
                int n = points.Count;
                // OverrideGeometry recibe píxeles del rect, no unidades de mundo.
                points.Add(new Vector2(left,y)); points.Add(new Vector2(left,y+1));
                points.Add(new Vector2(sprite.rect.width,y+1)); points.Add(new Vector2(sprite.rect.width,y));
                triangles.AddRange(new[] {(ushort)n,(ushort)(n+1),(ushort)(n+2),(ushort)n,(ushort)(n+2),(ushort)(n+3)});
            }
            sprite.OverrideGeometry(points.ToArray(),triangles.ToArray());
        }
        private void OnDisable() { if (lightPose != null) lightPose.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            foreach (var slice in slices.Values) { Destroy(slice.upper); Destroy(slice.lower); }
        }
    }
}
