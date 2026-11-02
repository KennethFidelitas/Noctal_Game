using UnityEngine;

namespace Noctal
{
    /// <summary>Efecto visual sin colliders; las interacciones pertenecen a TlamaniAbilities.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class TlamaniLightFx : MonoBehaviour
    {
        private TlamaniAbilities abilities;
        private TlamaniMotor motor;
        private Material ownedMaterial;
        private Mesh ribbonMesh, quad;
        private MeshRenderer ribbon, source, impact;
        private readonly MeshRenderer[] motes = new MeshRenderer[16];
        private MaterialPropertyBlock properties;
        private readonly Vector3[] vertices = new Vector3[4];
        public bool BeamVisible => ribbon != null && ribbon.enabled;
        public Vector3 RenderOrigin { get; private set; }

        public void Initialize(TlamaniAbilities owner, Material material)
        {
            abilities = owner; motor = GetComponent<TlamaniMotor>();
            if (ribbon != null) return;
            if (material == null)
            {
                var shader = Shader.Find("Noctal/Solar Light");
                if (shader == null) return;
                ownedMaterial = new Material(shader); material = ownedMaterial;
            }
            properties = new MaterialPropertyBlock();
            quad = new Mesh { name = "Destello de luz" };
            quad.vertices = new[] { new Vector3(-.5f,-.5f), new Vector3(-.5f,.5f), new Vector3(.5f,.5f), new Vector3(.5f,-.5f) };
            quad.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            quad.triangles = new[] { 0,1,2,0,2,3 }; quad.RecalculateBounds();
            ribbonMesh = new Mesh { name = "Haz de luz de la espada" };
            ribbonMesh.MarkDynamic(); ribbonMesh.vertices = vertices;
            ribbonMesh.uv = quad.uv; ribbonMesh.triangles = quad.triangles;
            ribbon = Part("Luz - núcleo y halo", ribbonMesh, material, 24);
            source = Part("Luz - punta de la espada", quad, material, 26);
            impact = Part("Luz - contacto", quad, material, 26);
            for (int i=0; i<motes.Length; i++) motes[i] = Part("Luz - chispa " + i, quad, material, 25);
            Clear();
        }
        private MeshRenderer Part(string label, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.sortingOrder = order; return renderer;
        }
        private void Appearance(MeshRenderer renderer, float mode, float strength, float length = 1)
        {
            properties.Clear(); properties.SetFloat("_Mode", mode); properties.SetFloat("_Strength", strength);
            properties.SetFloat("_BeamLength", length); properties.SetFloat("_Clock", Time.time);
            renderer.SetPropertyBlock(properties);
        }
        private void Flare(MeshRenderer renderer, Vector3 position, float size, float strength, float mode = 1)
        {
            renderer.enabled = strength > .001f;
            renderer.transform.position = position;
            renderer.transform.localScale = Vector3.one * size;
            Appearance(renderer, mode, strength);
        }
        private void LateUpdate()
        {
            if (ribbon == null || abilities == null) return;
            Vector2 tip = abilities.LightTipOffset;
            RenderOrigin = transform.position + new Vector3(tip.x * motor.Facing, tip.y, 0);
            bool active = abilities.IsLighting;
            float length = active ? Mathf.Max(0, (abilities.BeamEnd.x - RenderOrigin.x) * motor.Facing) : 0;
            bool blockedAtTip = active && length < .015f;
            float charge = Mathf.InverseLerp(.70f, 1, abilities.LightPoseProgress);
            float pulse = 1 + Mathf.Sin(Time.time * 18) * .07f;
            Flare(source, RenderOrigin, (.13f + charge * .40f) * pulse,
                blockedAtTip ? 0 : charge * (active ? 1 : .55f));
            ribbon.enabled = active && !blockedAtTip;
            if (ribbon.enabled)
            {
                // El origen sigue la posición interpolada del personaje, no el
                // paso de físicas anterior. El extremo queda fijo en el contacto.
                float start = motor.Facing * tip.x, end = start + motor.Facing * length;
                float halfWidth = .24f * pulse;
                vertices[0] = new Vector3(start, tip.y - halfWidth);
                vertices[1] = new Vector3(start, tip.y + halfWidth);
                vertices[2] = new Vector3(end, tip.y + halfWidth * .72f);
                vertices[3] = new Vector3(end, tip.y - halfWidth * .72f);
                ribbonMesh.vertices = vertices; ribbonMesh.RecalculateBounds();
                Appearance(ribbon, 0, 1, length);
            }
            Vector3 endPoint = new Vector3(abilities.BeamEnd.x, RenderOrigin.y, transform.position.z);
            Flare(impact, endPoint, .60f * pulse, active && abilities.BeamHit ? .85f : 0);
            for (int i=0; i<motes.Length; i++)
            {
                float seed = i * .618034f;
                float along = Mathf.Repeat(Time.time * (.38f + (i % 3) * .12f) + seed, 1);
                float fade = Mathf.Sin(along * Mathf.PI);
                float drift = Mathf.Sin(Time.time * 5 + i * 2.4f) * .10f;
                Vector3 position = Vector3.Lerp(RenderOrigin, endPoint, along) + Vector3.up * drift;
                if (i >= 12 && abilities.BeamHit)
                {
                    float age = Mathf.Repeat(Time.time * 1.5f + seed, 1);
                    position = endPoint + new Vector3(-motor.Facing * age * .32f, (i % 2 == 0 ? 1 : -1) * age * .30f);
                    fade = 1 - age;
                }
                Flare(motes[i], position, .045f + (i % 3) * .018f, ribbon.enabled ? fade * .75f : 0, 2);
            }
        }
        public void Clear()
        {
            if (ribbon != null) ribbon.enabled = false;
            if (source != null) source.enabled = false;
            if (impact != null) impact.enabled = false;
            foreach (var mote in motes) if (mote != null) mote.enabled = false;
        }
        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (ownedMaterial != null) Destroy(ownedMaterial);
            if (ribbonMesh != null) Destroy(ribbonMesh);
            if (quad != null) Destroy(quad);
        }
    }
}
