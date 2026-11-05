using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
[InitializeOnLoad] public static class CampaignProbe {
 static CampaignProbe(){EditorApplication.delayCall+=Run;}
 static void Run(){if(!File.Exists("scripts/campaign-probe.request"))return; File.Delete("scripts/campaign-probe.request");var s="";foreach(var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())s+=$"ROOT {r.name} {r.transform.position}\n";foreach(var c in Object.FindObjectsOfType<Collider2D>())if(c.transform.IsChildOf(GameObject.Find("Level").transform))s+=$"COL {c.name} {c.GetType().Name} {c.bounds.min} {c.bounds.max}\n";foreach(var r in Object.FindObjectsOfType<Rigidbody2D>())s+=$"BODY {r.name} {r.position} {r.transform.lossyScale}\n";File.WriteAllText("scripts/level-evidence/probe.txt",s);}
}
