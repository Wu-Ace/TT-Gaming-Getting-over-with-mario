using UnityEngine;
public sealed class CampaignEnemy : MonoBehaviour
{
    public enum Species { Bee, Ghost, Plant }
    public Species species;
    public Sprite[] frames;
    public Sprite defeated;
    public float range=2, speed=.8f, phase;
    Vector3 origin;
    SpriteRenderer visual;
    Collider2D hit;
    bool dead;
    void Start(){origin=transform.position;visual=GetComponent<SpriteRenderer>();hit=GetComponent<Collider2D>();}
    void Update()
    {
        if(dead)return;
        float t=Time.time*speed+phase;
        bool extended=species!=Species.Plant || t%6f>2.4f;
        hit.enabled=extended;visual.enabled=extended;
        if(species!=Species.Plant)transform.position=origin+new Vector3(Mathf.Sin(t)*range,Mathf.Sin(t*2)*.18f,0);
        if(frames.Length>0)visual.sprite=frames[(int)(Time.time*7)%frames.Length];
        visual.flipX=Mathf.Cos(t)<0;
    }
    void OnTriggerEnter2D(Collider2D c)
    {
        if(dead || CampaignLevel.Current==null || CampaignLevel.Current.Won)return;
        var body=CampaignInteractable.Body(c);
        if(body!=null){body.Kill();return;}
        if(c.attachedRigidbody!=null && c.attachedRigidbody.name=="Hammer")
        {
            GameplayAudio.Play(species==Species.Bee?"enemy_bee":species==Species.Ghost?"enemy_ghost":"enemy_plant");
            dead=true;hit.enabled=false;visual.enabled=true;if(defeated!=null)visual.sprite=defeated;
            var rb=gameObject.AddComponent<Rigidbody2D>();rb.gravityScale=2;rb.velocity=Vector2.up*3;Destroy(gameObject,3);
        }
    }
}
