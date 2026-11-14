
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Stop Play first");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Unsaved scene edits preserved");
var sprites=new UnityEngine.Sprite[9];
for(int i=0;i<9;i++) {string p="Assets/Art/11.10/DEC_1_"+(i+1)+".png";var t=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(p);t.textureType=UnityEditor.TextureImporterType.Sprite;t.spriteImportMode=UnityEditor.SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;t.SaveAndReimport();sprites[i]=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(p);}
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var report=new System.Collections.Generic.List<string>();
try {foreach(var path in System.IO.Directory.GetFiles("Assets/Scenes","*.unity").OrderBy(p=>p)) {
 if(path.Contains("MainMenu"))continue;
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
 if(UnityEngine.Object.FindObjectOfType<CampaignLevel>()==null && scene.name!="OneOneScene")continue;
 System.IO.File.Copy(path,"scripts/dec-20261111/"+scene.name+".before-free-layout",true);
 var roots=scene.GetRootGameObjects();
 foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true))) {
 if(r.name.StartsWith("Bush")||r.name.StartsWith("Mountain")||r.name.StartsWith("Forest ")||r.name.StartsWith("Cloud")) {r.enabled=false;UnityEditor.EditorUtility.SetDirty(r);if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(r))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
 var previous=UnityEngine.GameObject.Find("DEC platform dressing");if(previous)UnityEngine.Object.DestroyImmediate(previous);
 var group=new UnityEngine.GameObject("DEC platform dressing");
 var spans=new System.Collections.Generic.List<UnityEngine.Vector3>();
 foreach(var map in UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>()) foreach(var c in map.cellBounds.allPositionsWithin) if(map.HasTile(c)&&!map.HasTile(c+UnityEngine.Vector3Int.up)) {var p=map.GetCellCenterWorld(c);var size=map.layoutGrid.cellSize;spans.Add(new UnityEngine.Vector3(p.x-size.x*.5f,p.x+size.x*.5f,p.y+size.y*.5f));}
 foreach(var r in UnityEngine.Object.FindObjectsOfType<UnityEngine.SpriteRenderer>().Where(r=>r.name.StartsWith("GroundTile"))) {var b=r.bounds; if(!UnityEngine.Physics2D.OverlapPoint(new UnityEngine.Vector2(b.center.x,b.max.y+.08f)))spans.Add(new UnityEngine.Vector3(b.min.x,b.max.x,b.max.y));}
 var merged=new System.Collections.Generic.List<UnityEngine.Vector3>();foreach(var row in spans.GroupBy(p=>UnityEngine.Mathf.RoundToInt(p.z*100))) {UnityEngine.Vector3 run=UnityEngine.Vector3.zero;bool active=false;foreach(var p in row.OrderBy(p=>p.x)){if(active&&p.x<=run.y+.03f)run.y=UnityEngine.Mathf.Max(run.y,p.y);else {if(active)merged.Add(run);run=p;active=true;}}if(active)merged.Add(run);}

 var rng=new System.Random(scene.name.Aggregate(92137,(a,c)=>unchecked(a*31+c)));int count=0;
 var placed=new System.Collections.Generic.List<UnityEngine.SpriteRenderer>();
 foreach(var span in merged.OrderBy(p=>p.x).ThenBy(p=>p.z)) {
 float length=span.y-span.x;if(length<1.2f||rng.NextDouble()<.42)continue;
 int clusters=UnityEngine.Mathf.Max(1,(int)(length/(5f+(float)rng.NextDouble()*7f)));
 for(int cluster=0;cluster<clusters;cluster++){
 float anchor=UnityEngine.Mathf.Lerp(span.x+.35f,span.y-.35f,(float)rng.NextDouble());
 int members=rng.NextDouble()<.4?1:rng.Next(2,5);float direction=rng.NextDouble()<.5?-1:1;
 for(int member=0;member<members;member++){
 int id=member==0?(rng.NextDouble()<.52?rng.Next(3,9):rng.Next(3)):rng.Next(3);
 float h=id<3?.26f+(float)rng.NextDouble()*.42f:id==3?2.7f+(float)rng.NextDouble()*.65f:id==4?2.65f+(float)rng.NextDouble()*.35f:id==5?2.25f+(float)rng.NextDouble()*.3f:id==6?1.15f+(float)rng.NextDouble()*.25f:id==7?.9f+(float)rng.NextDouble()*.35f:2.25f+(float)rng.NextDouble()*.55f;
 var sprite=sprites[id];float scale=h/sprite.bounds.size.y,w=sprite.bounds.size.x*scale;
 if(w>length-.3f)continue;
 float center=anchor+(member==0?0:direction*(.2f+(float)rng.NextDouble()*1.1f));center=UnityEngine.Mathf.Clamp(center,span.x+w*.5f+.1f,span.y-w*.5f-.1f);
 if(placed.Any(r=>r.sprite==sprite&&UnityEngine.Mathf.Abs(r.bounds.min.y-span.z)<.25f&&UnityEngine.Mathf.Abs(r.bounds.center.x-center)<5f))continue;
 if(member==0&&placed.Any(r=>UnityEngine.Mathf.Abs(r.bounds.min.y-span.z)<.25f&&UnityEngine.Mathf.Abs(r.bounds.center.x-center)<(w+r.bounds.size.x)*.6f))continue;
 var go=new UnityEngine.GameObject("DEC "+(id+1)+(member==0?" anchor":" foreground cluster"));go.transform.SetParent(group.transform);var sr=go.AddComponent<UnityEngine.SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=member==0?-12:-6+member;go.transform.localScale=UnityEngine.Vector3.one*scale;
 float sink=member==0?.025f:.03f+(float)rng.NextDouble()*.045f;
 go.transform.position=new UnityEngine.Vector3(center-sprite.bounds.center.x*scale,span.z-sprite.bounds.min.y*scale-sink,0);placed.Add(sr);count++;
 }
 }
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);report.Add(scene.name+": "+count+" decorations, "+merged.Count+" platform spans");
 }}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);System.IO.File.WriteAllLines("scripts/dec-20261111/free-layout.txt",report);}UnityEngine.Debug.Log(string.Join("\n",report));
