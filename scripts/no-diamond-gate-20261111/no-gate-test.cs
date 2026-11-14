using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
IEnumerator Check(){
 var original=SceneManager.GetActiveScene().path;
 var previousPrefix=GameMenuUI.PreferencePrefix;var previousTesting=GameMenuUI.Testing;
 var prefix="Goombas.Campaign.TEST.NoDiamondGate."+Guid.NewGuid().ToString("N")+".";
 GameMenuUI.PreferencePrefix=prefix;GameMenuUI.Testing=true;
 var lines=new System.Collections.Generic.List<string>();
 try{
 foreach(var name in new[]{"Tutorial","Level01","Level02_Jungle","Level03_Beach","Level04_Town","Level05_Cave"}){
  SceneManager.LoadScene(name);yield return null;yield return null;
  foreach(var rb in UnityEngine.Object.FindObjectsOfType<Rigidbody2D>())rb.simulated=false;
  var level=CampaignLevel.Current;var goal=UnityEngine.Object.FindObjectsOfType<CampaignInteractable>().First(c=>c.kind==CampaignInteractable.Kind.Door);
  var death=UnityEngine.Object.FindObjectOfType<Character.Lifecycle.Death>();
  var body=death.GetComponentsInChildren<Collider2D>().First(c=>CampaignInteractable.Body(c)==death);
  var other=UnityEngine.Object.FindObjectsOfType<Collider2D>().First(c=>CampaignInteractable.Body(c)==null && c!=goal.GetComponent<Collider2D>());
  if(level.StarCount!=0)throw new Exception("Expected zero collectibles");
  goal.SendMessage("Refresh");if(goal.blocker && goal.blocker.activeSelf)throw new Exception("Blocker remains");
  goal.Touch(other);if(level.Won)throw new Exception("Non-body completed");
  goal.Touch(body);yield return null;
  if(!level.Won || level.StarCount!=0)throw new Exception("Zero collectible completion failed");
  var win=UnityEngine.Object.FindObjectOfType<Winning.WinThresholdDetection>();
  if(!(bool)typeof(Winning.WinThresholdDetection).GetField("_won",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(win))throw new Exception("Underlying win rejected");
  if(goal.goalVisual.sprite!=goal.activeSprite)throw new Exception("Flag not raised");
  if(level.TryExit())throw new Exception("Duplicate completion accepted");
  if(GameMenuUI.PreferencePrefix!=prefix)throw new Exception("Save isolation lost");
  lines.Add(name+": PASS zero-diamond body finish, non-body rejected, raised flag, blocker disabled, underlying win, duplicate rejected");
  if(name=="Level01"){ScreenCapture.CaptureScreenshot("scripts/no-diamond-gate-20261111/completion.png");yield return new WaitForEndOfFrame();yield return null;}
 }
 }finally{GameMenuUI.PreferencePrefix=previousPrefix;GameMenuUI.Testing=previousTesting;SceneManager.LoadScene(original);System.IO.File.WriteAllLines("scripts/no-diamond-gate-20261111/play.txt",lines);}
}
return Check();
