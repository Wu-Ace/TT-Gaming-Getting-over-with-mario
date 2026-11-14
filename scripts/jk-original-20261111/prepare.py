from pathlib import Path
import json
from PIL import Image
p=Path('scripts/jk-original-20261111');d=json.loads((p/'plan.json').read_text(encoding='utf-8-sig'));old=json.loads(Path('scripts/jk-tilemap-20261111/plan.json').read_text());a={(c['x'],c['y']):c['kind'] for c in d['cells']};b={(c['x'],c['y']):c['kind'] for c in old['cells']}
report={'sourceCells':len(a),'sourceSlopes':sum(v>0 for v in a.values()),'removedUnionCells':len(old['extra']),'correctedSlopeShapes':sum(v!=b.get(k) for k,v in a.items()),'kindCounts':{str(k):sum(v==k for v in a.values()) for k in range(5)}}
(p/'extraction-check.json').write_text(json.dumps(report,indent=2));print(report)
im=Image.new('RGB',(60,630),(25,35,45))
for (x,y),k in a.items():im.putpixel((x,629-y),(210,150,70) if k else (170,180,190))
im.resize((360,3780),Image.Resampling.NEAREST).save(p/'original-grid.png')
