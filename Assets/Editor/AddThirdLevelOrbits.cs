using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
[InitializeOnLoad] public static class AddThirdLevelOrbits {
 static AddThirdLevelOrbits(){EditorApplication.delayCall+=Run;}
 static void Run(){if(!File.Exists("scripts/add-orbits.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;
 if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)return;
 File.Delete("scripts/add-orbits.request");
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level03_Beach.unity");
 var level=Object.FindObjectOfType<CampaignLevel>();var parent=level.transform.Find("Interactions");
 foreach(var old in parent.GetComponentsInChildren<OrbitRidePlatform>())Object.DestroyImmediate(old.gameObject);
 foreach(var center in new[]{new Vector2(63.2f,-3.6f),new Vector2(9.6f,6.8f)})
 for(int i=0;i<4;i++){
  var g=new GameObject("Orbit platform "+i);g.layer=6;g.transform.SetParent(parent,false);
  var angle=i*Mathf.PI*.5f;g.transform.position=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*1.2f;
  var box=g.AddComponent<BoxCollider2D>();box.size=new Vector2(1.6f,.26f);
  var rb=g.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.useFullKinematicContacts=true;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
  var orbit=g.AddComponent<OrbitRidePlatform>();orbit.center=center;orbit.phase=i*.25f;
  for(int j=0;j<2;j++){var tile=new GameObject("Log tread");tile.transform.SetParent(g.transform,false);tile.transform.localPosition=new Vector3((j-.5f)*.8f,0,0);var sr=tile.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Campaign/donut-beach.png");sr.sortingOrder=12;}
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("scripts/orbit-build.txt","Added two groups of four orbiting horizontal platforms; Play and contact validation pending.");
 }
}
