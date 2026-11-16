using UnityEngine;
public sealed class OptionalCampaignCoin : MonoBehaviour {
    public int id;
    Transform visual;
    float phase;
    void Start() {
        var source=GetComponent<SpriteRenderer>();
        var child=new GameObject("Spinning icon coin");
        visual=child.transform;
        visual.SetParent(transform,false);
        var renderer=child.AddComponent<SpriteRenderer>();
        renderer.sprite=source.sprite;
        renderer.sortingLayerID=source.sortingLayerID;
        renderer.sortingOrder=source.sortingOrder;
        source.enabled=false;
        phase=Random.value*Mathf.PI*2f;
    }
    void Update() {
        float width=Mathf.Max(.08f,Mathf.Abs(Mathf.Cos(Time.time*4.5f+phase)));
        if(visual!=null)visual.localScale=new Vector3(width,1,1);
        if(CampaignLevel.Current!=null && CampaignLevel.Current.HasCoin(id)) { if(Odyssey.Pvp.PvpLevelCatalog.IsPvpScene(gameObject.scene.name))gameObject.SetActive(false);else Destroy(gameObject); }
    }
    void OnTriggerEnter2D(Collider2D other) {
        if(Odyssey.Pvp.PvpLevelCatalog.IsPvpScene(gameObject.scene.name)) {
            var actor=other.GetComponentInParent<Odyssey.Pvp.PvpActor>();
            if(Odyssey.Pvp.PvpSession.Current.IsHost && actor && actor.ContactActive && other.attachedRigidbody==actor.Body && CampaignLevel.Current!=null)
                if(CampaignLevel.Current.CollectCoin(id))gameObject.SetActive(false);
            return;
        }
        var body=CampaignInteractable.Body(other);
        if(body==null || body.Dead || CampaignLevel.Current==null)return;
        if(CampaignLevel.Current.CollectCoin(id))Destroy(gameObject);
    }
}
