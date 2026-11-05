"""Reference strip geometry, adapted to .8m hammer footholds. No source art resized."""
import json, shutil
from pathlib import Path
b=Path('art-backup/pre-level02-reference-20261105');b.mkdir(parents=True,exist_ok=True)
for f in ['scripts/level02-horizontal.json','Assets/Editor/Level02Designed.cs','Assets/Scenes/Level02_Jungle.unity']:
 p=b/Path(f).name
 if not p.exists(): shutil.copy2(f,p)
cells=set()
def rect(a,z,h):
 for x in range(a,z):
  for y in range(-8,h): cells.add((x,y))
# Start, low ledge, double-height plateau, isolated pillars, then final ground.
for a,z,h in [(-8,16,0),(18,22,1),(24,32,4),(27,31,8),(32,35,1),(36,41,5),(42,49,9),(52,56,0),(61,66,0),(62,65,8),(67,72,0),(73,76,4),(79,85,7),(102,106,2),(108,116,6),(117,120,0),(120,124,4),(126,130,4),(134,184,0)]: rect(a,z,h)
# Compact terminal staircase preserves the reference silhouette.
for i in range(6): rect(144+i*2,156,i+1)
v=lambda x,y:dict(x=x,y=y)
p=dict(number=2,reference='User supplied SMB 1-3 strip; hammer-adapted heights and gaps',title='Jungle moving pillar crossing',cells=[v(x,y) for x,y in sorted(cells)],stars=[v(36.4,4.65),v(65.6,3.05),v(90,3.85)],checkpoints=[v(48.9,-3.6),v(87.6,1.2)],exit=v(140,-3.6),route=[v(2,-2.6),v(36,4.6),v(65,3),v(90,3.8),v(140,-2.6)],authoredLength=150)
Path('scripts/level02-horizontal.json').write_text(json.dumps(p,indent=2))
print(len(cells),'native cells; 4 moving platforms')
