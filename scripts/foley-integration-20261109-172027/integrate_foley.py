from pathlib import Path
import datetime, shutil, hashlib, json, re, warnings
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly
from math import gcd
warnings.filterwarnings('ignore',category=wavfile.WavFileWarning)
r=Path(r'E:\TT Gaming\TT-Gaming-Getting-over-with-mario')
out=r/'scripts'/('foley-integration-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));out.mkdir()
root=Path(r'E:\510款沉浸式叙事电影级氛围音效【优品视觉】')
sources=list(root.rglob('*.wav'));modified=[];manifest=[]
def edit(rel,fn):
 p=r/rel; dest=out/'backup'/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 s=p.read_text(encoding='utf-8-sig');t=fn(s);assert t!=s,rel;p.write_text(t,encoding='utf-8');modified.append(p)
# Every cue is edited from a field/foley recording: no oscillators, pitch shift or synthesized layers.
specs={
 'gate':('Opening a door.wav',.85, .42,False),
 'locked':('Lock the door.wav',.20,.34,False),
 'platform_warn':('Wood 1.wav',.20,.30,False),
 'platform_fall':('Breaking Wood.wav',.38,.46,False),
 'growth_start':('拉链.wav',.42,.30,False),
 'growth_end':('Leaves Moving.wav',.25,.24,False),
 'strength_start':('Wood 2.wav',.25,.38,False),
 'strength_end':('Leaves Moving.wav',.32,.22,False),
 'warning':('Wood 1.wav',.095,.22,False),
 'enemy_ghost':('Leaves Moving.wav',.32,.28,False),
 'spawn':('Leaves Moving.wav',.25,.22,False),
 'pause':('Lock the door.wav',.12,.22,False),
 'resume':('Wood 2.wav',.10,.20,False),
 'complete':('Applauding.wav',1.1,.35,True),
 'fall_wind':('Wind 1.wav',.85,.25,True),
}
for cue,(suffix,duration,peak,ambient) in specs.items():
 matches=[p for p in sources if p.name.endswith(suffix) and 'Sam Kolder' in str(p)];assert len(matches)==1,(suffix,matches)
 src=matches[0];rate,data=wavfile.read(src)
 if np.issubdtype(data.dtype,np.integer):data=data.astype(np.float64)/max(abs(np.iinfo(data.dtype).min),np.iinfo(data.dtype).max)
 else:data=data.astype(np.float64)
 if data.ndim>1:data=data.mean(axis=1)
 data-=np.mean(data)
 # Locate first sustained activity, avoiding an empty leading interval.
 window=max(1,rate//100);pad=(-len(data))%window
 energy=np.sqrt(np.mean(np.pad(data,(0,pad)).reshape(-1,window)**2,axis=1))
 active=np.flatnonzero(energy>max(float(energy.max())*.16,.0001))
 assert active.size,suffix
 start=max(0,int(active[0]*window)-int(rate*.008))
 if ambient:start=min(start+int(.12*rate),max(0,len(data)-int(duration*rate)))
 clip=data[start:start+int(rate*duration)].copy();assert clip.size>rate*.05
 g=gcd(rate,44100);clip=resample_poly(clip,44100//g,rate//g)
 clip*=peak/max(float(np.max(np.abs(clip))),1e-8)
 fadein=min(len(clip)//3,int(44100*(.12 if ambient else .005)))
 fadeout=min(len(clip)//3,int(44100*(.18 if ambient else .045)))
 clip[:fadein]*=np.linspace(0,1,fadein);clip[-fadeout:]*=np.linspace(1,0,fadeout)
 dest=r/'Assets/Resources/NaturalAudio'/(cue+'.wav')
 if dest.exists():raise RuntimeError('Refusing overwrite '+str(dest))
 wavfile.write(dest,44100,np.round(clip*32767).astype(np.int16));modified.append(dest)
 manifest.append({'cue':cue,'source':str(src),'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'start_seconds':start/rate,'duration_seconds':len(clip)/44100,'peak':float(np.max(np.abs(clip))),'output':str(dest.relative_to(r)),'license':'User local collection; commercial license not independently verified'})
def router(s):
 for cue in specs:
  line=f'            case "{cue}": return "{cue}";'
  pattern=rf'            case "{cue}": return "[^"]+";'
  if re.search(pattern,s):s=re.sub(pattern,line,s)
  else:s=s.replace('            default: return null;',line+'\n            default: return null;')
 return s.replace('Only physical sample assets are allowed; unmapped cues intentionally remain silent.','Only physical recordings are allowed; no electronic/synthesized cues.')
edit('Assets/Scripts/GameplayAudio.cs',router)
edit('Assets/Scripts/Winning/WinThresholdDetection.cs',lambda s:s.replace('// Completion stays visual until a suitable natural celebration recording is selected.','GameplayAudio.Play("complete", .35f);'))
edit('Assets/Scripts/Character/Lifecycle/DeathFeedback.cs',lambda s:s.replace('if (!falling) GameplayAudio.Play("death_hit", .35f);','GameplayAudio.Play(falling ? "fall_wind" : "death_hit", falling ? .25f : .35f);'))
def validator(s):
 names=['table','can','canBounce','bump']+list(specs)
 s=re.sub(r'string\[\] cues=\{.*?\};','string[] cues={'+','.join('"'+n+'"' for n in names)+'};',s)
 s=s.replace('4 physical sample clips imported','19 natural clips imported')
 return s
edit('Assets/Editor/AudioFeedbackValidation.cs',validator)
for item in manifest:
 rate,data=wavfile.read(r/item['output']);assert rate==44100 and data.ndim==1 and np.abs(data.astype(np.int32)).max()<32767
for p in (r/'Assets/Scripts').rglob('*.cs'):
 assert 'AudioClip.Create(' not in p.read_text(encoding='utf-8-sig'),p
gifts=[p for p in (r/'Assets/Scripts').glob('Gift*.cs')]
report={'created':datetime.datetime.now().isoformat(),'engine':'2022.3.62t15 / Tuanjie 1.10.3','source_manifest':manifest,'checks':{'waveform_format_peak_fades':'PASS','no_runtime_synthesizer':'PASS','engine_compile':'PENDING','in_game_listening':'NOT RUN','physical_play':'NOT RUN','device':'NOT RUN'},'sha256':{str(p.relative_to(r)):hashlib.sha256(p.read_bytes()).hexdigest() for p in modified+gifts}}
(out/'evidence.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(Path(__file__),out/'integrate_foley.py')
with (r/'ai-handoff.md').open('a',encoding='utf-8') as f:f.write('\n## Natural foley integration 2026-11-09\nUser approved local candidate integration. Added 15 cropped mono44.1k PCM16 recordings: gate/lock, wood warning/break, zipper/leaves power-ups, light mechanical ticks, ghost/spawn rustle, pause/resume, short applause, fall wind. No pitch shift or synthesized samples. Existing hammer/body gain and gameplay/visual/gift logic preserved. Source provenance, trims, hashes and backups: '+str(out)+' . Waveform format/clipping/no-synth checks PASS; engine validation pending; actual audition/physical Play/device NOT RUN. Local collection commercial license not independently verified.\n')
print(out)
