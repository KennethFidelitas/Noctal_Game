using System.Collections;
using System.Collections.Generic;
using Noctal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TlamaniMovementTests
{
    private readonly List<Object> objects = new List<Object>();
    private TlamaniMotor motor;
    private Rigidbody2D body;
    private CapsuleCollider2D capsule;
    private BoxCollider2D floor;
    // Lejos de cualquier geometría de las escenas del equipo.
    private readonly Vector2 origin = new Vector2(1000f, 1000f);
    private GameObject Make(string name, Vector2 local)
    {
        var go = new GameObject(name); go.transform.position = origin + local; objects.Add(go); return go;
    }
    private BoxCollider2D Solid(string name, Vector2 local, Vector2 size)
    {
        var c = Make(name, local).AddComponent<BoxCollider2D>(); c.size = size; return c;
    }
    private IEnumerator Steps(int count)
    {
        for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
    }
    [UnitySetUp]
    public IEnumerator Setup()
    {
        floor = Solid("Suelo prueba", new Vector2(0,-0.5f), new Vector2(30,1));
        var go = Make("Tlamani prueba", new Vector2(0,2));
        capsule = go.AddComponent<CapsuleCollider2D>(); capsule.size = new Vector2(0.68f,1.1f);
        var mat = new PhysicsMaterial2D { friction = 0, bounciness = 0 }; objects.Add(mat); capsule.sharedMaterial = mat;
        body = go.AddComponent<Rigidbody2D>(); motor = go.AddComponent<TlamaniMotor>();
        yield return Steps(40);
        Assert.That(motor.IsGrounded, Is.True, "El personaje debe comenzar apoyado.");
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (var item in objects) if (item != null) Object.Destroy(item);
        objects.Clear(); yield return null;
    }
    [UnityTest]
    public IEnumerator CaminaEnAmbasDireccionesYFrena()
    {
        float start = body.position.x;
        motor.SetInput(1,false,false); yield return Steps(30);
        Assert.That(body.position.x - start, Is.GreaterThan(2.5f));
        Assert.That(motor.IsGrounded, Is.True);
        motor.SetInput(0,false,false); yield return Steps(10);
        Assert.That(Mathf.Abs(body.velocity.x), Is.LessThan(0.05f));
        float right = body.position.x;
        motor.SetInput(-1,false,false); yield return Steps(30);
        Assert.That(body.position.x, Is.LessThan(right - 2.5f));
        Assert.That(motor.Facing, Is.EqualTo(-1));
    }
    [UnityTest]
    public IEnumerator SaltaYRegresaSinAtravesarElSuelo()
    {
        float start = body.position.y;
        motor.SetInput(0,true,false); yield return Steps(16);
        Assert.That(body.position.y - start, Is.GreaterThan(1.8f));
        Assert.That(motor.IsGrounded, Is.False);
        yield return Steps(50);
        Assert.That(motor.IsGrounded, Is.True);
        Assert.That(body.position.y, Is.EqualTo(start).Within(0.04f));
        Assert.That(capsule.bounds.min.y, Is.GreaterThanOrEqualTo(floor.bounds.max.y - 0.02f));
    }
    [UnityTest]
    public IEnumerator NoPermiteDobleSaltoEnElAire()
    {
        motor.SetInput(0,true,false); yield return Steps(10);
        float before = body.velocity.y;
        motor.SetInput(0,true,false); yield return Steps(2);
        Assert.That(body.velocity.y, Is.LessThan(before));
    }
    [UnityTest]
    public IEnumerator SoltarEspacioRecortaElSalto()
    {
        motor.SetInput(0,true,false); yield return Steps(5);
        float before = body.velocity.y;
        motor.SetInput(0,false,false,true); yield return Steps(2);
        Assert.That(body.velocity.y, Is.LessThan(before * 0.6f));
    }
    [UnityTest]
    public IEnumerator DashRecorreDistanciaYRespetaRecarga()
    {
        float start = body.position.x;
        motor.SetInput(1,false,true); yield return Steps(8);
        Assert.That(body.position.x - start, Is.InRange(2.3f,3.3f));
        motor.SetInput(0,false,true); yield return Steps(3);
        Assert.That(motor.IsDashing, Is.False);
        Assert.That(motor.DashCooldownRemaining, Is.GreaterThan(0f));
        yield return Steps(28);
        Assert.That(motor.CanDash, Is.True);
        Assert.That(body.gravityScale, Is.EqualTo(3f));
    }
    [UnityTest]
    public IEnumerator DashSinEntradaTerminaSinDeslizamiento()
    {
        yield return FinishDashWithInput(0f);
        Assert.That(body.velocity.x, Is.EqualTo(0f).Within(0.01f));
        float end = body.position.x;
        yield return Steps(8);
        Assert.That(body.position.x, Is.EqualTo(end).Within(0.01f));
    }
    [UnityTest]
    public IEnumerator DashConEntradaRecuperaVelocidadNormal()
    {
        yield return FinishDashWithInput(1f);
        Assert.That(body.velocity.x, Is.EqualTo(6f).Within(0.01f));
        Assert.That(body.gravityScale, Is.EqualTo(3f));
    }
    [UnityTest]
    public IEnumerator DashPermiteCambiarDireccionAlTerminar()
    {
        yield return FinishDashWithInput(-1f);
        Assert.That(body.velocity.x, Is.EqualTo(-6f).Within(0.01f));
        Assert.That(motor.Facing, Is.EqualTo(-1));
    }
    [UnityTest]
    public IEnumerator DashAereoRecuperaGravedadAlTerminar()
    {
        motor.RespawnAt(origin + new Vector2(0, 5));
        yield return FinishDashWithInput(0f);
        Assert.That(body.gravityScale, Is.EqualTo(3f));
        Assert.That(body.velocity.y, Is.LessThan(0f));
        Assert.That(body.velocity.x, Is.EqualTo(0f).Within(0.01f));
    }
    private IEnumerator FinishDashWithInput(float input)
    {
        motor.SetInput(1, false, true);
        yield return Steps(1);
        Assert.That(motor.IsDashing, Is.True);
        motor.SetInput(input, false, false);
        for (int i = 0; i < 20 && motor.IsDashing; i++) yield return Steps(1);
        Assert.That(motor.IsDashing, Is.False);
    }
    [UnityTest]
    public IEnumerator DashIzquierdoSeDetieneAnteParedDelgada()
    {
        var wall = Solid("Pared delgada izquierda", new Vector2(-1.5f,2), new Vector2(0.05f,6));
        motor.SetInput(-1,false,true); yield return Steps(14);
        Assert.That(capsule.bounds.min.x, Is.GreaterThanOrEqualTo(wall.bounds.max.x - 0.02f));
        Assert.That(motor.IsDashing, Is.False);
    }
    [UnityTest]
    public IEnumerator DashDerechoSeDetieneAnteParedDelgada()
    {
        var wall = Solid("Pared delgada derecha", new Vector2(1.5f,2), new Vector2(0.05f,6));
        motor.SetInput(1,false,true); yield return Steps(14);
        Assert.That(capsule.bounds.max.x, Is.LessThanOrEqualTo(wall.bounds.min.x + 0.02f));
        Assert.That(motor.IsDashing, Is.False);
    }
    [UnityTest]
    public IEnumerator NoAtravesarTechoNiContarloComoSuelo()
    {
        var ceiling = Solid("Techo", new Vector2(0,2), new Vector2(8,0.1f));
        motor.SetInput(0,true,false);
        for (int i=0;i<12;i++)
        {
            yield return new WaitForFixedUpdate();
            Assert.That(capsule.bounds.max.y, Is.LessThanOrEqualTo(ceiling.bounds.min.y + 0.025f));
        }
        Assert.That(motor.IsGrounded, Is.False);
    }
    [UnityTest]
    public IEnumerator AterrizaSobrePlataformaDelgadaDesdeArriba()
    {
        var platform = Solid("Plataforma", new Vector2(0,1.5f), new Vector2(4,0.05f));
        motor.RespawnAt(origin + new Vector2(0,6)); body.velocity = Vector2.down * 40;
        yield return Steps(35);
        Assert.That(motor.IsGrounded, Is.True);
        Assert.That(capsule.bounds.min.y, Is.EqualTo(platform.bounds.max.y).Within(0.035f));
    }
    [UnityTest]
    public IEnumerator UnaCargaDeDashAereoHastaAterrizar()
    {
        motor.RespawnAt(origin + new Vector2(0,15));
        motor.SetInput(1,false,true); yield return Steps(31);
        Assert.That(motor.IsGrounded, Is.False);
        Assert.That(motor.CanDash, Is.False);
        motor.SetInput(0,false,true); yield return Steps(2);
        Assert.That(motor.IsDashing, Is.False);
        yield return Steps(45);
        Assert.That(motor.IsGrounded, Is.True);
        Assert.That(motor.CanDash, Is.True);
    }
    [UnityTest]
    public IEnumerator ReiniciarDuranteDashLimpiaVelocidadYGravedad()
    {
        motor.SetInput(1,false,true); yield return Steps(2);
        motor.RespawnAt(origin + new Vector2(0,2));
        Assert.That(motor.IsDashing, Is.False);
        Assert.That(body.gravityScale, Is.EqualTo(3f));
        Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
        yield return Steps(40); Assert.That(motor.IsGrounded, Is.True);
    }
    [UnityTest]
    public IEnumerator PermiteSaltarJustoDespuesDeSalirDelBorde()
    {
        floor.size = new Vector2(2,1);
        motor.SetInput(1,false,false);
        for (int i=0;i<40 && motor.IsGrounded;i++) yield return new WaitForFixedUpdate();
        Assert.That(motor.IsGrounded, Is.False);
        motor.SetInput(1,true,false); yield return Steps(2);
        Assert.That(body.velocity.y, Is.GreaterThan(8f));
    }
    [UnityTest]
    public IEnumerator SaltoAnticipadoSeEjecutaAlAterrizar()
    {
        motor.RespawnAt(origin + new Vector2(0,2));
        for (int i=0;i<40 && capsule.bounds.min.y > origin.y+0.18f;i++) yield return new WaitForFixedUpdate();
        Assert.That(motor.IsGrounded, Is.False);
        motor.SetInput(0,true,false); yield return Steps(4);
        Assert.That(body.velocity.y, Is.GreaterThan(8f));
    }
}
