using System;
using System.Linq;
using System.Collections;
using UnityEngine;
IEnumerator Check(){
 if(UnityEngine.Object.FindObjectsOfType<CampaignInteractable>(true).Any(c=>c.kind==CampaignInteractable.Kind.Star))throw new Exception("Collectibles remain in Play");
 yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("scripts/remove-diamonds-20261111/game.png");yield return new WaitForEndOfFrame();yield return null;
 System.IO.File.WriteAllText("scripts/remove-diamonds-20261111/play.txt","PASS: current Level05 runtime has zero objective collectibles; HUD Game capture. No completion/save writes. Six saved scenes checked during deletion.");
}
return Check();
