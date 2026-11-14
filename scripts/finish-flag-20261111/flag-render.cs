using System;
using System.Linq;
using UnityEngine;
using System.Collections;
IEnumerator Check(){
 GameMenuUI.Testing=true;
 foreach(var rb in UnityEngine.Object.FindObjectsOfType<Rigidbody2D>())rb.simulated=false;
 var level=CampaignLevel.Current;var goal=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(c=>c.kind==CampaignInteractable.Kind.Door);
 var go=new GameObject("Flag verification camera");var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.orthographic=true;cam.orthographicSize=3.5f;cam.transform.rotation=Quaternion.identity;
 var rt=new RenderTexture(1000,700,24);cam.targetTexture=rt;
 for(int i=0;i<2;i++){
  if(i==1){level.Collect(0);level.Collect(1);level.Collect(2);}goal.SendMessage("Refresh");
  cam.transform.position=goal.goalVisual.bounds.center+new Vector3(0,0,-10);cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,700),0,0);tex.Apply();System.IO.File.WriteAllBytes("scripts/finish-flag-20261111/"+(i==0?"down-render":"up-render")+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
  Debug.Log("Flag visible bounds="+goal.goalVisual.bounds+" active="+goal.goalVisual.enabled+" sprite="+goal.goalVisual.sprite.name);
 }
 RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(go);yield return null;
}
return Check();
