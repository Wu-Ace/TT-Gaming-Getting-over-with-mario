import json,numpy as np
from scipy.ndimage import label
from pathlib import Path
p=Path(__file__).parent;results=[]
for n,patch in enumerate(json.loads((p/'unity-local-grids.json').read_text())):
 samples=patch['samples']; xs=sorted(set(s['x'] for s in samples));ys=sorted(set(s['y'] for s in samples));a=np.array([s['distance'] for s in samples]).reshape(len(ys),len(xs)); item={'patch':n,'bounds':patch['bounds'],'thresholds':{}}
 for threshold in [0,.005,.01,.015,.02]:
  lab,num=label(a>threshold);edges={name:set(int(x) for x in edge if x) for name,edge in [('left',lab[:,0]),('right',lab[:,-1]),('bottom',lab[0,:]),('top',lab[-1,:])]}; crossings={f'{k}-{j}':sorted(v&edges[j]) for k,v in edges.items() for j in edges if k<j}; item['thresholds'][str(threshold)]={'crossings':crossings,'freeSamples':int((a>threshold).sum())}
 results.append(item)
(p/'unity-local-connectivity.json').write_text(json.dumps(results,indent=2));print(json.dumps(results,indent=2))
