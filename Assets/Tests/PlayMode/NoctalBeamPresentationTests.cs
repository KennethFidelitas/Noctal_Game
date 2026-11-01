using System.Collections;
using Noctal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class NoctalBeamPresentationTests
{
    private TlamaniMotor motor;
    private TlamaniAbilities abilities;
    private TlamaniLightFx effect;
    private IEnumerator Steps(int count) { for (int i=0;i<count;i++) yield return new WaitForFixedUpdate(); }
    [UnitySetUp]
    public IEnumerator Load()
    {
        Time.timeScale=1; yield return SceneManager.LoadSceneAsync("Mecanicas_Noctal"); yield return null;
        motor=Object.FindObjectOfType<TlamaniMotor>(); abilities=motor.GetComponent<TlamaniAbilities>(); effect=motor.GetComponent<TlamaniLightFx>();
        motor.GetComponent<TlamaniKeyboard>().enabled=false;
        foreach (var enemy in Object.FindObjectsOfType<NoctalEnemy>()) enemy.behaviorEnabled=false;
        yield return Steps(45);
    }
    [UnityTearDown] public IEnumerator Cleanup() { Time.timeScale=1; yield return null; }
    [UnityTest]
    public IEnumerator PreparaEspadaAntesDeEmitirDesdeLaPunta()
    {
        motor.RespawnAt(new Vector2(47.5f,.65f)); yield return Steps(15);
        abilities.SetLightHeld(true); yield return Steps(6); yield return null;
        Assert.That(abilities.IsLighting,Is.False); Assert.That(abilities.Energy,Is.EqualTo(100));
        NoctalGddSceneTests.Capture("13-preparar-espada");
        yield return Steps(10); yield return null;
        Assert.That(abilities.IsLighting,Is.True); Assert.That(effect.BeamVisible,Is.True);
        Assert.That(abilities.BeamHit,Is.True);
        var legs=motor.transform.Find("Postura de canalización/Piernas en movimiento").GetComponent<SpriteRenderer>();
        Assert.That(legs.enabled,Is.True); Assert.That(legs.bounds.size.y,Is.GreaterThan(.6f),"El recorte debe conservar el tamaño visible de las piernas.");
        Vector2 offset=abilities.BeamOrigin-motor.GetComponent<Rigidbody2D>().position;
        Assert.That(offset.x,Is.EqualTo(1.23f).Within(.01f)); Assert.That(offset.y,Is.EqualTo(.13f).Within(.01f));
        Assert.That(Vector2.Distance(effect.RenderOrigin,(Vector2)motor.transform.position+abilities.LightTipOffset),Is.LessThan(.01f));
        NoctalGddSceneTests.Capture("14-luz-espada");
    }
    [UnityTest]
    public IEnumerator LuzAcompanaGiroCaminataSaltoYPuma()
    {
        motor.RespawnAt(new Vector2(32,.65f)); yield return Steps(12);
        motor.SetInput(-1,false,false); abilities.SetLightHeld(true); yield return Steps(18); yield return null;
        Assert.That(motor.Velocity.x,Is.LessThan(-5)); Assert.That(motor.IsGrounded,Is.True);
        Assert.That(abilities.IsLighting,Is.True); Assert.That(effect.RenderOrigin.x,Is.LessThan(motor.transform.position.x));
        NoctalGddSceneTests.Capture("15-luz-izquierda");
        motor.SetInput(-1,true,false); yield return Steps(8); yield return null;
        Assert.That(motor.IsGrounded,Is.False); Assert.That(abilities.IsLighting,Is.True);
        NoctalGddSceneTests.Capture("16-luz-salto");
        abilities.UnlockPuma(); abilities.TryTogglePuma(); yield return Steps(16); yield return null;
        Assert.That(abilities.IsPuma,Is.True); Assert.That(abilities.IsLighting,Is.True);
        Assert.That(effect.RenderOrigin.y-motor.transform.position.y,Is.EqualTo(-.15f).Within(.01f));
        NoctalGddSceneTests.Capture("17-luz-puma");
        abilities.SetLightHeld(false); yield return Steps(2); yield return null;
        Assert.That(abilities.IsLighting,Is.False); Assert.That(effect.BeamVisible,Is.False);
        yield return Steps(8); Assert.That(abilities.LightPoseProgress,Is.EqualTo(0));
    }
    [UnityTest]
    public IEnumerator PuntaContraParedNoIluminaAlOtroLado()
    {
        var wall=new GameObject("Pared junto a la espada"); wall.layer=NoctalLayers.World;
        wall.transform.position=new Vector2(2,1); wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,2);
        var target=new GameObject("Sello detrás de la pared"); target.layer=NoctalLayers.Interactable;
        target.transform.position=new Vector2(4,.7f); target.AddComponent<BoxCollider2D>().isTrigger=true;
        var receiver=target.AddComponent<NoctalLightTarget>(); receiver.kind=NoctalLightTarget.TargetKind.Mechanism;
        abilities.SetLightHeld(true); yield return Steps(45); yield return null;
        Assert.That(receiver.Activated,Is.False); Assert.That(abilities.BeamEnd.x,Is.LessThan(2));
        Assert.That(effect.BeamVisible,Is.False,"Un obstáculo entre el cuerpo y la punta no produce un haz invertido.");
        Object.Destroy(wall); yield return null; yield return Steps(35);
        Assert.That(receiver.Activated,Is.True); Object.Destroy(target);
    }
    [UnityTest]
    public IEnumerator AtaquePausaMuerteYReinicioLimpianLaCanalizacion()
    {
        abilities.SetLightHeld(true); yield return Steps(16); yield return null;
        Assert.That(effect.BeamVisible,Is.True);
        abilities.TryAttack(); yield return Steps(2); yield return null;
        Assert.That(abilities.IsLighting,Is.False); Assert.That(effect.BeamVisible,Is.False);
        yield return Steps(22); abilities.SetLightHeld(true); yield return Steps(16);
        NoctalLevel.Instance.TogglePause(); yield return null;
        Assert.That(effect.BeamVisible,Is.False); Assert.That(abilities.LightPoseProgress,Is.EqualTo(0));
        NoctalLevel.Instance.TogglePause(); abilities.SetLightHeld(true); yield return Steps(16);
        motor.GetComponent<TlamaniVitals>().TakeDamage(99); yield return null;
        Assert.That(effect.BeamVisible,Is.False); Assert.That(abilities.IsLighting,Is.False);
        NoctalLevel.Instance.Retry(); yield return Steps(3);
        Assert.That(abilities.LightPoseProgress,Is.EqualTo(0)); Assert.That(abilities.Energy,Is.EqualTo(100));
    }
}
