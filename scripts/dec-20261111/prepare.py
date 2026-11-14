import json
s=r'''
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Stop Play first");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Unsaved scene edits preserved");
var sprites=new UnityEngine.Sprite[9];
for(int i=0;i<9;i++) {string p="Assets/Art/11.10/DEC_1_"+(i+1)+".png";var t=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(p);t.textureType=UnityEditor.TextureImporterType.Sprite;t.spriteImportMode=UnityEditor.SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;t.SaveAndReimport();sprites[i]=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(p);}
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var report=new System.Collections.Generic.List<string>();
try {foreach(var path in System.IO.Directory.GetFiles("Assets/Scenes","*.unity").OrderBy(p=>p)) {
 if(path.Contains("MainMenu"))continue;
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
 if(UnityEngine.Object.FindObjectOfType<CampaignLevel>()==null && scene.name!="OneOneScene")continue;
 System.IO.File.Copy(path,"scripts/dec-20261111/"+scene.name+".backup",true);
 var roots=scene.GetRootGameObjects();
 foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))) {
 if(r.name.StartsWith("Bush")||r.name.StartsWith("Mountain")||r.name.StartsWith("Forest ")||r.name.StartsWith("Cloud")) {r.enabled=false;UnityEditor.EditorUtility.SetDirty(r);if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(r))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
 var previous=UnityEngine.GameObject.Find("DEC platform dressing");if(previous)UnityEngine.Object.DestroyImmediate(previous);
 var group=new UnityEngine.GameObject("DEC platform dressing");
 var spans=new System.Collections.Generic.List<UnityEngine.Vector3>();
 foreach(var map in UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>()) foreach(var c in map.cellBounds.allPositionsWithin) if(map.HasTile(c)&&!map.HasTile(c+UnityEngine.Vector3Int.up)) {var p=map.GetCellCenterWorld(c);var size=map.layoutGrid.cellSize;spans.Add(new UnityEngine.Vector3(p.x-size.x*.5f,p.x+size.x*.5f,p.y+size.y*.5f));}
 foreach(var r in UnityEngine.Object.FindObjectsOfType<UnityEngine.SpriteRenderer>().Where(r=>r.name.StartsWith("GroundTile"))) {var b=r.bounds; if(!UnityEngine.Physics2D.OverlapPoint(new UnityEngine.Vector2(b.center.x,b.max.y+.08f)))spans.Add(new UnityEngine.Vector3(b.min.x,b.max.x,b.max.y));}
 var merged=new System.Collections.Generic.List<UnityEngine.Vector3>();foreach(var row in spans.GroupBy(p=>UnityEngine.Mathf.RoundToInt(p.z*100))) {UnityEngine.Vector3 run=UnityEngine.Vector3.zero;bool active=false;foreach(var p in row.OrderBy(p=>p.x)){if(active&&p.x<=run.y+.03f)run.y=UnityEngine.Mathf.Max(run.y,p.y);else {if(active)merged.Add(run);run=p;active=true;}}if(active)merged.Add(run);}
 var rng=new System.Random(scene.name.Aggregate(7123,(a,c)=>unchecked(a*31+c)));int count=0;int lastId=-1;
 foreach(var span in merged.OrderBy(p=>p.x).ThenBy(p=>p.z)) {if(rng.NextDouble()<.30)continue;float x=span.x+.2f+(float)rng.NextDouble()*.8f;while(x<span.y-.3f) {int id=rng.Next(9);while(id==lastId)id=rng.Next(9);float h=id<3?.35f+(float)rng.NextDouble()*.5f:id==3?3.2f+(float)rng.NextDouble()*.6f:id==4?2.9f:id==5?2.5f:id==6?1.5f:id==7?1.4f:2.8f;var sprite=sprites[id];float scale=h/sprite.bounds.size.y,w=sprite.bounds.size.x*scale;if(x+w>span.y-.15f){x+=.6f;continue;}
 var go=new UnityEngine.GameObject("DEC "+(id+1));go.transform.SetParent(group.transform);var sr=go.AddComponent<UnityEngine.SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=-8;go.transform.localScale=UnityEngine.Vector3.one*scale;go.transform.position=new UnityEngine.Vector3(x-sprite.bounds.min.x*scale,span.z-sprite.bounds.min.y*scale-.025f,0);lastId=id;count++;x+=w+.15f+(float)rng.NextDouble()*(rng.NextDouble()<.5?1f:6f);
 }}UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);report.Add(scene.name+": "+count+" decorations, "+merged.Count+" platform spans");
 }}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);System.IO.File.WriteAllLines("scripts/dec-20261111/applied.txt",report);}UnityEngine.Debug.Log(string.Join("\n",report));
'''
open('scripts/dec-20261111/apply.cs','w').write(s)
json.dump(dict(name='exec_editor_script',arguments=dict(script=s,summary='Replace old scenery with varied DEC platform decorations')),open('scripts/dec-20261111/apply.json','w'))
