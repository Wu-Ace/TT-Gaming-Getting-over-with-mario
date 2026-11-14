import json,pathlib,numpy as np
p=pathlib.Path('scripts/clearance-extended-20261112');paths=json.loads((p/'full-paths.json').read_text())
for path in paths:
 a=np.array(path['points']);d=np.diff(a,axis=0);turn=np.any(np.abs(d[1:]-d[:-1])>.001,axis=1);a=a[np.r_[True,turn,True]];path['points']=a.tolist();path['segmentCount']=len(a)-1
(p/'compressed-paths.json').write_text(json.dumps(paths));print([(x['scene'],x['segmentCount']) for x in paths])
s=(p/'verify-paths.csx').read_text(encoding='utf-8-sig');s=s.replace('System.Collections.IEnumerator Check(){','').replace('return Check();','');s=s.rsplit('\n}',1)[0];s=s.replace('var originalPath=original.path;','var originalPath="Assets/Scenes/Level01.unity";').replace('full-paths.json','compressed-paths.json').replace('full-path-results.json','sweep-results.json');s=s.replace('var probes=new System.Collections.Generic.List<UnityEngine.PolygonCollider2D>();','var probeBody=go.AddComponent<UnityEngine.Rigidbody2D>();probeBody.bodyType=UnityEngine.RigidbodyType2D.Kinematic;var probes=new System.Collections.Generic.List<UnityEngine.PolygonCollider2D>();')
a=s.index(' var points=');b=s.index(' reports.Add(',a)
s=s[:a]+'''
 var points=path["points"];float minimum=100;int samples=0;var failures=new System.Collections.Generic.List<object>();var hits=new UnityEngine.RaycastHit2D[64];var filter=new UnityEngine.ContactFilter2D();filter.SetLayerMask(1<<6);filter.useTriggers=false;
 for(int i=1;i<points.Count();i++){
 var a=new UnityEngine.Vector2((float)points[i-1][0],(float)points[i-1][1]);var b=new UnityEngine.Vector2((float)points[i][0],(float)points[i][1]);var delta=b-a;go.transform.position=a;UnityEngine.Physics2D.SyncTransforms();
 foreach(var pc in probes){int count=pc.Cast(delta.normalized,filter,hits,delta.magnitude,true);for(int k=0;k<count;k++)if(terrain.Contains(hits[k].collider)&&hits[k].distance<delta.magnitude-.001f)failures.Add(new{segment=i,probe=pc.GetInstanceID(),distance=hits[k].distance,obstacle=hits[k].collider.name});samples++;}
 foreach(var p in new[]{a,b}){go.transform.position=p;UnityEngine.Physics2D.SyncTransforms();foreach(var pc in probes){var region=pc.bounds;region.Expand(.3f);foreach(var c in terrain)if(region.Intersects(c.bounds)){var d=pc.Distance(c);if(d.isValid)minimum=UnityEngine.Mathf.Min(minimum,d.distance);}}}
 }
''' +s[b:]
(p/'verify-sweeps.csx').write_text(s);(p/'verify-sweeps.json').write_text(json.dumps({'name':'exec_editor_script','arguments':{'script':s,'summary':'Verify continuous collider sweeps along compressed full-rig paths and restore Level01','timeoutSeconds':300}}))
