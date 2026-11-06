import json
from pathlib import Path
from PIL import Image,ImageDraw
mask=Image.new('1',(1024,574));d=ImageDraw.Draw(mask)
polys=[[(0,4),(69,4),(70,242),(65,253),(73,289),(83,298),(83,329),(92,366),(93,413),(106,431),(108,445),(130,448),(141,460),(145,483),(156,492),(162,573),(0,573)],[(222,345),(341,345),(341,237),(348,231),(395,231),(397,72),(390,53),(395,43),(389,31),(378,39),(368,40),(357,28),(350,12),(344,13),(335,34),(321,52),(309,54),(302,65),(302,73),(293,76),(288,89),(275,99),(269,111),(271,124),(264,135),(267,146),(263,158),(244,181),(244,200),(238,218),(231,226),(231,239),(225,253),(228,263),(222,281)],[(306,573),(306,520),(311,502),(307,492),(312,478),(318,468),(325,474),(326,489),(334,504),(340,511),(340,573)],[(542,573),(542,521),(535,514),(548,514),(554,505),(557,488),(566,475),(572,480),(578,503),(578,511),(587,516),(594,511),(595,573)],[(777,573),(777,501),(786,487),(791,475),(791,457),(801,444),(811,436),(823,436),(830,430),(855,429),(866,433),(878,429),(885,432),(893,425),(899,405),(897,386),(900,374),(897,366),(906,351),(923,352),(934,351),(942,355),(965,350),(984,355),(1000,350),(1024,351),(1024,573)],[(563,91),(800,91),(800,117),(563,117)]]
for poly in polys:d.polygon(poly,fill=1)
cells={(x,57-r) for x in range(103) for r in range(58) if mask.getpixel((min(1023,x*10+5),min(573,r*10+5)))}
donuts=[]
def ledge(px,py,w,fall=False):
 x=round(px/10);y=57-round(py/10);w=max(1,round(w/10))
 if fall:donuts.append(dict(x=x,y=y,width=w))
 else:
  for a in range(x,x+w):cells.add((a,y))
for a,b,c in [(188,499,72),(444,471,36),(487,478,20),(594,463,64),(686,463,46),(757,443,24),(129,342,20),(317,34,20),(151,291,20),(187,187,20),(230,112,20),(264,81,20),(291,55,20),(437,43,18),(473,58,24),(508,72,32),(544,114,16),(535,246,14),(566,260,14)]:ledge(a,b,c,True)
for a,b,c in [(357,490,28),(394,483,30),(500,502,14),(510,525,14),(523,492,14),(598,503,14),(617,484,14),(617,520,14),(635,503,14),(662,506,20),(702,503,14),(716,484,14),(716,520,14),(734,503,14),(153,470,20),(129,391,14),(111,269,14),(134,259,14),(102,245,14),(128,235,14),(180,221,18),(201,156,32),(194,135,32),(118,423,20),(108,309,20)]:ledge(a,b,c)
for q in donuts:
 while any((x,q['y']) in cells for x in range(q['x'],q['x']+q['width'])):q['y']+=1
v=lambda x,y:dict(x=x,y=y)
def world(px,py):return v(px*.08,(573-py)*.08-3.6)
p=dict(number=3,reference='User silhouette 1024x573; structural cliffs with native Beach cells',title='Beach cliff loop',cells=[v(x,y) for x,y in sorted(cells)],donuts=donuts,stars=[world(685,475),world(154,322),world(483,30)],checkpoints=[world(120,412),world(345,0)],spawn=world(958,333),exit=world(755,73),route=[world(958,333),world(764,426),world(690,450),world(596,450),world(450,460),world(190,485),world(120,412),world(145,250),world(210,127),world(345,0),world(480,30),world(755,73)],authoredLength=160)
for point in p['stars']+[p['spawn'],p['exit']]+p['checkpoints']:assert (int(point['x']/.8),int((point['y']+3.6)/.8)) not in cells,point
Path('scripts/level03-designed.json').write_text(json.dumps(p,indent=2))
preview=Image.new('RGB',(1030,580),'white');draw=ImageDraw.Draw(preview)
for x,y in cells:draw.rectangle((x*10,(57-y)*10,x*10+9,(57-y)*10+9),fill='#bb9b64')
for q in donuts:draw.rectangle((q['x']*10,(57-q['y'])*10,q['x']*10+q['width']*10-1,(57-q['y'])*10+4),fill='#775126')
Path('scripts/level03-reference-evidence').mkdir(exist_ok=True)
preview.save('scripts/level03-reference-evidence/layout.png')
print(len(cells),len(donuts))
