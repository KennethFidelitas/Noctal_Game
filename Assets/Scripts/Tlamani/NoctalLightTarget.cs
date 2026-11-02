using UnityEngine;
namespace Noctal
{
    public sealed class NoctalLightTarget : MonoBehaviour
    {
        public enum TargetKind { Shadow, Mechanism, Tenebris }
        public TargetKind kind;
        public GameObject gate;
        public float exposureRequired = 0.55f;
        public bool Activated { get; private set; }
        private float exposure, lastLight = -10;
        public void Illuminate(float dt)
        {
            if (Activated) return;
            if (kind == TargetKind.Tenebris && (NoctalLevel.Instance == null || !NoctalLevel.Instance.BossDefeated)) return;
            if (Time.time - lastLight > 0.1f) exposure = 0;
            lastLight = Time.time; exposure += dt;
            if (exposure < exposureRequired) return;
            Activated = true;
            if (gate != null) gate.SetActive(false);
            if (kind == TargetKind.Tenebris) NoctalLevel.Instance.Complete();
            if (kind == TargetKind.Shadow) gameObject.SetActive(false);
            else { var glyph = GetComponent<NoctalGlyph>(); if (glyph != null) glyph.Tint(Color.cyan); }
        }
        public void ResetTarget()
        {
            Activated = false; exposure = 0; lastLight = -10; gameObject.SetActive(true);
            if (gate != null) gate.SetActive(true);
            GetComponent<NoctalGlyph>()?.Tint(Color.white);
        }
    }
}
