using System;
using System.Linq;
using UnityEngine;
using System.Collections;
IEnumerator Check(){
 var prev=GameMenuUI.PreferencePrefix;var testing=GameMenuUI.Testing;GameMenuUI.PreferencePrefix="Goombas.Campaign.TEST.NoGateFinal."+Guid.NewGuid().ToString("N")+".";GameMenuUI.Testing=true;
 try{
 foreach(var rb in UnityEngine.Object.FindObjectsOfType<Rigidbody2D>())rb.simulated=false;
 var level=CampaignLevel.Current;var goal=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(c=>c.kind==CampaignInteractable.Kind.Door);var death=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();var body=death.GetComponentsInChildren<Collider2D>().First(c=>CampaignInteractable.Body(c)==death);
 if(level.StarCount!=0)throw new Exception("Not zero diamonds");goal.Touch(body);yield return null;
 if(!level.Won||goal.goalVisual.sprite!=goal.activeSprite)throw new Exception("Completion failed");
 ScreenCapture.CaptureScreenshot("scripts/no-diamond-gate-20261111/final-completion.png");yield return new WaitForEndOfFrame();yield return null;
 System.IO.File.WriteAllText("scripts/no-diamond-gate-20261111/final-play.txt","PASS Level01 zero-diamond body finish and raised flag, isolated preference prefix, updated completion UI.");
 }finally{GameMenuUI.PreferencePrefix=prev;GameMenuUI.Testing=testing;}
}
return Check();
