import json
from pathlib import Path
p=Path('scripts/dec-20261111/prepare.py')
s=p.read_text();s=s.replace('int count=0;','int count=0;int lastId=-1;').replace('int id=rng.Next(9);','int id=rng.Next(9);while(id==lastId)id=rng.Next(9);').replace('count++;x+=','lastId=id;count++;x+=');p.write_text(s)
script=r'''
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play first");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("Unsaved edits preserved");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var report=new System.Collections.Generic.List<string>();
try{foreach(var path in System.IO.Directory.GetFiles("Assets/Scenes","*.unity").OrderBy(p=>p)){
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);var group=UnityEngine.GameObject.Find("DEC platform dressing");if(!group)continue;
System.IO.File.Copy(path,"scripts/dec-20261111/"+scene.name+".before-dedup",true);
var placed=new System.Collections.Generic.List<UnityEngine.SpriteRenderer>();int changed=0;
foreach(var r in group.GetComponentsInChildren<UnityEngine.SpriteRenderer>().OrderBy(r=>r.bounds.min.x).ThenBy(r=>r.bounds.min.y)){
var b=r.bounds;var nearby=placed.Where(p=>UnityEngine.Mathf.Abs(p.bounds.min.y-b.min.y)<.2f && b.min.x-p.bounds.max.x<5f).ToArray();
if(nearby.Any(p=>p.sprite==r.sprite)){
var candidates=new[]{1,2,3,4,5,6,7,8,9}.Select(i=>new{ id=i,sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/11.10/DEC_1_"+i+".png")}).Where(p=>p.sprite && !nearby.Any(n=>n.sprite==p.sprite)).ToArray();
var choice=candidates.FirstOrDefault(p=>(p.id<=3? .6f:p.id==4?3.4f:p.id==5?2.9f:p.id==6?2.5f:p.id==7?1.5f:p.id==8?1.4f:2.8f)*p.sprite.bounds.size.x/p.sprite.bounds.size.y<=b.size.x+.01f);
if(choice==null)choice=candidates.OrderBy(p=>p.sprite.bounds.size.x/p.sprite.bounds.size.y).First();
float h=choice.id<=3?.6f:choice.id==4?3.4f:choice.id==5?2.9f:choice.id==6?2.5f:choice.id==7?1.5f:choice.id==8?1.4f:2.8f;
float scale=UnityEngine.Mathf.Min(h/choice.sprite.bounds.size.y,b.size.x/choice.sprite.bounds.size.x);r.sprite=choice.sprite;r.name="DEC "+choice.id;r.transform.localScale=UnityEngine.Vector3.one*scale;r.transform.position=new UnityEngine.Vector3(b.min.x-choice.sprite.bounds.min.x*scale,b.min.y-choice.sprite.bounds.min.y*scale,r.transform.position.z);changed++;
}placed.Add(r);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);report.Add(scene.name+": replaced "+changed+" neighboring duplicates");
}}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);System.IO.File.WriteAllLines("scripts/dec-20261111/dedup.txt",report);}UnityEngine.Debug.Log(string.Join("\n",report));
'''
Path('scripts/dec-20261111/dedup.cs').write_text(script)
json.dump(dict(name='exec_editor_script',arguments=dict(script=script,summary='Remove neighboring identical DEC decorations')),open('scripts/dec-20261111/dedup.json','w'))
