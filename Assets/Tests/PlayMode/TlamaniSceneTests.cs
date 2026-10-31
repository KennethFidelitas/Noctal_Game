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
        motor.SetInput(0,false,false); yield return Steps(55); yield return null;
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
