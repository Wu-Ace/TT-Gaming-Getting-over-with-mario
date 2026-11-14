from pathlib import Path
import json
p=Path('scripts/crush-landing-fix-20261112')
s=Path('scripts/crush-death-20261112/test-physics.csx').read_text(encoding='utf-8-sig').replace('scripts/crush-death-20261112/physics-results.json','scripts/crush-landing-fix-20261112/physics-results.json')
s=s.replace('Assert(detector.IsCrushed(),"body embedding beyond .06 kills without timer")','Assert(!detector.IsCrushed(),"single static-wall penetration does not cause fall death")')
marker='// Actual solver steps:'
s=s.replace(marker,'''// Falling velocity must not turn stationary nearby surfaces into a crusher.
var lowRoof=Box("Static landing corner",new UnityEngine.Vector2(0,1.15f),new UnityEngine.Vector2(8,1));body.velocity=UnityEngine.Vector2.down*50;UnityEngine.Physics2D.SyncTransforms();Assert(!detector.IsCrushed(),"fast fall between stationary surfaces is not crushing");body.velocity=UnityEngine.Vector2.zero;UnityEngine.Object.DestroyImmediate(lowRoof.gameObject);
var l=Box("Parallel left platform",new UnityEngine.Vector2(-.91f,0),new UnityEngine.Vector2(1,6),true);var r=Box("Parallel right platform",new UnityEngine.Vector2(.91f,0),new UnityEngine.Vector2(1,6),true);l.attachedRigidbody.velocity=new UnityEngine.Vector2(2,1);r.attachedRigidbody.velocity=new UnityEngine.Vector2(2,1);UnityEngine.Physics2D.SyncTransforms();Assert(!detector.IsCrushed(),"platforms translating together do not close their gap");UnityEngine.Object.DestroyImmediate(l.gameObject);UnityEngine.Object.DestroyImmediate(r.gameObject);
'''+marker)
(p/'test-physics.csx').write_text(s)
(p/'test-physics.json').write_text(json.dumps({'name':'exec_editor_script','arguments':{'script':s,'summary':'Regress falling false positives and retain actual moving-wall squeeze detection'}}))
# Reuse the actual compound-polygon landing checks, including faster impacts.
s=Path('scripts/crush-death-20261112/test-actual-player.csx').read_text(encoding='utf-8-sig').replace('scripts/crush-death-20261112/actual-player-results.json','scripts/crush-landing-fix-20261112/actual-player-results.json').replace('new[]{0f,-15f}','new[]{0f,-15f,-30f,-50f}')
(p/'test-actual-player.csx').write_text(s)
(p/'test-actual-player.json').write_text(json.dumps({'name':'exec_editor_script','arguments':{'script':s,'summary':'Verify actual player compound polygons survive ordinary and fast landings while moving ceiling crush remains fatal'}}))
