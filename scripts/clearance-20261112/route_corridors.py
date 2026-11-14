import json,numpy as np
from pathlib import Path
from scipy.ndimage import label
root=Path(__file__).parent
report=[]
for r in json.loads((root/'screening.json').read_text()):
    name=Path(r['scene']).stem
    z=np.load(root/(name+'-grid.npz')); solid=z['solid']; blocked=z['blocked']; lo=z['lo']; h,w=solid.shape
    s=next(s for s in json.loads((root/'geometry.json').read_text()) if s['scene']==r['scene'])
    route=s['route']; out={'scene':r['scene']}
    if len(route)<3: out['corridor']='not applicable: no detailed authored route'
    else:
        yy,xx=np.mgrid[:h,:w]; xx=xx*.05+lo[0]; yy=yy*.05+lo[1]; d2=np.full((h,w),np.inf)
        for a,b in zip(route,route[1:]):
            a=np.array(a); b=np.array(b); v=b-a; t=np.clip(((xx-a[0])*v[0]+(yy-a[1])*v[1])/max(v@v,1e-9),0,1)
            d2=np.minimum(d2,(xx-a[0]-t*v[0])**2+(yy-a[1]-t*v[1])**2)
        out['radii']={}
        for radius in [2.4,4.,6.]:
            lab,n=label((~blocked)&(d2<=radius**2)); groups=[]
            for anchor in r['anchors']:
                if anchor['component']!=1: continue
                x,y=np.rint((np.array(anchor['nearestFree'])-lo)/.05).astype(int); groups.append(int(lab[y,x]))
            out['radii'][str(radius)]={'anchorComponents':sorted(set(groups)), 'connected':len(set(groups))==1 and 0 not in groups}
    report.append(out)
(root/'route-corridors.json').write_text(json.dumps(report,indent=2)); print(json.dumps(report,indent=2))
