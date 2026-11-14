using System;
using System.Linq;
using UnityEngine;
using System.Collections;
IEnumerator Check(){
 GameMenuUI.Testing=true;
 var level=CampaignLevel.Current;
 var goal=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(c=>c.kind==CampaignInteractable.Kind.Door);
 foreach(var rb in UnityEngine.Object.FindObjectsOfType<Rigidbody2D>())rb.simulated=false;
 var camera=Camera.main;var brain=camera.GetComponent<Cinemachine.CinemachineBrain>();if(brain)brain.enabled=false;
 camera.orthographic=true;camera.orthographicSize=3.3f;camera.transform.position=goal.goalVisual.bounds.center+new Vector3(0,0,-10);
 yield return null;
 goal.SendMessage("Refresh");
 if(goal.goalVisual.sprite!=goal.inactiveSprite)throw new Exception("Down flag not applied");
 if(level.TryExit())throw new Exception("Locked exit allowed");
 ScreenCapture.CaptureScreenshot("scripts/finish-flag-20261111/down-game.png");yield return new WaitForEndOfFrame();yield return null;
 level.Collect(0);level.Collect(1);level.Collect(2);goal.SendMessage("Refresh");
 if(goal.goalVisual.sprite!=goal.activeSprite)throw new Exception("Up flag not applied");
 if(goal.GetComponent<SpriteRenderer>().enabled)throw new Exception("Door still visible");
 ScreenCapture.CaptureScreenshot("scripts/finish-flag-20261111/up-game.png");yield return new WaitForEndOfFrame();yield return null;
 System.IO.File.WriteAllText("scripts/finish-flag-20261111/play.txt","PASS: down at 0/3; locked exit rejected; up at 3/3; old door hidden. No completion or save writes. Frozen rig, not traversal.");
}
return Check();
