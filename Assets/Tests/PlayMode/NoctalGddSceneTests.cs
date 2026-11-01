using System.Collections;
using System.IO;
using Noctal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class NoctalGddSceneTests
{
    private NoctalLevel level;
    private TlamaniMotor motor;
    private TlamaniAbilities abilities;
    private IEnumerator Steps(int count) { for(int i=0;i<count;i++) yield return new WaitForFixedUpdate(); }
    [UnitySetUp]
    public IEnumerator Load()
    {
        Time.timeScale=1; yield return SceneManager.LoadSceneAsync("Mecanicas_Noctal"); yield return null;
        level=Object.FindObjectOfType<NoctalLevel>(); motor=level.player; abilities=motor.GetComponent<TlamaniAbilities>();
        motor.GetComponent<TlamaniKeyboard>().enabled=false;
        foreach(var enemy in Object.FindObjectsOfType<NoctalEnemy>()) enemy.behaviorEnabled=false;
        yield return Steps(45); yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup() { Time.timeScale=1; yield return null; }
    [UnityTest]
    public IEnumerator EscenaIntegraAtaqueTerrestreYAereoConHUD()
    {
        Assert.That(motor.IsGrounded,Is.True); Assert.That(Object.FindObjectsOfType<NoctalEnemy>().Length,Is.EqualTo(6));
        Assert.That(Object.FindObjectOfType<NoctalHud>().health.text,Does.Contain("6")); Capture("07-inicio-gdd");
        var spider=GameObject.Find("Apasanka").GetComponent<NoctalEnemy>();
        motor.RespawnAt(new Vector2(spider.transform.position.x-1,.65f)); yield return Steps(15);
        Assert.That(abilities.TryAttack(),Is.True); yield return Steps(8); yield return null; Capture("08-ataque");
        Assert.That(spider.Health,Is.LessThan(spider.maximumHealth));
        yield return Steps(20);
        var moth=GameObject.Find("Pilpintu").GetComponent<NoctalEnemy>();
        motor.RespawnAt(new Vector2(moth.transform.position.x-1,1.65f)); yield return Steps(5);
        motor.SetInput(0,true,false); yield return Steps(9);
        Assert.That(motor.IsGrounded,Is.False);
        abilities.TryAttack(); yield return Steps(9); yield return null; Capture("09-ataque-aereo");
        Assert.That(moth.Health,Is.LessThan(moth.maximumHealth));
    }
    [UnityTest]
    public IEnumerator SantuarioDesbloqueaPumaDisipaSombrasYPuerta()
    {
        motor.RespawnAt(new Vector2(65,.65f)); yield return Steps(8);
        Assert.That(abilities.PumaUnlocked,Is.True); Assert.That(abilities.TryTogglePuma(),Is.True);
        var shadow=GameObject.Find("Sombra disipable").GetComponent<NoctalLightTarget>();
        var mechanism=GameObject.Find("Mecanismo solar").GetComponent<NoctalLightTarget>();
        abilities.SetLightHeld(true); motor.SetInput(1,false,false);
        yield return Steps(55); yield return null; Capture("10-puma-luz");
        yield return Steps(70); motor.SetInput(0,false,false); abilities.SetLightHeld(false);
        Assert.That(shadow.Activated,Is.True); Assert.That(mechanism.Activated,Is.True);
        Assert.That(mechanism.gate.activeSelf,Is.False); Assert.That(motor.transform.position.x,Is.GreaterThan(81));
        level.TogglePause(); Vector3 paused=motor.transform.position; float energy=abilities.Energy;
        yield return null; yield return null; Assert.That(motor.transform.position,Is.EqualTo(paused)); Assert.That(abilities.Energy,Is.EqualTo(energy));
        level.Retry(); yield return Steps(3);
        Assert.That(level.Paused,Is.False); Assert.That(abilities.IsPuma,Is.False); Assert.That(abilities.PumaUnlocked,Is.True);
        Assert.That(mechanism.Activated,Is.False); Assert.That(shadow.gameObject.activeSelf,Is.True);
    }
    [UnityTest]
    public IEnumerator CombateRealContraLlanthuSellaTenebrisYPermiteReintentar()
    {
        var boss=level.boss; boss.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
        var core=GameObject.Find("Tenebris").GetComponent<NoctalLightTarget>();
        core.Illuminate(2); Assert.That(core.Activated,Is.False,"Tenebris no se puede sellar antes de derrotar al jefe.");
        motor.RespawnAt(new Vector2(boss.transform.position.x-1.25f,.65f)); yield return Steps(12);
        abilities.TryAttack(); yield return Steps(24); Assert.That(boss.Health,Is.EqualTo(18));
        for(int i=0;i<18;i++)
        {
            abilities.SetLightHeld(true); yield return Steps(16); abilities.SetLightHeld(false);
            Assert.That(boss.IsExposed,Is.True);
            abilities.TryAttack(); yield return Steps(24);
            if(i==9) { yield return null; Capture("11-llanthu-fase2"); }
        }
        Assert.That(boss.IsDead,Is.True); Assert.That(level.BossDefeated,Is.True);
        motor.RespawnAt(new Vector2(106.5f,.65f)); abilities.SetLightHeld(true);
        yield return new WaitForSecondsRealtime(1.5f); yield return null;
        Assert.That(core.Activated,Is.True); Assert.That(level.Won,Is.True); Capture("12-tenebris-sellado");
        level.Retry(); yield return Steps(3); Assert.That(boss.Health,Is.EqualTo(18)); Assert.That(level.Won,Is.False);
        Assert.That(motor.GetComponent<TlamaniVitals>().Health,Is.EqualTo(6));
        yield return Steps(50);
        motor.RespawnAt(new Vector2(90,-5)); yield return Steps(3); Assert.That(motor.GetComponent<TlamaniVitals>().IsDead,Is.True);
    }
    public static void Capture(string name)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera=Camera.main; var previous=camera.targetTexture; var active=RenderTexture.active;
        var target=RenderTexture.GetTemporary(1280,720,24); var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            Canvas.ForceUpdateCanvases(); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            string folder=Path.Combine(Application.dataPath,"../docs/capturas"); Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());
        }
        finally { camera.targetTexture=previous; RenderTexture.active=active; RenderTexture.ReleaseTemporary(target); Object.Destroy(texture); }
    }
}
