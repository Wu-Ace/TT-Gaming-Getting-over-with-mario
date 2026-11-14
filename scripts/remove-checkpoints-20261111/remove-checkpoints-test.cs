using System;
using System.Linq;
using System.Collections;
using UnityEngine;
IEnumerator Check(){
 var level=CampaignLevel.Current;var initial=level.initialSpawn;
 if(UnityEngine.Object.FindObjectsOfType<CampaignInteractable>(true).Any(c=>c.kind==CampaignInteractable.Kind.Checkpoint))throw new Exception("Checkpoint remains");
 level.Checkpoint(initial+new Vector2(99,99));
 var death=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();death.Kill();
 float deadline=Time.realtimeSinceStartup+15;
 while((CampaignLevel.Current==null || CampaignLevel.Current==level)&&Time.realtimeSinceStartup<deadline)yield return null;
 yield return null;
 var next=CampaignLevel.Current;if(next==null || next==level)throw new Exception("Death did not reload scene");
 if(Vector2.Distance(next.SpawnPosition,initial)>.001f)throw new Exception("Respawn not initial");
 var player=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();if(player.Dead)throw new Exception("Player not alive after reload");
 if(Vector2.Distance(player.transform.position,initial)>.5f)throw new Exception("Body not near start");
 System.IO.File.WriteAllText("scripts/remove-checkpoints-20261111/play.txt","PASS Level05: no checkpoints; legacy checkpoint request ignored; actual Death.Kill -> automatic scene reload -> living body at initial spawn. SpawnPosition="+next.SpawnPosition+" body="+player.transform.position+". No explicit save writes.");
}
return Check();
