using System;
using System.IO;
using System.Linq;
using Noctal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ConfigureNoctal
{
    private const string Folder = "Assets/Mechanics";
    private const string ScenePath = "Assets/Scenes/Mecanicas_Noctal.unity";
    private static Mesh quad;
    private static Material unlit;
    [MenuItem("Noctal/Abrir mecanicas del GDD")]
    public static void OpenLevel() { EditorSceneManager.OpenScene(ScenePath); Selection.activeGameObject = GameObject.Find("Tlamani"); }
    [MenuItem("Noctal/Construir recorrido de mecanicas GDD")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        ConfigureLayers(); SliceAttack();
        var source = EditorSceneManager.OpenScene("Assets/Scenes/Entrada_Pasillo.unity");
        var sourceMap = GameObject.Find("Tilemap_ParedesYSuelo").GetComponent<Tilemap>();
        TileBase floorTile = sourceMap.GetTile(new Vector3Int(-15,-3,0));
        if (floorTile == null) floorTile = sourceMap.GetTilesBlock(sourceMap.cellBounds).First(t => t != null);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Physics/TlamaniSprite.mat");
        quad = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Quad.asset");
        if (quad == null)
        {
            quad = new Mesh { name = "Quad Noctal" };
            quad.vertices = new[] { new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f) };
            quad.uv = new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right }; quad.triangles = new[] {0,1,2,0,2,3};
            quad.RecalculateBounds(); AssetDatabase.CreateAsset(quad,Folder + "/Quad.asset");
        }
        Background();
        var grid = new GameObject("Templo - plataformas", typeof(Grid));
        var ground = new GameObject("Suelo y plataformas",typeof(Tilemap),typeof(TilemapRenderer)); ground.transform.SetParent(grid.transform);
        ground.layer = NoctalLayers.World;
        var tiles = ground.GetComponent<Tilemap>(); ground.GetComponent<TilemapRenderer>().sharedMaterial = unlit;
        for (int x=-3;x<113;x++) for (int y=-3;y<0;y++)
            if (x < 12 || x >= 14) tiles.SetTile(new Vector3Int(x,y,0),floorTile);
        for (int x=5;x<7;x++) tiles.SetTile(new Vector3Int(x,0,0),floorTile);
        for (int x=8;x<10;x++) tiles.SetTile(new Vector3Int(x,1,0),floorTile);
        for (int x=44;x<47;x++) tiles.SetTile(new Vector3Int(x,0,0),floorTile);
        var staticBody = ground.AddComponent<Rigidbody2D>(); staticBody.bodyType = RigidbodyType2D.Static;
        var composite = ground.AddComponent<CompositeCollider2D>(); composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Physics/Tlamani.physicsMaterial2D");
        var collider = ground.AddComponent<TilemapCollider2D>(); collider.usedByComposite = true;
        foreach (var cell in tiles.cellBounds.allPositionsWithin) if (tiles.HasTile(cell)) tiles.SetColliderType(cell,Tile.ColliderType.Grid);
        collider.ProcessTilemapChanges(); composite.GenerateGeometry();
        Block("Límite izquierdo",new Vector2(-3,3),new Vector2(1,8),new Color(.12f,.14f,.21f),true);
        Block("Límite derecho",new Vector2(113,3),new Vector2(1,8),new Color(.12f,.14f,.21f),true);

        var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tlamani.prefab"));
        actor.name = "Tlamani"; actor.transform.position = new Vector3(1.5f,1); actor.layer = NoctalLayers.Player;
        var motor = actor.GetComponent<TlamaniMotor>(); var health = actor.AddComponent<TlamaniVitals>(); var abilities = actor.AddComponent<TlamaniAbilities>();
        var motorSettings = new SerializedObject(motor); motorSettings.FindProperty("solidLayers").intValue = NoctalLayers.WorldMask; motorSettings.ApplyModifiedPropertiesWithoutUndo();
        var puma = Glyph("Ánima del puma",NoctalGlyph.Shape.Puma,new Vector2(-.1f,-.62f),.065f,actor.transform);
        puma.SetActive(false);
        Reference(abilities,"pumaVisual",puma);
        Reference(abilities,"beam",Beam("Rayo de la orden Inti",actor.transform,new Color(1,.90f,.42f),.10f));
        Reference(abilities,"lightEffectMaterial",LightMaterial());
        var attack = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Tlamani-attack.png").OfType<Sprite>()
            .OrderBy(s=>int.Parse(s.name.Substring(s.name.LastIndexOf('_')+1))).ToArray();
        var visual = new SerializedObject(actor.GetComponent<TlamaniVisual>()); var frames = visual.FindProperty("attack"); frames.arraySize = attack.Length;
        for (int i=0;i<attack.Length;i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = attack[i]; visual.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(actor,"Assets/Prefabs/Tlamani_Mecanicas.prefab");

        var cameraObject = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(TlamaniCamera));
        cameraObject.tag = "MainCamera"; var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.07f);
        camera.transform.position = new Vector3(1.5f,2.5f,-10); Reference(cameraObject.GetComponent<TlamaniCamera>(),"target",actor.transform);

        var level = new GameObject("Mecánicas del templo",typeof(NoctalLevel)).GetComponent<NoctalLevel>(); level.player = motor;
        SpawnEnemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(25,.38f),health);
        SpawnEnemy(NoctalEnemy.EnemyKind.Nawi,new Vector2(34,1.5f),health);
        SpawnEnemy(NoctalEnemy.EnemyKind.Pilpintu,new Vector2(45,2.9f),health);
        SpawnEnemy(NoctalEnemy.EnemyKind.Guerrero,new Vector2(52,.78f),health);
        SpawnEnemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(58,.38f),health);
        level.boss = SpawnEnemy(NoctalEnemy.EnemyKind.Llanthu,new Vector2(96,1.16f),health);
        level.boss.alertRange = 10;
        Pickup("Cristal de la galería",new Vector2(29,.4f),NoctalPickup.PickupKind.Energy,35);
        Pickup("Ofrenda de vida",new Vector2(39,.4f),NoctalPickup.PickupKind.Health,2);
        Pickup("Ánima del santuario",new Vector2(65,.4f),NoctalPickup.PickupKind.Anima,0);
        Pickup("Cristal del santuario",new Vector2(68,.4f),NoctalPickup.PickupKind.Energy,45);
        Pickup("Fuente de luz oeste",new Vector2(88,.4f),NoctalPickup.PickupKind.Energy,40,8);
        Pickup("Fuente de luz este",new Vector2(104,.4f),NoctalPickup.PickupKind.Energy,40,8);
        Pickup("Ofrenda del guardián",new Vector2(83,.4f),NoctalPickup.PickupKind.Health,3);
        var shadow = Block("Sombra disipable",new Vector2(70,1.4f),new Vector2(.65f,2.8f),new Color(.24f,.08f,.35f),true);
        shadow.AddComponent<NoctalLightTarget>().kind = NoctalLightTarget.TargetKind.Shadow;
        Block("Paso del puma",new Vector2(74,2.1f),new Vector2(4,2.2f),new Color(.21f,.24f,.3f),true);
        var gate = Block("Puerta solar",new Vector2(81,2),new Vector2(.55f,4),new Color(.64f,.45f,.21f),true);
        var seal = Glyph("Mecanismo solar",NoctalGlyph.Shape.Sun,new Vector2(79,.25f),.07f); seal.layer = NoctalLayers.Interactable;
        var sealCollider = seal.AddComponent<BoxCollider2D>(); sealCollider.isTrigger = true; sealCollider.size = new Vector2(.8f,1); sealCollider.offset = Vector2.up*.4f;
        var mechanism = seal.AddComponent<NoctalLightTarget>(); mechanism.kind = NoctalLightTarget.TargetKind.Mechanism; mechanism.gate = gate;
        var core = Glyph("Tenebris",NoctalGlyph.Shape.Eye,new Vector2(109,.2f),.15f); core.layer = NoctalLayers.Interactable;
        var coreCollider = core.AddComponent<BoxCollider2D>(); coreCollider.isTrigger = true; coreCollider.size = new Vector2(2,2); coreCollider.offset = Vector2.up;
        var tenebris = core.AddComponent<NoctalLightTarget>(); tenebris.kind = NoctalLightTarget.TargetKind.Tenebris; tenebris.exposureRequired = 1;
        CreateHud(health,level,camera);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true), new EditorBuildSettingsScene("Assets/Scenes/Entrada_Pasillo.unity",true), new EditorBuildSettingsScene("Assets/Scenes/Intro.unity",true) };
        EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
        Debug.Log("NOCTAL_GDD_CONFIGURED: siete mecanicas, cinco salas conectadas, cuatro enemigos y Llanthu.");
    }
    private static Material LightMaterial()
    {
        const string path = Folder + "/SolarLight.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Noctal/Solar Light"));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
    [MenuItem("Noctal/Actualizar luz de la espada")]
    public static void UpgradeLight()
    {
        var material = LightMaterial();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (var abilities in Object.FindObjectsOfType<TlamaniAbilities>()) Reference(abilities,"lightEffectMaterial",material);
        EditorSceneManager.SaveScene(scene);
        const string prefabPath = "Assets/Prefabs/Tlamani_Mecanicas.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Reference(prefab.GetComponent<TlamaniAbilities>(),"lightEffectMaterial",material);
            PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        Debug.Log("NOCTAL_SWORD_LIGHT_UPDATED");
    }
    private static void ConfigureLayers()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        layers.GetArrayElementAtIndex(8).stringValue = "NoctalWorld"; layers.GetArrayElementAtIndex(9).stringValue = "NoctalEnemy";
        layers.GetArrayElementAtIndex(10).stringValue = "NoctalPlayer"; layers.GetArrayElementAtIndex(11).stringValue = "NoctalInteractable";
        tags.ApplyModifiedPropertiesWithoutUndo();
        Physics2D.IgnoreLayerCollision(9,10,true); Physics2D.IgnoreLayerCollision(9,9,true);
        // Persistir la matriz de colisiones del proyecto.
        var physics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]; EditorUtility.SetDirty(physics);
    }
    private static void SliceAttack()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Tlamani-attack.png");
        importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 24;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        var factories = new SpriteDataProviderFactories(); factories.Init(); var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider(); var old = provider.GetSpriteRects();
        var rects = Enumerable.Range(0,8).Select(i=>new SpriteRect { name="Tlamani-attack_"+i,rect=new Rect(i*64,0,64,64),
            alignment=SpriteAlignment.Custom,pivot=new Vector2(.5f,.32625f),spriteID=old.FirstOrDefault(s=>s.name=="Tlamani-attack_"+i)?.spriteID??GUID.Generate() }).ToArray();
        provider.SetSpriteRects(rects); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }
    private static void Reference(Object owner,string name,Object value)
    { var so = new SerializedObject(owner); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static GameObject Glyph(string name,NoctalGlyph.Shape shape,Vector2 position,float size,Transform parent=null)
    {
        var go = new GameObject(name); if (parent!=null) go.transform.SetParent(parent,false); go.transform.localPosition = position;
        var glyph = go.AddComponent<NoctalGlyph>(); glyph.shape=shape; glyph.pixelSize=size; glyph.Rebuild(); return go;
    }
    private static Material ColorMaterial(Color color)
    {
        string path=Folder+"/Color_"+ColorUtility.ToHtmlStringRGBA(color)+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(Shader.Find("Sprites/Default")); material.color=color; AssetDatabase.CreateAsset(material,path); } return material;
    }
    private static GameObject Block(string name,Vector2 center,Vector2 size,Color color,bool solid,int order=-5)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.position=center; go.transform.localScale=size;
        go.GetComponent<MeshFilter>().sharedMesh=quad; var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=ColorMaterial(color); renderer.sortingOrder=order;
        if(solid) { go.layer=NoctalLayers.World; go.AddComponent<BoxCollider2D>(); } return go;
    }
    private static void Background()
    {
        Block("Fondo del templo",new Vector2(52,4),new Vector2(150,18),new Color(.045f,.06f,.10f),false,-40);
        for(int x=-10;x<=120;x+=4)
        {
            Block("Pilar",new Vector2(x,3),new Vector2(.6f,7),new Color(.08f,.11f,.16f),false,-30);
            Block("Capitel",new Vector2(x,5),new Vector2(1.2f,.35f),new Color(.24f,.20f,.15f),false,-29);
            Block("Base del pilar",new Vector2(x,.1f),new Vector2(1.2f,.3f),new Color(.24f,.20f,.15f),false,-29);
        }
        for(int x=18;x<105;x+=22)
        {
            Block("Arco del templo",new Vector2(x,5.8f),new Vector2(4,.6f),new Color(.20f,.18f,.16f),false,-20);
            Glyph("Sello de Inti",NoctalGlyph.Shape.Sun,new Vector2(x,4),.09f);
        }
    }
    private static LineRenderer Beam(string name,Transform parent,Color color,float width)
    {
        var go=new GameObject(name,typeof(LineRenderer)); go.transform.SetParent(parent,false); var line=go.GetComponent<LineRenderer>();
        line.sharedMaterial=unlit; line.positionCount=2; line.useWorldSpace=true; line.startWidth=line.endWidth=width;
        line.startColor=line.endColor=color; line.sortingOrder=20; line.enabled=false; return line;
    }
    private static NoctalEnemy SpawnEnemy(NoctalEnemy.EnemyKind kind,Vector2 position,TlamaniVitals player)
    {
        var go=new GameObject(kind.ToString(),typeof(Rigidbody2D),typeof(CapsuleCollider2D)); go.transform.position=position; go.layer=NoctalLayers.Enemy;
        var capsule=go.GetComponent<CapsuleCollider2D>(); capsule.size=kind==NoctalEnemy.EnemyKind.Llanthu?new Vector2(1.2f,2.3f):kind==NoctalEnemy.EnemyKind.Guerrero?new Vector2(.75f,1.5f):new Vector2(1,.65f);
        capsule.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Physics/Tlamani.physicsMaterial2D");
        var art=kind==NoctalEnemy.EnemyKind.Apasanka?NoctalGlyph.Shape.Spider:kind==NoctalEnemy.EnemyKind.Pilpintu?NoctalGlyph.Shape.Moth:
            kind==NoctalEnemy.EnemyKind.Guerrero?NoctalGlyph.Shape.Warrior:kind==NoctalEnemy.EnemyKind.Nawi?NoctalGlyph.Shape.Eye:NoctalGlyph.Shape.Boss;
        Glyph("Aspecto",art,new Vector2(0,-capsule.size.y/2),kind==NoctalEnemy.EnemyKind.Llanthu?.095f:.075f,go.transform);
        var labelGo=new GameObject("Nombre y defensa",typeof(TextMesh)); labelGo.transform.SetParent(go.transform,false); labelGo.transform.localPosition=new Vector3(0,capsule.size.y/2+.5f);
        var label=labelGo.GetComponent<TextMesh>(); label.anchor=TextAnchor.MiddleCenter; label.fontSize=40; label.characterSize=.065f; label.color=new Color(.9f,.85f,.65f); labelGo.GetComponent<MeshRenderer>().sortingOrder=25;
        var enemy=go.AddComponent<NoctalEnemy>(); enemy.kind=kind; enemy.target=player; enemy.maximumHealth=kind==NoctalEnemy.EnemyKind.Llanthu?18:kind==NoctalEnemy.EnemyKind.Guerrero?5:3;
        enemy.aim=Beam("Telegráfo de ataque",go.transform,Color.red,.03f); enemy.label=label;
        return enemy;
    }
    private static void Pickup(string name,Vector2 position,NoctalPickup.PickupKind kind,float amount,float recharge=0)
    {
        var go=Glyph(name,kind==NoctalPickup.PickupKind.Anima?NoctalGlyph.Shape.Puma:kind==NoctalPickup.PickupKind.Health?NoctalGlyph.Shape.Sun:NoctalGlyph.Shape.Crystal,position,.065f);
        go.layer=NoctalLayers.Interactable; var col=go.AddComponent<BoxCollider2D>(); col.isTrigger=true; col.size=new Vector2(1.3f,1); col.offset=new Vector2(0,.3f);
        var pickup=go.AddComponent<NoctalPickup>(); pickup.kind=kind; pickup.amount=amount; pickup.rechargeSeconds=recharge;
    }
    private static void CreateHud(TlamaniVitals player,NoctalLevel level,Camera camera)
    {
        var go=new GameObject("HUD de mecánicas",typeof(Canvas),typeof(CanvasScaler)); var canvas=go.GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; canvas.sortingOrder=100;
        var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
        Text Label(string name,Vector2 anchor,Vector2 position,Vector2 size,int font,TextAnchor alignment)
        {
            var child=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow)); child.transform.SetParent(go.transform,false);
            var rect=child.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=anchor; rect.anchoredPosition=position; rect.sizeDelta=size;
            var text=child.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=font; text.alignment=alignment; text.color=new Color(.98f,.94f,.80f); text.raycastTarget=false;
            child.GetComponent<Shadow>().effectColor=Color.black; child.GetComponent<Shadow>().effectDistance=new Vector2(2,-2); return text;
        }
        var hud=go.AddComponent<NoctalHud>(); hud.player=player; hud.level=level;
        hud.health=Label("Vida",new Vector2(0,1),new Vector2(25,-20),new Vector2(240,45),24,TextAnchor.UpperLeft);
        hud.energy=Label("Energía y ánima",Vector2.one,new Vector2(-25,-20),new Vector2(290,70),20,TextAnchor.UpperRight);
        hud.room=Label("Sala",new Vector2(.5f,1),new Vector2(0,-20),new Vector2(600,40),24,TextAnchor.UpperCenter);
        hud.hint=Label("Guía de la sala",new Vector2(.5f,0),new Vector2(0,55),new Vector2(1190,55),19,TextAnchor.LowerCenter);
        hud.boss=Label("Vida de Llanthu",new Vector2(.5f,1),new Vector2(0,-66),new Vector2(630,70),20,TextAnchor.UpperCenter);
        hud.overlay=Label("Estado de partida",new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,200),34,TextAnchor.MiddleCenter);
        Label("Controles",new Vector2(.5f,0),new Vector2(0,16),new Vector2(1230,30),17,TextAnchor.LowerCenter).text="A/D: mover   ESPACIO: salto   J / clic: ataque   K / clic derecho: luz   Q: puma   R: reintentar   ESC: pausa";
    }
}
