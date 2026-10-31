using System.Collections;
using System.IO;
using Noctal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class TlamaniSceneTests
{
#if UNITY_EDITOR
    [Test]
    public void PiesDeAnimacionesQuedanSobreLaBaseDelCollider()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tlamani.prefab");
        var capsule = prefab.GetComponent<CapsuleCollider2D>();
        float bottom = capsule.offset.y - capsule.size.y / 2f;
        foreach (string sheet in new[] { "Tlamani-Idle-Sheet", "Tlamani-walk", "Tlamani-run", "Tlamani-jump-Sheet" })
        {
            string path = "Assets/Sprites/" + sheet + ".png";
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is Sprite sprite)) continue;
                    int lowest = (int)sprite.rect.height;
                    for (int y = 0; y < sprite.rect.height && lowest == sprite.rect.height; y++)
                        for (int x = 0; x < sprite.rect.width; x++)
                            if (texture.GetPixel((int)sprite.rect.x + x, (int)sprite.rect.y + y).a > 0.5f)
                            { lowest = y; break; }
                    float feet = (lowest - sprite.pivot.y) / sprite.pixelsPerUnit;
                    // Permite levantar el pie hasta 4 px en la animacion, nunca enterrarlo.
                    Assert.That(feet, Is.InRange(bottom - 0.005f, bottom + 4.1f / sprite.pixelsPerUnit), sprite.name);
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }
    }
#endif
    private IEnumerator Steps(int count) { for(int i=0;i<count;i++) yield return new WaitForFixedUpdate(); }

    [UnityTest]
    public IEnumerator EntradaPasilloPermiteCaminarSaltarYDash()
    {
        yield return SceneManager.LoadSceneAsync("Entrada_Pasillo");
        yield return null;
        var motor = Object.FindObjectOfType<TlamaniMotor>();
        Assert.That(motor, Is.Not.Null);
        motor.GetComponent<TlamaniKeyboard>().enabled = false;
        Assert.That(motor.CompareTag("Player"), Is.True);
        Assert.That(GameObject.Find("Tilemap_ParedesYSuelo").GetComponent<CompositeCollider2D>(), Is.Not.Null);
        Assert.That(GameObject.Find("Tilemap_fondo").GetComponent<Collider2D>(), Is.Null);
        yield return Steps(50); yield return null;
        Assert.That(motor.IsGrounded, Is.True, "El punto inicial debe quedar sobre suelo sólido.");
        Capture("01-inicio");
        float startX = motor.transform.position.x;
        motor.SetInput(1,false,false); yield return Steps(24); yield return null;
        Assert.That(motor.transform.position.x, Is.GreaterThan(startX + 2f));
        float floorY = motor.transform.position.y;
        Capture("02-caminar");
        motor.SetInput(0,true,false); yield return Steps(12); yield return null;
        Assert.That(motor.transform.position.y, Is.GreaterThan(floorY + 1.5f));
        Capture("03-salto");
        motor.SetInput(1,false,true); yield return Steps(3); yield return null;
        Assert.That(motor.IsDashing, Is.True);
        Capture("04-dash");
        motor.SetInput(0,false,false);
        for (int i = 0; i < 20 && motor.IsDashing; i++) yield return Steps(1);
        yield return null;
        Assert.That(motor.IsDashing, Is.False);
        Assert.That(motor.Velocity.x, Is.EqualTo(0f).Within(0.01f));
        Capture("06-fin-dash");
        yield return Steps(50); yield return null;
        Assert.That(motor.IsGrounded, Is.True);
        Capture("05-aterrizaje");
        // Zona_Muerte real: el trigger debe devolverlo al punto asignado.
        motor.RespawnAt(new Vector2(26.42f,-4.42f)); yield return Steps(3);
        Assert.That(motor.transform.position.x, Is.EqualTo(startX).Within(0.1f));
        Assert.That(motor.Velocity.y, Is.GreaterThan(-3f));
    }

    [UnityTest]
    public IEnumerator IntroTienePersonajeYSueloConfigurados()
    {
        yield return SceneManager.LoadSceneAsync("Intro"); yield return null;
        var motor = Object.FindObjectOfType<TlamaniMotor>(); Assert.That(motor, Is.Not.Null);
        motor.GetComponent<TlamaniKeyboard>().enabled = false;
        Assert.That(GameObject.Find("Ground").GetComponent<TilemapCollider2D>().usedByComposite, Is.True);
        yield return Steps(50); Assert.That(motor.IsGrounded, Is.True);
    }

    // Evidencia renderizada por Unity durante PlayMode; no se generan imágenes con -nographics.
    private static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main;
        var previous = camera.targetTexture; var active = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280,720,24);
        var texture = new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            string folder = Path.Combine(Application.dataPath,"../docs/capturas"); Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous; RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target); Object.Destroy(texture);
        }
    }
}
