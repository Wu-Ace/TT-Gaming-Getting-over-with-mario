import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy.signal import fftconvolve
from scipy.ndimage import label, distance_transform_edt
root=Path(__file__).parent
scenes=json.loads((root/'geometry.json').read_text())
results=[]
STEP=.05
for s in scenes:
    terrain=[c for c in s['colliders'] if c['body'].lower()=='static' and c['name']!='Closed door seal']
    vv=np.concatenate([c['vertices'] for c in terrain]); lo=np.floor(vv.min(0)-4); hi=np.ceil(vv.max(0)+4)
    shape=np.ceil((hi-lo)/STEP).astype(int)+1
    im=Image.new('1',tuple(shape)); draw=ImageDraw.Draw(im)
    def paint(draw,c,offset):
        v=(np.asarray(c['vertices'])-offset)/STEP
        for tri in np.asarray(c['triangles']).reshape(-1,3): draw.polygon([tuple(p) for p in v[tri]],fill=1)
    for c in terrain: paint(draw,c,lo)
    solid=np.array(im,dtype=bool)
    cauldron=next(c for c in s['colliders'] if c['name']=='Cauldron')
    ref=(np.min(cauldron['vertices'],axis=0)+np.max(cauldron['vertices'],axis=0))/2
    # Symmetric kernel origin, actual upright body union; paddle deliberately separate.
    k=Image.new('1',(81,81)); kd=ImageDraw.Draw(k)
    for c in s['colliders']:
        if c['name'] in ['Cauldron','Mario','Shape']: paint(kd,c,ref-np.array([2.,2.]))
    kernel=np.array(k,dtype=float)
    blocked=fftconvolve(solid.astype(float),kernel[::-1,::-1],mode='same')>.5
    from scipy.ndimage import binary_dilation
    blocked=binary_dilation(blocked,iterations=1)
    labels,count=label(~blocked)
    centers=[]
    for p in [s['spawn']]+(s['route'] or [])+[np.mean(next(c for c in s['colliders'] if c['name']=='Closed door seal')['vertices'],axis=0).tolist()]:
        # initialSpawn is body transform, .17 above cauldron bounds center.
        q=np.rint((np.array(p)-[0,.1664]-lo)/STEP).astype(int)
        x,y=q; rad=40
        x0=max(0,x-rad); y0=max(0,y-rad); cut=labels[y0:y+rad+1,x0:x+rad+1]
        ys,xs=np.where(cut>0)
        if len(xs):
            j=np.argmin((xs+x0-x)**2+(ys+y0-y)**2); xx=xs[j]+x0; yy=ys[j]+y0
            centers.append({'target':p,'nearestFree':(lo+np.array([xx,yy])*STEP).tolist(),'distance':float(np.hypot(xx-x,yy-y)*STEP),'component':int(labels[yy,xx])})
        else: centers.append({'target':p,'component':None})
    res={'scene':s['scene'],'step':STEP,'staticColliders':len(terrain),'components':count,'anchors':centers,'bodyBounds':[.765,1.5], 'bounds':[lo.tolist(),hi.tolist()]}
    results.append(res)
    # Overview, red = body cannot fit, grey = solid.
    rgb=np.full((*solid.shape,3),245,dtype=np.uint8); rgb[blocked]=[245,185,185]; rgb[solid]=[65,70,80]
    pic=Image.fromarray(rgb[::-1]); pic.thumbnail((1800,2400)); pic.save(root/(Path(s['scene']).stem+'-rig-clearance.png'))
    np.savez_compressed(root/(Path(s['scene']).stem+'-rig-grid.npz'),solid=solid,blocked=blocked,labels=labels,lo=lo)
(root/'rig-screening.json').write_text(json.dumps(results,indent=2))
for r in results: print(r['scene'], 'components',r['components'],'anchors',[(a['target'],a['component'],round(a.get('distance',999),2)) for a in r['anchors']])

