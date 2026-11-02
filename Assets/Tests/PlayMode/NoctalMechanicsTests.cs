using System.Collections;
using System.Collections.Generic;
using Noctal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NoctalMechanicsTests
{
    private readonly List<Object> objects = new List<Object>();
    private readonly Vector2 origin = new Vector2(2000,2000);
    private TlamaniMotor motor;
    private TlamaniAbilities abilities;
    private TlamaniVitals health;
    private Rigidbody2D body;
    private GameObject Make(string name,Vector2 position,int layer=NoctalLayers.World)
    { var go=new GameObject(name); go.transform.position=origin+position; go.layer=layer; objects.Add(go); return go; }
    private IEnumerator Steps(int count) { for(int i=0;i<count;i++) yield return new WaitForFixedUpdate(); }
    private NoctalEnemy Enemy(NoctalEnemy.EnemyKind kind,Vector2 position,bool ai=false)
    {
        var go=Make(kind.ToString(),position,NoctalLayers.Enemy); go.SetActive(false);
        go.AddComponent<Rigidbody2D>(); go.AddComponent<CapsuleCollider2D>().size=new Vector2(.7f,.7f);
        var enemy=go.AddComponent<NoctalEnemy>(); enemy.kind=kind; enemy.maximumHealth=kind==NoctalEnemy.EnemyKind.Llanthu?18:4; enemy.target=health;
        go.SetActive(true); enemy.enabled=ai;
        if(!ai) { go.GetComponent<Rigidbody2D>().gravityScale=0; go.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll; }
        return enemy;
    }
    [UnitySetUp]
    public IEnumerator Setup()
    {
        Time.timeScale=1; Physics2D.IgnoreLayerCollision(NoctalLayers.Enemy,NoctalLayers.Player,true);
        Make("Suelo",new Vector2(0,-.5f)).AddComponent<BoxCollider2D>().size=new Vector2(40,1);
        var actor=Make("Tlamani",new Vector2(0,2),NoctalLayers.Player); actor.SetActive(false);
        var capsule=actor.AddComponent<CapsuleCollider2D>(); capsule.size=new Vector2(.68f,1.1f); capsule.offset=new Vector2(-.1f,-.07f);
        var material=new PhysicsMaterial2D { friction=0,bounciness=0 }; objects.Add(material); capsule.sharedMaterial=material;
        body=actor.AddComponent<Rigidbody2D>(); motor=actor.AddComponent<TlamaniMotor>(); health=actor.AddComponent<TlamaniVitals>(); abilities=actor.AddComponent<TlamaniAbilities>();
        actor.SetActive(true); yield return Steps(40);
    }
    [UnityTearDown]
    public IEnumerator Cleanup() { Time.timeScale=1; foreach(var obj in objects) if(obj!=null) Object.Destroy(obj); objects.Clear(); foreach(var shot in Object.FindObjectsOfType<NoctalProjectile>()) Object.Destroy(shot.gameObject); yield return null; }

    [UnityTest]
    public IEnumerator AtaqueCortoGolpeaUnaVezAunqueHayaVariosColliders()
    {
        var enemy=Enemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(1,.6f));
        var child=new GameObject("Segundo collider"); child.transform.SetParent(enemy.transform,false); child.layer=NoctalLayers.Enemy; child.AddComponent<BoxCollider2D>().size=Vector2.one*.4f;
        Assert.That(abilities.TryAttack(),Is.True); Assert.That(abilities.TryAttack(),Is.False);
        yield return Steps(20); Assert.That(enemy.Health,Is.EqualTo(3));
        yield return Steps(5); abilities.TryAttack(); yield return Steps(20); Assert.That(enemy.Health,Is.EqualTo(2));
    }
    [UnityTest]
    public IEnumerator AtaqueAereoAlcanzaEnemigoElevado()
    {
        motor.SetInput(0,true,false); yield return Steps(8);
        var enemy=Enemy(NoctalEnemy.EnemyKind.Pilpintu,(Vector2)motor.transform.position-origin+new Vector2(.9f,.4f));
        Assert.That(motor.IsGrounded,Is.False); abilities.TryAttack(); yield return Steps(12);
        Assert.That(enemy.Health,Is.LessThan(4));
    }
    [UnityTest]
    public IEnumerator AtaqueNoCruzaParedNiAlcanzaObjetivosLejanosODetras()
    {
        var behind=Enemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(-1,.6f));
        var distant=Enemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(3,.6f));
        var blocked=Enemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(1,.6f));
        Make("Pared",new Vector2(.5f,1)).AddComponent<BoxCollider2D>().size=new Vector2(.1f,2);
        abilities.TryAttack(); yield return Steps(20);
        Assert.That(behind.Health,Is.EqualTo(4)); Assert.That(distant.Health,Is.EqualTo(4)); Assert.That(blocked.Health,Is.EqualTo(4));
    }
    [UnityTest]
    public IEnumerator VidaTieneInvulnerabilidadMuerteYReinicio()
    {
        Assert.That(health.TakeDamage(1),Is.True); Assert.That(health.TakeDamage(1),Is.False); Assert.That(health.Health,Is.EqualTo(5));
        yield return Steps(50); Assert.That(health.TakeDamage(99),Is.True);
        Assert.That(health.IsDead,Is.True); Assert.That(motor.enabled,Is.False); Assert.That(abilities.TryAttack(),Is.False);
        health.Restore(); abilities.ResetForRetry(); motor.Respawn(); yield return Steps(1);
        Assert.That(health.Health,Is.EqualTo(6)); Assert.That(motor.enabled,Is.True);
    }
    [UnityTest]
    public IEnumerator PumaSeDesbloqueaYCambiaMovilidadSinHundirLosPies()
    {
        Assert.That(abilities.TryTogglePuma(),Is.False);
        var capsule=motor.GetComponent<CapsuleCollider2D>(); float feet=capsule.bounds.min.y;
        abilities.UnlockPuma(); Assert.That(abilities.TryTogglePuma(),Is.True); yield return Steps(5);
        Assert.That(abilities.IsPuma,Is.True); Assert.That(capsule.bounds.min.y,Is.EqualTo(feet).Within(.04f));
        motor.SetInput(1,false,false); yield return Steps(15); Assert.That(body.velocity.x,Is.GreaterThan(8));
        Assert.That(abilities.TryTogglePuma(),Is.True); Assert.That(abilities.IsPuma,Is.False);
    }
    [UnityTest]
    public IEnumerator PumaNoPuedeVolverseHumanoDentroDeUnPasoBajo()
    {
        abilities.UnlockPuma(); abilities.TryTogglePuma(); yield return Steps(5);
        var roof=Make("Techo bajo",new Vector2(0,1.1f)).AddComponent<BoxCollider2D>(); roof.size=new Vector2(3,.4f); yield return Steps(2);
        Assert.That(abilities.TryTogglePuma(),Is.False); Assert.That(abilities.IsPuma,Is.True);
        Object.Destroy(roof.gameObject); yield return null; Assert.That(abilities.TryTogglePuma(),Is.True);
    }
    [UnityTest]
    public IEnumerator LuzConsumeEnergiaSeAgotaYRecargaConExploracion()
    {
        abilities.SetLightHeld(true); yield return Steps(300);
        Assert.That(abilities.Energy,Is.EqualTo(0)); Assert.That(abilities.IsLighting,Is.False);
        var go=Make("Cristal",new Vector2(1,.5f),NoctalLayers.Interactable); var pickup=go.AddComponent<NoctalPickup>(); pickup.amount=35;
        Assert.That(pickup.TryCollect(abilities),Is.True); Assert.That(pickup.TryCollect(abilities),Is.False);
        Assert.That(abilities.Energy,Is.EqualTo(35));
    }
    [UnityTest]
    public IEnumerator LuzDebilitaEnemigosPeroNoCruzaParedes()
    {
        var enemy=Enemy(NoctalEnemy.EnemyKind.Guerrero,new Vector2(2,.7f));
        var wall=Make("Pared",new Vector2(1,1)).AddComponent<BoxCollider2D>(); wall.size=new Vector2(.1f,2);
        abilities.SetLightHeld(true); yield return Steps(10); Assert.That(enemy.IsExposed,Is.False);
        Object.Destroy(wall.gameObject); yield return null; yield return Steps(5); Assert.That(enemy.IsExposed,Is.True);
        abilities.SetLightHeld(false); yield return Steps(155); Assert.That(enemy.IsExposed,Is.False);
    }
    [UnityTest]
    public IEnumerator LuzDisipaSombraYActivaMecanismo()
    {
        var shadow=Make("Sombra",new Vector2(2,.7f)); shadow.AddComponent<BoxCollider2D>(); var target=shadow.AddComponent<NoctalLightTarget>();
        abilities.SetLightHeld(true); yield return Steps(45); Assert.That(target.Activated,Is.True); Assert.That(shadow.activeSelf,Is.False);
        var seal=Make("Sello",new Vector2(3,.7f),NoctalLayers.Interactable); seal.AddComponent<BoxCollider2D>().isTrigger=true;
        var receiver=seal.AddComponent<NoctalLightTarget>(); receiver.kind=NoctalLightTarget.TargetKind.Mechanism;
        receiver.gate=Make("Puerta",new Vector2(4,1)); yield return Steps(35);
        Assert.That(receiver.Activated,Is.True); Assert.That(receiver.gate.activeSelf,Is.False);
    }
    [UnityTest]
    public IEnumerator GuardiaExigeEspaldaOLuzYElCombateRecuperaEnergia()
    {
        var guard=Enemy(NoctalEnemy.EnemyKind.Guerrero,new Vector2(1,.7f));
        Assert.That(guard.ReceiveHit(1,origin),Is.False); Assert.That(guard.ReceiveHit(1,origin+Vector2.right*2),Is.True);
        abilities.SetLightHeld(true); yield return Steps(20); abilities.SetLightHeld(false); float depleted=abilities.Energy;
        abilities.TryAttack(); yield return Steps(20);
        Assert.That(guard.Health,Is.EqualTo(2)); Assert.That(abilities.Energy,Is.GreaterThan(depleted));
    }
    [UnityTest]
    public IEnumerator ApasankaTelegrafiaYEmbiste()
    {
        var spider=Enemy(NoctalEnemy.EnemyKind.Apasanka,new Vector2(3,.4f),true);
        yield return Steps(5); Assert.That(spider.State,Is.EqualTo(NoctalEnemy.EnemyState.Telegraph));
        yield return Steps(22); Assert.That(spider.State,Is.EqualTo(NoctalEnemy.EnemyState.Attack));
        Assert.That(Mathf.Abs(spider.GetComponent<Rigidbody2D>().velocity.x),Is.GreaterThan(4));
    }
    [UnityTest]
    public IEnumerator PilpintuHacePicadaYQuedaVulnerableAbajo()
    {
        var moth=Enemy(NoctalEnemy.EnemyKind.Pilpintu,new Vector2(1,2.8f),true);
        yield return Steps(5); Assert.That(moth.State,Is.EqualTo(NoctalEnemy.EnemyState.Telegraph));
        yield return Steps(60); Assert.That(moth.State,Is.EqualTo(NoctalEnemy.EnemyState.Recover));
        Assert.That(moth.transform.position.y,Is.LessThan(origin.y+2));
    }
    [UnityTest]
    public IEnumerator NawiPreparaRayoYCausaDano()
    {
        var eye=Enemy(NoctalEnemy.EnemyKind.Nawi,new Vector2(3,.8f),true);
        yield return Steps(5); Assert.That(eye.State,Is.EqualTo(NoctalEnemy.EnemyState.Telegraph));
        yield return Steps(50); Assert.That(health.Health,Is.EqualTo(5));
    }
    [UnityTest]
    public IEnumerator LlanthuExigeLuzYEntraEnSegundaFase()
    {
        var boss=Enemy(NoctalEnemy.EnemyKind.Llanthu,new Vector2(2,.8f));
        Assert.That(boss.ReceiveHit(1,origin),Is.False); boss.ExposeToLight(3);
        Assert.That(boss.ReceiveHit(9,origin),Is.True); Assert.That(boss.IsPhaseTwo,Is.True);
        Assert.That(boss.ReceiveHit(9,origin),Is.True); Assert.That(boss.IsDead,Is.True);
        boss.ResetActor(); Assert.That(boss.Health,Is.EqualTo(18)); Assert.That(boss.IsPhaseTwo,Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator LlanthuDisparaOndasYCambiaDePosicionEnFaseDos()
    {
        var boss=Enemy(NoctalEnemy.EnemyKind.Llanthu,new Vector2(5,.8f),true);
        boss.ExposeToLight(.01f); boss.ReceiveHit(9,origin);
        yield return Steps(5); Assert.That(boss.State,Is.EqualTo(NoctalEnemy.EnemyState.Telegraph));
        yield return Steps(26); Assert.That(Object.FindObjectsOfType<NoctalProjectile>().Length,Is.GreaterThanOrEqualTo(2));
        bool relocated=false;
        for(int i=0;i<260;i++)
        {
            yield return Steps(1);
            if(health.IsDead) health.Restore();
            if(boss.transform.position.x>origin.x+8) relocated=true;
        }
        Assert.That(relocated,Is.True,"La segunda fase debe cambiar de lado en la arena.");
    }
}
