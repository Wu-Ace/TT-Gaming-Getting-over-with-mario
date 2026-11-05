using UnityEngine;

// Author's full-width foreground layers retain their canvas and pixel proportions.
public sealed class AncientForestDepth : MonoBehaviour
{
 public Sprite undergrowth, foreground;
 Camera view; SpriteRenderer[] layers;
 void Awake(){
  view=GetComponent<Camera>();layers=new SpriteRenderer[6];
  for(int i=0;i<6;i++){var go=new GameObject(i<3?"Forest near bushes":"Forest foreground silhouette");go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=i<3?undergrowth:foreground;r.sortingOrder=i<3?-100:300;layers[i]=r;}
 }
 void LateUpdate(){
  if(view==null||!view.orthographic)return;
  for(int i=0;i<layers.Length;i++){var r=layers[i];if(r.sprite==null)continue;float scale=Mathf.Max(view.orthographicSize*2/r.sprite.bounds.size.y,view.orthographicSize*2*view.aspect/r.sprite.bounds.size.x);float width=r.sprite.bounds.size.x*scale;float rate=i<3?.12f:.25f;float shift=Mathf.Repeat(transform.position.x*rate,width);
   r.transform.localScale=Vector3.one*scale;r.transform.localPosition=new Vector3((i%3-1)*width-shift,0,10);
  }
 }
}
