using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class DonutFallPlatform : MonoBehaviour
{
    public float delay=.7f, resetAfter=4f, fallSpeed=5f;
    public bool Triggered { get; private set; }
    public bool Falling => Triggered && timer>=delay;
    Vector2 origin;
    Rigidbody2D body;
    float timer;
    void Awake() { body=GetComponent<Rigidbody2D>(); origin=body.position; }
    void OnCollisionEnter2D(Collision2D collision) { OnCollisionStay2D(collision); }
    void OnCollisionStay2D(Collision2D collision)
    {
        if(Triggered || collision.rigidbody==null)return;
        if(Odyssey.Pvp.PvpLevelCatalog.IsPvpScene(gameObject.scene.name))
        {
            var session=Odyssey.Pvp.PvpSession.Current;
            if(!session.IsHost || session.Phase!=Odyssey.Pvp.PvpPhase.Racing)return;
            var pvp=collision.rigidbody.GetComponentInParent<Odyssey.Pvp.PvpActor>();
            if(!pvp || !pvp.ContactActive)return;
            if(collision.rigidbody.name=="Hammer") { BeginCountdown();return; }
            if(collision.rigidbody!=pvp.Body)return;
            float pvpTop=GetComponent<BoxCollider2D>().bounds.max.y;
            foreach(var point in collision.contacts)
                if(point.point.y>=pvpTop-.12f && pvp.Body.position.y>pvpTop) { BeginCountdown();break; }
            return;
        }
        var owner=collision.rigidbody.transform.root.GetComponentInChildren<Character.Lifecycle.Death>();
        var arms=collision.rigidbody.transform.root.GetComponentInChildren<Character.Control.ArmsSimulation>();
        if(owner!=null && !owner.Dead && arms!=null && arms.HammerTip!=null &&
            arms.HammerTip.GetComponentInParent<Rigidbody2D>()==collision.rigidbody)
        {
            BeginCountdown();
            return;
        }
        var actor=collision.rigidbody==null?null:collision.rigidbody.GetComponent<Character.Lifecycle.Death>();
        if(actor==null || actor.Dead || Triggered)return;
        float top=GetComponent<BoxCollider2D>().bounds.max.y;
        foreach(var contact in collision.contacts)
            if(contact.point.y>=top-.12f && actor.transform.position.y>top) { BeginCountdown(); break; }
    }
    void BeginCountdown() { if(Triggered)return; Triggered=true; timer=0; GameplayAudio.Play("platform_warn",.4f); }
    void FixedUpdate()
    {
        if(!Triggered)return;
        bool wasFalling=Falling;
        timer+=Time.fixedDeltaTime;
        if(!wasFalling && Falling)GameplayAudio.Play("platform_fall",.5f);
        if(timer>=resetAfter) { body.position=origin; body.velocity=Vector2.zero; Triggered=false;timer=0;return; }
        if(Falling)body.MovePosition(body.position+Vector2.down*fallSpeed*Time.fixedDeltaTime);
    }
}
