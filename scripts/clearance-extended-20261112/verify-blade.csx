System.Collections.IEnumerator Check(){
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(original.isDirty)throw new System.Exception("Preserving dirty scene");var originalPath=original.path;var reports=new System.Collections.Generic.List<object>();
try{var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level04_Town.unity");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Collider2D>(true)).ToArray();var terrain=all.Where(c=>c.enabled&&!c.isTrigger&&c.name!="Closed door seal"&&(c.attachedRigidbody==null||c.attachedRigidbody.bodyType==UnityEngine.RigidbodyType2D.Static)).ToArray();
var body=all.First(c=>c.name=="Cauldron");var origin=body.bounds.center;var hinge=body.GetComponent<UnityEngine.HingeJoint2D>();var pivot=body.transform.TransformPoint(hinge.anchor);var pivotOffset=pivot-origin;
var source=all.OfType<UnityEngine.PolygonCollider2D>().First(c=>c.name=="Shape");var go=new UnityEngine.GameObject("Temporary articulated blade probe");var blade=go.AddComponent<UnityEngine.PolygonCollider2D>();blade.pathCount=source.pathCount;for(int j=0;j<source.pathCount;j++)blade.SetPath(j,source.GetPath(j).Select(v=>(UnityEngine.Vector2)(source.transform.TransformPoint(v+source.offset)-pivot)).ToArray());
try{
var paths=Codely.Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("scripts/clearance-20261112/local-witness-paths.json"));
foreach(var path in paths){float best=-100;int bestAngle=-1;var angleResults=new System.Collections.Generic.List<object>();
for(int angle=0;angle<360;angle+=5){float min=100;go.transform.rotation=UnityEngine.Quaternion.Euler(0,0,angle);
foreach(var point in path){go.transform.position=new UnityEngine.Vector3((float)point[0],(float)point[1])+pivotOffset;UnityEngine.Physics2D.SyncTransforms();var region=blade.bounds;region.Expand(.3f);foreach(var c in terrain)if(region.Intersects(c.bounds)){var d=blade.Distance(c);if(d.isValid)min=UnityEngine.Mathf.Min(min,d.distance);}if(min<.005f)break;}
angleResults.Add(new{angle=angle,minDistance=min});if(min>best){best=min;bestAngle=angle;}if(angle%30==0)yield return null;
}
float fine=100;int samples=0;
if(best>.015f){go.transform.rotation=UnityEngine.Quaternion.Euler(0,0,bestAngle);for(int i=1;i<path.Count();i++){var a=new UnityEngine.Vector2((float)path[i-1][0],(float)path[i-1][1]);var b=new UnityEngine.Vector2((float)path[i][0],(float)path[i][1]);for(int j=0;j<=4;j++){go.transform.position=(UnityEngine.Vector3)UnityEngine.Vector2.Lerp(a,b,j/4f)+pivotOffset;UnityEngine.Physics2D.SyncTransforms();var region=blade.bounds;region.Expand(.3f);foreach(var c in terrain)if(region.Intersects(c.bounds)){var d=blade.Distance(c);if(d.isValid)fine=UnityEngine.Mathf.Min(fine,d.distance);}samples++;}}}
reports.Add(new{bestAngle=bestAngle,coarseMinimum=best,fineMinimum=fine,fineSamples=samples,angleResults=angleResults});System.IO.File.WriteAllText("scripts/clearance-extended-20261112/fourth-blade-results.json",Codely.Newtonsoft.Json.JsonConvert.SerializeObject(reports));UnityEngine.Debug.Log("Blade best rotation="+bestAngle+" min="+fine+" samples="+samples);
}
}finally{UnityEngine.Object.DestroyImmediate(go);}
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalPath);}
}
return Check();
