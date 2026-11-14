import json,numpy as np
from pathlib import Path
from scipy.ndimage import distance_transform_edt
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import dijkstra
from PIL import Image,ImageDraw
r=Path(__file__).parent;prior=r.parent/'clearance-20261112';scenes=json.loads((prior/'geometry.json').read_text());reports=[]
for stem in ['Level01','Level05_Cave']:
 s=next(s for s in scenes if Path(s['scene']).stem==stem);z=np.load(prior/(stem+'-rig-grid.npz'));lo=z['lo'];solid=z['solid'][::2,::2];blocked=z['blocked'][::2,::2];h,w=solid.shape;yy,xx=np.mgrid[:h,:w];worldx=lo[0]+xx*.1;worldy=lo[1]+yy*.1
 goal=s['route'][-1] if s['route'] else np.mean(next(c for c in s['colliders'] if c['name']=='Closed door seal')['vertices'],axis=0).tolist()
 allowed=((worldx>=.8)&(worldx<=23.2)&(worldy>=-2)&(worldy<=247)) if stem=='Level05_Cave' else ((worldx>=s['spawn'][0]-1)&(worldx<=goal[0]+1)&(worldy>=-3.1)&(worldy<=23))
 free=(~blocked)&allowed; ids=np.full((h,w),-1,dtype=np.int32);ids[free]=np.arange(free.sum());cost=1+np.minimum(distance_transform_edt(~solid)*.1,10)*.1
 row=[];col=[];data=[]
 for dy,dx in [(0,1),(1,0)]:
  a=ids[:h-dy,:w-dx];b=ids[dy:,dx:];ok=(a>=0)&(b>=0);av=a[ok];bv=b[ok];weights=(cost[:h-dy,:w-dx][ok]+cost[dy:,dx:][ok])/2
  row.extend([av,bv]);col.extend([bv,av]);data.extend([weights,weights])
 graph=coo_matrix((np.concatenate(data),(np.concatenate(row),np.concatenate(col))),shape=(int(free.sum()),int(free.sum()))).tocsr(); fy,fx=np.where(free)
 def anchor(p):
  q=(np.asarray(p)-[0,.1664]-lo)/.1;j=np.argmin((fx-q[0])**2+(fy-q[1])**2);return j,float(np.hypot(fx[j]-q[0],fy[j]-q[1])*.1)
 a,da=anchor(s['spawn']);b,db=anchor(goal);dist,pred=dijkstra(graph,indices=a,return_predecessors=True);assert np.isfinite(dist[b]),stem
 seq=[];i=b
 while i!=a:seq.append(i);i=int(pred[i])
 seq.append(a);seq=np.array(seq[::-1]);points=np.c_[fx[seq],fy[seq]]*.1+lo
 out={'scene':s['scene'],'pose':'authored fixed body+paddle configuration, all translated together','spawn':s['spawn'],'goal':goal,'anchorShifts':[da,db],'pointCount':len(points),'length':(len(points)-1)*.1,'bounds': [points.min(0).tolist(),points.max(0).tolist()],'points':points.tolist()};reports.append(out)
 rgb=np.full((*solid.shape,3),245,dtype=np.uint8);rgb[blocked]=[245,185,185];rgb[solid]=[65,70,80];im=Image.fromarray(rgb[::-1]);draw=ImageDraw.Draw(im);draw.line([(int(x),int(h-1-y)) for x,y in zip(fx[seq],fy[seq])],fill=(0,110,220),width=2);im.thumbnail((2000,2600));im.save(r/(stem+'-witness.png'))
 print(stem,len(points),'points',out['bounds'],'anchor shifts',out['anchorShifts'])
(r/'full-paths.json').write_text(json.dumps(reports))
