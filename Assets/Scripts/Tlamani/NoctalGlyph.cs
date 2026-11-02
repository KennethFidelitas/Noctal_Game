using System.Collections.Generic;
using UnityEngine;

namespace Noctal
{
    /// <summary>Arte provisional construido como mallas; reemplazable por sprites del equipo.</summary>
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class NoctalGlyph : MonoBehaviour
    {
        public enum Shape { Puma, Spider, Moth, Warrior, Eye, Boss, Crystal, Sun }
        public Shape shape;
        public float pixelSize = 0.07f;
        private Mesh ownedMesh;
        private Material ownedMaterial;
        private MaterialPropertyBlock properties;
        private void OnEnable() => Rebuild();
        public void Rebuild()
        {
            Release();
            string[] rows;
            switch (shape)
            {
                case Shape.Puma: rows = new[] {
                    ".....................c...c..", "....................ccc.ccc.",
                    "...................ccccccwc", "........ccccccccccccccccccc",
                    "......ccccccccccccccccccccc", "cc...ccccvvvvvvvvcccccccc..",
                    ".cc.ccccvvvvvvvvvvcccccc...", "..ccccccccccccccccccccc...",
                    "...cccccccccccccccccccc...", ".....ccc.ccc......ccc.ccc..",
                    "....ccc...ccc.....cc...cc..", "...cccc...cccc...ccc..ccc.." }; break;
                case Shape.Spider: rows = new[] {
                    "....v............v....", "...v..pppppppp....v...", "..v..pppppppppp....v..",
                    ".vv.ppwwppppwwpp...vv.", "vv..pppppppppppp....vv", ".vvvvppppppppppvvvvv..",
                    "..vvvppppppppppvvv...", ".vv..vppppppppv..vv..", "vv...vv......vv...vv.",
                    "v.....v......v.....v." }; break;
                case Shape.Moth: rows = new[] {
                    "vv......................vv", "vpvv.................vvpvv", "vpppvv.....ww.....vvppppvv",
                    "vpppppvv..pwwp..vvppppppvv", ".vppppppvppppppvpppppppv..", "..vpppppppppppppppppppv...",
                    "...vvppppppppppppppvv.....", ".....vvppppppppppvv.......", "...vvppvppppppvppvv.......",
                    "..vppvv.vppppv.vvppv......", ".vvv.....vppv.....vvv.....", "..........vv.............." }; break;
                case Shape.Warrior: rows = new[] {
                    ".........gg.........", "........gppg........", ".......gppppg.......", ".......gwwwwg.......",
                    ".......pppppp.......", "......ggppppgg......", "....gggppppppggg....", "...gppgppppppgppg...",
                    "...gppgppppppgppg.g.", "..gggggppggppgppg.g.", ".gppppggppppggppg.g.", ".gpggpgppppppgppg.g.",
                    ".gpggpgppppppgppgggg", ".gppppgppppppg....g.", "..gggg.pppppp.....g.", ".......pp..pp.....g.",
                    "......ppp..ppp......", "......ppp..ppp......", ".....gggg..gggg....." }; break;
                case Shape.Eye: rows = new[] {
                    ".......gggggg.......", "....gggppppppggg....", "..ggppppppppppppgg..", ".gpppwwwwwwwwwwpppg.",
                    "gppwwwwppppppwwwwppg", "gppwwwpppccpppwwwppg", "gppwwwpppccpppwwwppg", ".gppwwwppppppwwwppg.",
                    "..ggppwwwwwwwwppgg..", "....gggppppppggg....", ".......gggggg......." }; break;
                case Shape.Boss: rows = new[] {
                    "v.........................v", "vv...........gg..........vv", "vpv.........gppg........vpv",
                    "vppv.......gppppg......vppv", "vpppv.....gppppppg....vpppv", "vppppv....gpwwwwpg...vppppv",
                    "vpppppv...gppppppg..vpppppv", ".vpppppv.gggppppgggvpppppv.", "..vppppgggppppppppgggpppv..",
                    "...vppgppgppppppppgppgppv..", "....vgpggpgppppppgpggpgv...", ".....gpggpgppggppgpggpg....",
                    ".....gppppgppggppgppppg....", ".....ggggggppppppgggggg....", "....vvppppppppppppppppvv...",
                    "...vvvppppppppppppppppvvv..", "..vvvppppppppppppppppppvvv.", "..vvppppppppppppppppppppvv.",
                    "...vppppppppppppppppppppv..", "....vppppppppppppppppppv...", ".....vppppppppppppppppv....",
                    "......vvpppp....ppppvv.....", ".......gpppg....gpppg......", ".......gpppg....gpppg......",
                    "......gggggg....gggggg....." }; break;
                case Shape.Crystal: rows = new[] { "....c....", "...cwc...", "..cwwwc..", ".cwwwwwc.", "ccwwwwwcc", ".ccccccc.", "..ccccc..", "...ccc...", "....c...." }; break;
                default: rows = new[] { ".....g.....", "..g..g..g..", "...ggggg...", "..ggwwwgg..", "..gwwwwwg..", "gggwwwwwggg", "..gwwwwwg..", "..ggwwwgg..", "...ggggg...", "..g..g..g..", ".....g....." }; break;
            }
            int width = 0; foreach (string row in rows) width = Mathf.Max(width, row.Length);
            var verts = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
            for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++)
            {
                char ch = rows[y][x]; if (ch == '.') continue;
                Color color = ch == 'c' ? new Color(.32f,.95f,.9f) : ch == 'w' ? new Color(1,.91f,.60f) :
                    ch == 'g' ? new Color(.70f,.48f,.23f) : ch == 'v' ? new Color(.36f,.20f,.50f) : new Color(.12f,.08f,.22f);
                int n = verts.Count; float left = (x - width * .5f) * pixelSize, bottom = (rows.Length - y - 1) * pixelSize;
                verts.Add(new Vector3(left,bottom)); verts.Add(new Vector3(left,bottom+pixelSize));
                verts.Add(new Vector3(left+pixelSize,bottom+pixelSize)); verts.Add(new Vector3(left+pixelSize,bottom));
                for (int j=0;j<4;j++) colors.Add(color);
                triangles.AddRange(new[] { n,n+1,n+2,n,n+2,n+3 });
            }
            ownedMesh = new Mesh { name = "Noctal " + shape, hideFlags = HideFlags.DontSave };
            ownedMesh.SetVertices(verts); ownedMesh.SetColors(colors); ownedMesh.SetTriangles(triangles,0); ownedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            ownedMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            var renderer = GetComponent<MeshRenderer>(); renderer.sharedMaterial = ownedMaterial; renderer.sortingOrder = 12;
        }
        public void Tint(Color color)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.SetColor("_Color", color); GetComponent<MeshRenderer>().SetPropertyBlock(properties);
        }
        private void Release()
        {
            if (ownedMesh != null) { if (Application.isPlaying) Destroy(ownedMesh); else DestroyImmediate(ownedMesh); }
            if (ownedMaterial != null) { if (Application.isPlaying) Destroy(ownedMaterial); else DestroyImmediate(ownedMaterial); }
        }
        private void OnDisable() => Release();
    }
}
