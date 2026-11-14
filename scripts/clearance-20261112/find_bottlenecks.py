exec((__import__('pathlib').Path(__file__).parent/'plot_fourth.py').read_text().split('fig,ax=')[0])
from scipy.ndimage import distance_transform_edt
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import dijkstra
route=np.array(s['route']); yy,xx=np.mgrid[:h,:w];xx=xx*.05+lo[0];yy=yy*.05+lo[1];d2=np.full((h,w),np.inf)
for a,b in zip(route,route[1:]):
 v=b-a;t=np.clip(((xx-a[0])*v[0]+(yy-a[1])*v[1])/max(v@v,1e-9),0,1);d2=np.minimum(d2,(xx-a[0]-t*v[0])**2+(yy-a[1]-t*v[1])**2)
# 10 cm path search, evaluated against 5 cm collision raster.
free=(~solid & (d2<2.4**2))[::2,::2]; bk=block[::2,::2]; hh,ww=free.shape; idx=np.arange(hh*ww).reshape(hh,ww); costs=1+bk*100+distance_transform_edt(bk)*25
rr=[];cc=[];dd=[]
for dy,dx in [(0,1),(1,0)]:
 a=idx[:hh-dy,:ww-dx]; b=idx[dy:,dx:];ok=free[:hh-dy,:ww-dx]&free[dy:,dx:];av=a[ok];bv=b[ok];weight=(costs.ravel()[av]+costs.ravel()[bv])/2
 rr.extend([av,bv]);cc.extend([bv,av]);dd.extend([weight,weight])
g=coo_matrix((np.concatenate(dd),(np.concatenate(rr),np.concatenate(cc))),shape=(hh*ww,hh*ww)).tocsr()
def anchor(p):
 q=(np.array(p)-[0,.1664]-lo)/.1; y,x=np.where(free&~bk);j=np.argmin((x-q[0])**2+(y-q[1])**2);return int(idx[y[j],x[j]])
a=anchor(s['spawn']);b=anchor(route[-1]);dist,pred=dijkstra(g,indices=a,return_predecessors=True);path=[];i=b
while i!=a and i>=0:path.append(i);i=int(pred[i])
path.append(a);path=np.array(path[::-1]);points=np.c_[path%ww,path//ww]*.1+lo;hit=bk.ravel()[path];groups=[];current=[]
for p,yes in zip(points,hit):
 if yes:current.append(p.tolist())
 elif current:groups.append(current);current=[]
if current:groups.append(current)
(r/'fourth-bottlenecks.json').write_text(json.dumps({'points':points.tolist(),'blockedGroups':groups},indent=2))
print('cost',dist[b],'blocked runs',[(g[0],g[-1],len(g)) for g in groups])
fig,ax=plt.subplots(figsize=(10,14));ax.imshow(np.where(solid,2,np.where(block,1,0)),origin='lower',extent=[lo[0],lo[0]+w*.05,lo[1],lo[1]+h*.05],cmap='Pastel1',vmin=0,vmax=3);ax.plot(*points.T,'b-',lw=1);ax.scatter(*points[hit].T,c='red',s=3);ax.grid();fig.savefig(r/'fourth-bottlenecks.png',dpi=120)
