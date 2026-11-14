using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
var setup=EditorSceneManager.GetSceneManagerSetup();foreach(var s in setup)if(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)throw new Exception("Unsaved scene edits");
var report=new System.Collections.Generic.List<string>();
try{foreach(var e in EditorBuildSettings.scenes.Where(s=>s.enabled&&!s.path.EndsWith("MainMenu.unity"))){File.Copy(e.path,"scripts/remove-diamonds-20261111/backup/"+Path.GetFileName(e.path),true);var scene=EditorSceneManager.OpenScene(e.path);var items=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignInteractable>(true)).Where(c=>c.kind==CampaignInteractable.Kind.Star).ToArray();foreach(var c in items){report.Add(e.path+": removed "+c.name);UnityEngine.Object.DestroyImmediate(c.gameObject);}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignInteractable>(true)).Any(c=>c.kind==CampaignInteractable.Kind.Star))throw new Exception("Remaining collectibles");}}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}File.WriteAllLines("scripts/remove-diamonds-20261111/removed.txt",report);return string.Join("\n",report);
