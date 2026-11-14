from pathlib import Path
import json
exec(Path('scripts/dec-20261111/prepare.py').read_text().split("open('scripts/dec-20261111/apply.cs'")[0])
s=s.replace('".backup",true','".before-free-layout",true')
a=s.index(' var rng=');b=s.index('UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene)',a)
s=s[:a]+r'''
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
 '''+s[b:]
s=s.replace('scripts/dec-20261111/applied.txt','scripts/dec-20261111/free-layout.txt')
Path('scripts/dec-20261111/free-layout.cs').write_text(s)
json.dump(dict(name='exec_editor_script',arguments=dict(script=s,summary='Arrange freeform DEC clusters with varied scale and depth')),open('scripts/dec-20261111/free-layout.json','w'))
