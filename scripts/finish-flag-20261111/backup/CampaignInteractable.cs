using Character.Lifecycle;
using UnityEngine;

public sealed class CampaignInteractable : MonoBehaviour
{
    public enum Kind { Star, Door, Checkpoint, Spikes, Switch, Barrier, Key, KeyGate }
    public Kind kind;
    public int starId;
    public string link;
    public Vector2 respawn;
    public Sprite inactiveSprite, activeSprite;
    public GameObject blocker;
    SpriteRenderer visual;

    void Start(){visual=GetComponent<SpriteRenderer>(); Refresh();}
    void Update(){Refresh();}
    void Refresh()
    {
        var level=CampaignLevel.Current;if(level==null)return;
        bool active=kind==Kind.Star?level.HasStar(starId):kind==Kind.Door?level.StarCount==3:level.Activated(link);
        if(kind==Kind.Star || kind==Kind.Key) { if(active)gameObject.SetActive(false); }
        else if(kind==Kind.Barrier || kind==Kind.KeyGate) {if(active && blocker!=null && blocker.activeSelf)GameplayAudio.Play("gate",.45f);if(blocker!=null)blocker.SetActive(!active);if(visual!=null)visual.enabled=!active;}
        else {if(visual!=null && (active?activeSprite:inactiveSprite)!=null)visual.sprite=active?activeSprite:inactiveSprite;if(kind==Kind.Door && blocker!=null)blocker.SetActive(!active);}
    }
    // Only the cauldron's rigidbody owns Death. Hammer and hand colliders cannot collect or die.
    public static Death Body(Collider2D c) => c.attachedRigidbody==null?null:c.attachedRigidbody.GetComponent<Death>();
    void OnTriggerEnter2D(Collider2D c){if(kind==Kind.Door && Body(c)!=null && CampaignLevel.Current!=null && CampaignLevel.Current.StarCount<3)GameplayAudio.Play("locked",.3f);Touch(c);}
    void OnTriggerStay2D(Collider2D c){if(kind==Kind.Door)Touch(c);}
    public void Touch(Collider2D c)
    {
        var level=CampaignLevel.Current;if(level==null || level.Won)return;
        var body=Body(c);
        if(kind==Kind.Switch)
        {
            if(c.attachedRigidbody!=null && c.transform.root==FindObjectOfType<Death>().transform.root)
            {
                if(!level.Activated(link)) GameplayAudio.Play("switch");
                level.Activate(link);
            }
            return;
        }
        if(body==null || body.Dead)return;
        switch(kind)
        {
            case Kind.Star:level.Collect(starId);Refresh();break;
            case Kind.Door:if(level.StarCount==3)level.TryExit();break;
            case Kind.Checkpoint:if(!level.Activated(link))GameplayAudio.Play("checkpoint");level.Activate(link);level.Checkpoint(respawn);Refresh();break;
            case Kind.Spikes:body.Kill();break;
            case Kind.Key:if(!level.Activated(link))GameplayAudio.Play("key");level.Activate(link);Refresh();break;
        }
    }
}
