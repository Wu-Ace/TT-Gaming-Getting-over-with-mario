using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad] public static class ThirdOrbitCheck {
 static OrbitRidePlatform[] rigs;static Vector2[] start;static double due;static bool active;
 static ThirdOrbitCheck(){EditorApplication.delayCall+=()=>{if(File.Exists("scripts/third-orbit-check.request")&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("scripts/third-orbit-check.request");SessionState.SetBool("ThirdOrbitCheck",true);EditorApplication.EnterPlaymode();}};EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ThirdOrbitCheck",false)){SessionState.SetBool("ThirdOrbitCheck",false);GameMenuUI.Testing=true;GameMenuUI.PreferencePrefix="Goombas.Campaign.TEST.Orbit."+Guid.NewGuid()+".";rigs=UnityEngine.Object.FindObjectsOfType<OrbitRidePlatform>().Where(o=>o.center.x>45).OrderBy(o=>o.center.x).ThenBy(o=>o.phase).ToArray();start=rigs.Select(o=>(Vector2)o.transform.position).ToArray();due=EditorApplication.timeSinceStartup+1;active=true;EditorApplication.update+=Tick;}};}
 static void Tick(){if(!active||EditorApplication.timeSinceStartup<due)return;try{
 if(rigs.Length!=8)throw new Exception("Expected eight red-box platforms");
 foreach(var group in rigs.GroupBy(o=>o.center)){if(group.Count()!=4)throw new Exception("Expected four per center");}
 for(int i=0;i<rigs.Length;i++){var o=rigs[i];if(Vector2.Distance(o.transform.position,start[i])<.5f)throw new Exception("Platform did not orbit");if(Mathf.Abs(Vector2.Distance(o.transform.position,o.center)-1.6f)>.04f)throw new Exception("Wrong radius");if(Quaternion.Angle(o.transform.rotation,Quaternion.identity)>.01f)throw new Exception("Tread rotated");if(o.GetComponent<BoxCollider2D>().size!=Vector2.one*.8f)throw new Exception("Wrong collider");}
 File.WriteAllText("scripts/third-orbit-check.txt","UTC "+DateTime.UtcNow.ToString("O")+"\nPASS actual Play: two groups x four, each moves >.5m in 1s on 1.6m circle, horizontal square treads, .8m colliders. Passenger carrying and full traversal NOT RUN.");
 }catch(Exception e){File.WriteAllText("scripts/third-orbit-check.txt","FAIL "+e);}finally{active=false;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}}
}
// Confirm actual two four-platform Ferris-wheel motion.
