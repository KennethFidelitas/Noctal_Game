using System;
using System.IO;
using System.Linq;
using Noctal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Configuración reproducible de las escenas; no se ejecuta al abrir el proyecto.</summary>
public static class ConfigureTlamani
{
    [MenuItem("Noctal/Configurar Tlamani en las escenas")]
    public static void Run()
    {
        Slice("Tlamani-walk"); Slice("Tlamani-run"); Slice("Tlamani-jump-Sheet");
        Directory.CreateDirectory("Assets/Physics");
        Directory.CreateDirectory("Assets/Prefabs");
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Physics/Tlamani.physicsMaterial2D");
        if (material == null)
        {
            material = new PhysicsMaterial2D("Tlamani") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, "Assets/Physics/Tlamani.physicsMaterial2D");
        }
        ConfigureScene("Assets/Scenes/Intro.unity", "Square", material);
        ConfigureScene("Assets/Scenes/Entrada_Pasillo.unity", "Player_test", material);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/Entrada_Pasillo.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Intro.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("TLAMANI_CONFIGURED: escenas, prefab, físicas, sprites y controles guardados.");
    }
    private static void Slice(string name)
    {
        string path = "Assets/Sprites/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int count = texture.width / 64;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 24;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects();
        var rects = Enumerable.Range(0, count).Select(i => new SpriteRect {
            name = name + "_" + i, rect = new Rect(i * 64, 0, 64, 64), alignment = SpriteAlignment.Custom,
            pivot = new Vector2(0.6563609f, 0.4776628f),
            spriteID = existing.FirstOrDefault(r => r.name == name + "_" + i)?.spriteID ?? GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }
    private static Sprite[] Sprites(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/" + name + ".png")
        .OfType<Sprite>().OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    private static T Add<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }
    private static void Reference(Object owner, string property, Object value)
    {
        var so = new SerializedObject(owner);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Frames(TlamaniVisual visual, string field, Sprite[] sprites)
    {
        var so = new SerializedObject(visual); var array = so.FindProperty(field); array.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ConfigureScene(string path, string oldName, PhysicsMaterial2D material)
    {
        var scene = EditorSceneManager.OpenScene(path);
        var player = GameObject.Find("Tlamani") ?? GameObject.Find(oldName);
        if (player == null) throw new InvalidOperationException("Falta personaje en " + path);
        player.name = "Tlamani"; player.tag = "Player";
        foreach (var c in player.GetComponents<Collider2D>()) if (!(c is CapsuleCollider2D)) Object.DestroyImmediate(c);
        var oldAnimator = player.GetComponent<Animator>(); if (oldAnimator != null) Object.DestroyImmediate(oldAnimator);
        var capsule = Add<CapsuleCollider2D>(player);
        capsule.size = new Vector2(0.68f, 1.10f); capsule.offset = new Vector2(-0.10f, -0.07f);
        capsule.sharedMaterial = material; capsule.isTrigger = false;
        var body = Add<Rigidbody2D>(player);
        body.bodyType = RigidbodyType2D.Dynamic; body.gravityScale = 3f; body.mass = 1f; body.drag = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var renderer = Add<SpriteRenderer>(player);
        renderer.sprite = Sprites("Tlamani-Idle-Sheet")[0]; renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Simple; renderer.sortingOrder = 10;
        renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        // Reutilizar un material serializado, sin instancias temporales en la escena.
        var spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Physics/TlamaniSprite.mat");
        if (spriteMaterial == null) { spriteMaterial = renderer.sharedMaterial; AssetDatabase.CreateAsset(spriteMaterial, "Assets/Physics/TlamaniSprite.mat"); }
        renderer.sharedMaterial = spriteMaterial;
        var motor = Add<TlamaniMotor>(player); Add<TlamaniKeyboard>(player);
        var visual = Add<TlamaniVisual>(player);
        Frames(visual, "idle", Sprites("Tlamani-Idle-Sheet")); Frames(visual, "walk", Sprites("Tlamani-walk"));
        Frames(visual, "jump", Sprites("Tlamani-jump-Sheet")); Frames(visual, "dash", Sprites("Tlamani-run"));
        foreach (var tilemap in Object.FindObjectsOfType<Tilemap>())
        {
            if (tilemap.name != "Ground" && tilemap.name != "Tilemap_ParedesYSuelo") continue;
            Add<Rigidbody2D>(tilemap.gameObject).bodyType = RigidbodyType2D.Static;
            var composite = Add<CompositeCollider2D>(tilemap.gameObject);
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.sharedMaterial = material;
            var collider = Add<TilemapCollider2D>(tilemap.gameObject); collider.usedByComposite = true;
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(cell)) tilemap.SetColliderType(cell, Tile.ColliderType.Grid);
            collider.ProcessTilemapChanges(); composite.GenerateGeometry();
        }
        var spawn = GameObject.Find("Punto_Inicio") ?? new GameObject("Punto_Inicio");
        spawn.transform.position = player.transform.position;
        Reference(motor, "spawnPoint", spawn.transform);
        foreach (var zone in Object.FindObjectsOfType<Zona_Muerte>()) Reference(zone, "puntoInicio", spawn.transform);
        var camera = Camera.main;
        camera.orthographic = true; camera.orthographicSize = 4.5f;
        camera.transform.position = player.transform.position + new Vector3(0f, 1.5f, -10f);
        Reference(Add<TlamaniCamera>(camera.gameObject), "target", player.transform);
        CreateHud(motor);
        PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Tlamani.prefab");
        // Los prefabs no pueden guardar referencias a objetos de la escena; la instancia sí.
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    private static void CreateHud(TlamaniMotor player)
    {
        if (GameObject.Find("Controles Tlamani") != null) return;
        var go = new GameObject("Controles Tlamani", typeof(Canvas), typeof(CanvasScaler));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        Text MakeText(string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text)); child.transform.SetParent(go.transform, false);
            var rect = child.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
            rect.anchoredPosition = pos; rect.sizeDelta = size;
            var label = child.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize; label.text = text; label.alignment = alignment;
            label.color = Color.white; label.raycastTarget = false;
            var shadow = child.AddComponent<Shadow>(); shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1,-1);
            return label;
        }
        MakeText("Titulo", "NOCTAL  /  TLAMANI", new Vector2(0,1), new Vector2(24,-20), new Vector2(600,45), 24, TextAnchor.UpperLeft);
        MakeText("Teclas", "A / D o flechas: caminar     ESPACIO: saltar     SHIFT: dash     R: reiniciar", Vector2.zero, new Vector2(24,20), new Vector2(1200,40), 20, TextAnchor.LowerLeft);
        var status = MakeText("Estado dash", "DASH LISTO", Vector2.one, new Vector2(-24,-20), new Vector2(400,40), 20, TextAnchor.UpperRight);
        var hud = go.AddComponent<TlamaniHud>(); Reference(hud,"player",player); Reference(hud,"status",status);
    }
}
