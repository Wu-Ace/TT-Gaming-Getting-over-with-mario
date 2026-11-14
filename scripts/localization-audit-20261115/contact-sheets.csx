using System.Linq;
string root=System.IO.Path.GetFullPath("scripts/localization-audit-20261115");
System.IO.Directory.CreateDirectory(root+"/sheets");
var codes=GameLocalization.Locales.Select(l=>l.code).ToArray();
foreach(string kind in new[]{"settings","levels","home","loading","hud","completion-preview"})
for(int page=0;page<(codes.Length+3)/4;page++)
{
 int tileW=900,tileH=500;
 var output=new UnityEngine.Texture2D(tileW*2,tileH*2,UnityEngine.TextureFormat.RGBA32,false);
 var dest=new UnityEngine.Color32[tileW*2*tileH*2];
 for(int i=0;i<4;i++)
 {
  int index=page*4+i;if(index>=codes.Length)break;
  string file=root+"/ui/"+codes[index]+"-"+kind+".png";if(!System.IO.File.Exists(file))continue;
  var source=new UnityEngine.Texture2D(2,2);UnityEngine.ImageConversion.LoadImage(source,System.IO.File.ReadAllBytes(file));var src=source.GetPixels32();
  int x0=0,y0=0,w=source.width,h=source.height;
  if(kind=="settings" || kind=="completion-preview")
  {x0=(int)(source.width*.23f);y0=(int)(source.height*.12f);w=(int)(source.width*.55f);h=(int)(source.height*.78f);}
  int dx=(i%2)*tileW,dy=(1-i/2)*tileH;
  for(int y=0;y<tileH-32;y++)for(int x=0;x<tileW;x++)
  {int sx=x0+x*w/tileW,sy=y0+y*h/(tileH-32);dest[(dy+y)*(tileW*2)+dx+x]=src[sy*source.width+sx];}
  var label=LocalizedWindowsText.Rasterize(codes[index]+" / "+kind,"Noto Sans",tileW,32,22,false,true);
  var caption=label.GetPixels32();
  for(int y=0;y<32;y++)for(int x=0;x<tileW;x++){var p=caption[y*tileW+x];byte v=(byte)(24+p.a*231/255);dest[(dy+tileH-32+y)*(tileW*2)+dx+x]=new UnityEngine.Color32(v,v,v,255);}
  UnityEngine.Object.DestroyImmediate(label);UnityEngine.Object.DestroyImmediate(source);
 }
 output.SetPixels32(dest);output.Apply();System.IO.File.WriteAllBytes(root+"/sheets/"+kind+"-"+page+".png",UnityEngine.ImageConversion.EncodeToPNG(output));UnityEngine.Object.DestroyImmediate(output);
}
return "Contact sheets generated; ordered four locales per sheet.";
