import json,numpy as np,matplotlib.pyplot as plt
from pathlib import Path
r=Path(__file__).parent;s=next(x for x in json.loads((r/'geometry.json').read_text()) if 'Level04' in x['scene']); z=np.load(r/'Level04_Town-grid.npz'); lo=z['lo']; solid=z['solid'];block=z['blocked']; h,w=solid.shape
fig,ax=plt.subplots(figsize=(10,16)); ax.imshow(np.where(solid,2,np.where(block,1,0)),origin='lower',extent=[lo[0],lo[0]+w*.05,lo[1],lo[1]+h*.05],cmap='Pastel1',vmin=0,vmax=3); route=np.array(s['route']);ax.plot(*route.T,'b.-');
for i,p in enumerate(route):ax.text(*p,str(i),fontsize=8)
ax.set_xticks(np.arange(0,45,2));ax.set_yticks(np.arange(-4,58,2));ax.grid(alpha=.3);fig.savefig(r/'fourth-route.png',dpi=120)
