using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public sealed class PlayTerrainSaveWindow : EditorWindow {
 [Serializable] sealed class Entry {public string id,path;public Vector3 position,scale;public Quaternion rotation;}
 [Serializable] sealed class Snapshot {public string scene;public List<Entry> entries=new List<Entry>();}
 const string Pending="Library/PlayTerrainSave.pending.json";
 const string ArmedKey="PlayTerrainSave.Armed";
 const string StatusKey="PlayTerrainSave.Status";
 static PlayTerrainSaveWindow(){EditorApplication.playModeStateChanged+=Changed;}
 [MenuItem("Tools/Levels/保存 Play 中的地图调整")]
 public static void Open(){GetWindow<PlayTerrainSaveWindow>("地图调整保存");}
 void OnGUI(){
  EditorGUILayout.HelpBox("在 Play 中微调地图后点击记录。退出 Play 时，会把位置、旋转和缩放应用回原场景并保存。支持现有地形块及 Interactions 下的钻石、检查点和出口。",MessageType.Info);
  using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying)) if(GUILayout.Button("开启本次 Play 地图保存",GUILayout.Height(36))) Capture();
  EditorGUILayout.HelpBox(SessionState.GetString(StatusKey,"尚未记录调整。"),MessageType.None);
  if(File.Exists(Pending))EditorGUILayout.HelpBox("已有待保存记录。已开启本次保存；停止 Play 时自动记录最终位置，无需重复点击。新增或删除对象、碰撞形状和组件参数不在保存范围。",MessageType.Warning);
 }
 static void SetStatus(string value){SessionState.SetString(StatusKey,value);foreach(var window in Resources.FindObjectsOfTypeAll<PlayTerrainSaveWindow>())window.Repaint();Debug.Log(value);}
 static void Capture(){
  SessionState.SetBool(ArmedKey,true);
  var scene=SceneManager.GetActiveScene();var level=UnityEngine.Object.FindObjectOfType<CampaignLevel>();
  if(level==null||level.gameObject.scene!=scene||string.IsNullOrEmpty(scene.path)){SetStatus("记录失败：请打开并运行已保存的关卡场景。");return;}
  var snapshot=new Snapshot{scene=scene.path};
  foreach(string section in new[]{"Terrain","Interactions"}){
   var root=level.transform.Find(section);if(root==null)continue;
   foreach(var t in root.GetComponentsInChildren<Transform>(true)){
    // GlobalObjectId resolves to the serialized scene object after Play ends.
    var id=GlobalObjectId.GetGlobalObjectIdSlow(t);if(id.targetObjectId==0)continue;
    string path=section;var chain=new List<string>();for(var n=t;n!=root;n=n.parent)chain.Add(n.name);chain.Reverse();if(chain.Count>0)path+="/"+string.Join("/",chain.ToArray());
    snapshot.entries.Add(new Entry{id=id.ToString(),path=path,position=t.localPosition,rotation=t.localRotation,scale=t.localScale});
   }
  }
  if(snapshot.entries.Count==0){SetStatus("没有找到可保存的原场景地图对象。");return;}
  File.WriteAllText(Pending,JsonUtility.ToJson(snapshot,true));SetStatus("已开启保存，当前记录 "+snapshot.entries.Count+" 个对象；停止 Play 时抓取最终调整。");
 }
 static void Changed(PlayModeStateChange state){
  if(state==PlayModeStateChange.EnteredPlayMode)SessionState.SetBool(ArmedKey,false);
  if(state==PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(ArmedKey,false))Capture();
  if(state==PlayModeStateChange.EnteredEditMode && File.Exists(Pending))EditorApplication.delayCall+=Apply;
 }
 static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Pending))return;
  var snapshot=JsonUtility.FromJson<Snapshot>(File.ReadAllText(Pending));var scene=SceneManager.GetSceneByPath(snapshot.scene);
  if(!scene.IsValid()||!scene.isLoaded){SetStatus("应用暂停：原关卡场景未加载，记录保留在 "+Pending);return;}
  // Resolve everything before changing anything: failed mappings leave the snapshot intact.
  var targets=new List<Transform>();
  foreach(var item in snapshot.entries){
   if(!GlobalObjectId.TryParse(item.id,out var id)){SetStatus("应用失败：对象标识无效，记录已保留。");return;}
   Transform t=null;
   foreach(var rootObject in scene.GetRootGameObjects()){
    var level=rootObject.GetComponentInChildren<CampaignLevel>(true);if(level!=null && !string.IsNullOrEmpty(item.path)){t=level.transform.Find(item.path);if(t!=null)break;}
   }
   if(t==null)t=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Transform;
   if(t==null||t.gameObject.scene!=scene){SetStatus("应用失败：地图对象已删除或替换，记录已保留。");return;}targets.Add(t);
  }
  string dir="art-backup/play-terrain-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(dir);
  File.Copy(snapshot.scene,Path.Combine(dir,Path.GetFileName(snapshot.scene)));
  File.Copy(Pending,Path.Combine(dir,"play-adjustments.json"));
  // Keep a copy of existing unsaved edit-mode changes as well.
  EditorSceneManager.SaveScene(scene,Path.Combine(dir,"loaded-scene.unity"),true);
  Undo.RecordObjects(targets.ToArray(),"Apply Play terrain adjustments");
  for(int i=0;i<targets.Count;i++){var t=targets[i];var item=snapshot.entries[i];t.localPosition=item.position;t.localRotation=item.rotation;t.localScale=item.scale;EditorUtility.SetDirty(t);}
  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene)){SetStatus("场景保存失败；调整仍在编辑场景中，记录已保留。");return;}
  SessionState.SetBool(ArmedKey,false);File.Move(Pending,Path.Combine(dir,"applied.json"));SetStatus("已保存地图调整。可 Ctrl+Z 撤销；修改前备份："+dir);
 }
}
