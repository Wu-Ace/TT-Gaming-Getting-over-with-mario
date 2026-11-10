from pathlib import Path
import json,shutil,hashlib,datetime,re
r=Path(r'E:\TT Gaming\TT-Gaming-Getting-over-with-mario');out=r/'scripts/impact-grass-20261109';out.mkdir(exist_ok=True)
assert not (out/'evidence.json').exists(),'Already applied'
changed=[]
def write(rel,s):
 p=r/rel
 if p.exists():
  b=out/'backup'/rel;b.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,b)
 p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8');changed.append(p)
def edit(rel,fn):write(rel,fn((r/rel).read_text(encoding='utf-8-sig')))
src=r/'Assets/Sound/kenney_impact-sounds (1)';assert 'commercial' in (src/'License.txt').read_text()
names=[f'footstep_grass_{i:03d}' for i in range(5)]+['impactWood_light_000','impactWood_medium_000','impactPlank_medium_000','impactMetal_light_000','impactGlass_light_000','impactGlass_medium_000','impactGeneric_light_000','impactSoft_medium_000']
dest=r/'Assets/Resources/NaturalAudio/KenneyImpact';dest.mkdir(parents=True,exist_ok=True)
for n in names:
 p=dest/(n+'.ogg');assert not p.exists();shutil.copy2(src/'Audio'/p.name,p);changed.append(p)
shutil.copy2(src/'License.txt',dest/'License.txt')
routes={'star':'impactGlass_light_000','stars_complete':'impactGlass_medium_000','checkpoint':'impactWood_medium_000','key':'impactMetal_light_000','switch':'impactWood_light_000','platform_warn':'impactWood_light_000','platform_fall':'impactPlank_medium_000','enemy_bee':'impactSoft_medium_000','enemy_plant':'impactSoft_medium_000','enemy_hit':'impactSoft_medium_000','death_hit':'impactSoft_medium_000','coin':'impactMetal_light_000','ui_click':'impactWood_light_000','ui_back':'impactGeneric_light_000','warning':'impactWood_light_000','pause':'impactWood_light_000','resume':'impactGeneric_light_000'}
def router(s):
 for cue,n in routes.items():s=re.sub(rf'case "{cue}": return "[^"]+";',f'case "{cue}": return "KenneyImpact/{n}";',s)
 s=s.replace('case "hammer_hit": return "table";','case "hammer_hit": return "KenneyImpact/footstep_grass_000";').replace('case "body_hit": return "canBounce";','case "body_hit": return "KenneyImpact/footstep_grass_000";')
 s=s.replace('    int next;','    int next;\n    int lastGrass = -1;')
 s=s.replace('        if(!audio.clips.TryGetValue(cue,out var clip)) { clip=Resources.Load<AudioClip>("NaturalAudio/"+physical); audio.clips[cue]=clip; }','''        if(cue == "hammer_hit" || cue == "body_hit")
        {
            int variant = Random.Range(0, audio.lastGrass < 0 ? 5 : 4);
            if(audio.lastGrass >= 0 && variant >= audio.lastGrass) variant++;
            audio.lastGrass = variant;
            physical = "KenneyImpact/footstep_grass_" + variant.ToString("000");
        }
        if(!audio.clips.TryGetValue(physical,out var clip)) { clip=Resources.Load<AudioClip>("NaturalAudio/"+physical); audio.clips[physical]=clip; }
        audio.LastClipName = clip == null ? null : clip.name;
        audio.PlayCount++;''')
 s=s.replace('    public static float Gain', '    public string LastClipName { get; private set; }\n    public int PlayCount { get; private set; }\n    public static float Gain')
 return s
edit('Assets/Scripts/GameplayAudio.cs',router)
write('Assets/Scripts/Character/ContactSoundEffect.cs','''using Character.Lifecycle;
using UnityEngine;

namespace Character
{
    // Collision approach speed, rather than sliding speed, controls impact feedback.
    public class ContactSoundEffect : MonoBehaviour
    {
        public const float HammerThreshold = 2.2f;
        public const float BodyThreshold = 2.8f;
        Death lifecycle;
        bool body;
        float nextImpact;
        void Awake()
        {
            lifecycle = FindObjectOfType<Death>();
            body = GetComponent<Death>() != null;
            nextImpact = Time.unscaledTime + .25f;
        }
        public static float ImpactGain(bool body, float normalSpeed)
        {
            float threshold = body ? BodyThreshold : HammerThreshold;
            if(float.IsNaN(normalSpeed) || float.IsInfinity(normalSpeed) || normalSpeed < threshold) return 0;
            return Mathf.Lerp(body ? .025f : .07f, body ? .13f : .32f,
                Mathf.InverseLerp(threshold, body ? 12f : 9f, normalSpeed));
        }
        public bool TryPlayImpact(float normalSpeed)
        {
            if((lifecycle != null && lifecycle.Dead) || Time.unscaledTime < nextImpact) return false;
            float gain = ImpactGain(body, normalSpeed);
            if(gain <= 0) return false;
            GameplayAudio.Play(body ? "body_hit" : "hammer_hit", gain);
            nextImpact = Time.unscaledTime + (body ? .45f : .16f);
            return true;
        }
        void OnCollisionEnter2D(Collision2D collision)
        {
            if(collision.gameObject.layer != LayerMask.NameToLayer("Collider")) return;
            float speed = 0;
            foreach(var contact in collision.contacts)
                speed = Mathf.Max(speed, Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal)));
            TryPlayImpact(speed);
        }
    }
}
''')
write('Assets/Editor/AudioFeedbackValidation.cs','''using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
public static class AudioFeedbackValidation
{
 public static void Run() {
  var route=typeof(GameplayAudio).GetMethod("NaturalClip",BindingFlags.Static|BindingFlags.NonPublic);
  string[] cues={"hammer_hit","body_hit","star","stars_complete","checkpoint","key","switch","locked","platform_warn","platform_fall","enemy_bee","enemy_plant","enemy_hit","death_hit","coin","ui_click","ui_back","gate","growth_start","growth_end","strength_start","strength_end","warning","enemy_ghost","spawn","pause","resume","complete","fall_wind"};
  foreach(var cue in cues) {var path=(string)route.Invoke(null,new object[]{cue});var clip=Resources.Load<AudioClip>("NaturalAudio/"+path);if(clip==null || clip.length<=0)throw new Exception("Missing cue: "+cue);}
  for(int i=0;i<5;i++)if(Resources.Load<AudioClip>("NaturalAudio/KenneyImpact/footstep_grass_"+i.ToString("000"))==null)throw new Exception("Missing grass variant");
  if(Character.ContactSoundEffect.ImpactGain(false,2.19f)!=0 || Character.ContactSoundEffect.ImpactGain(true,2.79f)!=0)throw new Exception("Light collision must be silent");
  if(Character.ContactSoundEffect.ImpactGain(false,4f)<=0 || Character.ContactSoundEffect.ImpactGain(true,5f)<=0)throw new Exception("Heavy collision must play");
  Debug.Log("IMPACT_GRASS_STATIC_PASS: 29 cue routes,5 grass variants,light-hit thresholds.");
 }
}
''')
report={'time':datetime.datetime.now().isoformat(),'source':str(src),'license':'CC0, included License.txt explicitly permits commercial projects','grass_variants':names[:5],'other_replacements':routes,'thresholds':{'hammer':2.2,'body':2.8},'checks':{'compile':'PENDING','runtime':'PENDING','subjective_listening':'NOT RUN'},'sha256':{str(p.relative_to(r)):hashlib.sha256(p.read_bytes()).hexdigest() for p in changed}}
(out/'evidence.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(__file__,out/'grass_impacts.py')
with (r/'ai-handoff.md').open('a',encoding='utf-8') as f:f.write('\n## Grass impacts and CC0 cues 2026-11-09\nUser selected Kenney Impact Sounds footstep_grass_000..004 for both body landing and hammer. Original pitch, randomized without immediate repeats; hammer normal-speed threshold2.2,body2.8; quiet gain retained. Light hits/slides silent. Other short cues switched to CC0 wood/metal/glass/soft recordings. Remaining gate/growth/ghost/spawn/complete/wind from prior local library unchanged, still not commercially cleared. No gift/scene/physics edits. License copy and evidence '+str(out)+'; compile/runtime pending.\n')
print(out)
