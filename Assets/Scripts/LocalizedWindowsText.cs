using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

// Windows-only text rasterizer. GDI performs logical-order shaping, BiDi and
// script-aware line breaking; the existing IMGUI transform/clip/input stay intact.
public static class LocalizedWindowsText
{
    static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
    static long cacheBytes;
    static bool fontsLoaded;
    public static void Clear()
    {
        foreach(var texture in cache.Values)if(texture!=null)UnityEngine.Object.Destroy(texture);
        cache.Clear();cacheBytes=0;
    }
    public static void Release()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        Shutdown();
#else
        Clear();
#endif
    }
    public static void Draw(Rect rect,string text,int size,Color color,bool center,GameLocalization.Locale locale=null,bool brand=false,bool fixedSize=false)
    {
        if(string.IsNullOrEmpty(text) || Event.current.type!=EventType.Repaint || rect.width<=0 || rect.height<=0)return;
        locale=locale??GameLocalization.Active;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        EnsureFonts();
        float scale=Mathf.Clamp(GUI.matrix.MultiplyVector(Vector3.right).magnitude,1,2);
        int width=Mathf.Clamp(Mathf.CeilToInt(rect.width*scale),1,4096),height=Mathf.Clamp(Mathf.CeilToInt(rect.height*scale),1,1024);
        string face=brand?"MaokenAssortedSans":locale.font;
        string key=face+"|"+locale.rtl+"|"+center+"|"+width+"|"+height+"|"+size+"|"+fixedSize+"|"+text;
        Texture2D texture;
        if(!cache.TryGetValue(key,out texture))
        {
            if(cacheBytes>24*1024*1024)Clear();
            texture=Rasterize(text,face,width,height,Mathf.RoundToInt(size*scale),locale.rtl,center,fixedSize);
            texture.Apply(false,true);
            cache.Add(key,texture);cacheBytes+=(long)width*height*4;
        }
        var old=GUI.color;GUI.color=old*color;
        GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,true);
        GUI.color=old;
#else
        // Other targets require an equivalent shaping backend before shipping.
        var style=new GUIStyle(GUI.skin.label){font=GameFont.Main,fontSize=size,wordWrap=!fixedSize,alignment=center?TextAnchor.MiddleCenter:(locale.rtl?TextAnchor.MiddleRight:TextAnchor.MiddleLeft)};
        style.normal.textColor=color;GUI.Label(rect,text,style);
#endif
    }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    static readonly List<string> registered=new List<string>();
    public static void EnsureFonts()
    {
        if(fontsLoaded)return;
        string path=Path.Combine(Application.streamingAssetsPath,"LocalizationFonts");
        if(File.Exists(Path.Combine(path,"NotoSansMyanmar-User-Regular.ttf")))
        {
            // Retire references registered by earlier editor domains before selecting
            // the replacement. FR_PRIVATE only affects this process.
            string previous=Path.Combine(path,"NotoSansMyanmar-Regular.ttf");
            for(int i=0;i<64 && RemoveFontResourceEx(previous,0x10,IntPtr.Zero);i++){}
        }
        if(Directory.Exists(path))foreach(string file in Directory.GetFiles(path))
            if(file.EndsWith(".ttf",StringComparison.OrdinalIgnoreCase)||file.EndsWith(".otf",StringComparison.OrdinalIgnoreCase))
            {
                // The user-supplied Myanmar revision supersedes the original same-family file.
                if(Path.GetFileName(file)=="NotoSansMyanmar-Regular.ttf" && File.Exists(Path.Combine(path,"NotoSansMyanmar-User-Regular.ttf")))continue;
                if(AddFontResourceEx(file,0x10,IntPtr.Zero)>0)registered.Add(file);else Debug.LogError("Unable to register bundled font: "+file);
            }
        fontsLoaded=true;
        Application.quitting-=Shutdown;Application.quitting+=Shutdown;
    }
    static void Shutdown(){Clear();foreach(var file in registered)RemoveFontResourceEx(file,0x10,IntPtr.Zero);registered.Clear();fontsLoaded=false;}
    public static string InspectCoverage(string text,string face)
    {
        EnsureFonts();IntPtr dc=CreateCompatibleDC(IntPtr.Zero);
        IntPtr font=CreateFont(-32,0,0,0,400,0,0,0,1,0,0,4,0,face),previous=SelectObject(dc,font);
        try
        {
            var actual=new System.Text.StringBuilder(128);GetTextFace(dc,actual.Capacity,actual);
            bool maokenAlias=face=="MaokenAssortedSans" && actual.ToString()=="猫啃什锦黑";
            if(!maokenAlias && !string.Equals(actual.ToString(),face,StringComparison.OrdinalIgnoreCase))return "Font substitution: "+face+" -> "+actual;
            ushort[] glyphs=new ushort[text.Length];
            if(GetGlyphIndices(dc,text,text.Length,glyphs,1)==uint.MaxValue)return "Glyph query failed: "+face;
            foreach(string fallback in new[]{"Noto Sans","Segoe UI Symbol"})
            {
                IntPtr fallbackFont=CreateFont(-32,0,0,0,400,0,0,0,1,0,0,4,0,fallback);
                SelectObject(dc,fallbackFont);var fallbackGlyphs=new ushort[text.Length];
                if(GetGlyphIndices(dc,text,text.Length,fallbackGlyphs,1)!=uint.MaxValue)
                    for(int i=0;i<glyphs.Length;i++)if(glyphs[i]==65535 && fallbackGlyphs[i]!=65535)glyphs[i]=fallbackGlyphs[i];
                SelectObject(dc,font);DeleteObject(fallbackFont);
            }
            var missing=new System.Text.StringBuilder();
            for(int i=0;i<glyphs.Length;i++)if(glyphs[i]==65535 && !char.IsWhiteSpace(text[i]))missing.Append(" U+"+((int)text[i]).ToString("X4"));
            return missing.ToString();
        }
        finally {SelectObject(dc,previous);DeleteObject(font);DeleteDC(dc);}
    }
    public static uint SelectedFontByteLength(string face)
    {
        EnsureFonts();IntPtr dc=CreateCompatibleDC(IntPtr.Zero);
        IntPtr font=CreateFont(-32,0,0,0,400,0,0,0,1,0,0,4,0,face),previous=SelectObject(dc,font);
        try {return GetFontData(dc,0,0,IntPtr.Zero,0);}
        finally {SelectObject(dc,previous);DeleteObject(font);DeleteDC(dc);}
    }
    [StructLayout(LayoutKind.Sequential)] struct NativeRect {public int left,top,right,bottom;public NativeRect(int w,int h){left=top=0;right=w;bottom=h;}}
    [StructLayout(LayoutKind.Sequential)] struct BitmapInfo
    {
        public uint size;public int width,height;public ushort planes,bits;public uint compression,imageSize;public int xppm,yppm;public uint used,important;
    }
    public static Vector2 MeasureSingleLine(string text,string face,int size)
    {
        EnsureFonts();IntPtr dc=CreateCompatibleDC(IntPtr.Zero);
        IntPtr font=CreateFont(-size,0,0,0,400,0,0,0,1,0,0,4,0,face),previous=SelectObject(dc,font);
        try {var rect=new NativeRect(10000,1000);DrawText(dc,text,text.Length,ref rect,0x20u|0x800u|0x400u);return new Vector2(rect.right-rect.left,rect.bottom-rect.top);}
        finally {SelectObject(dc,previous);DeleteObject(font);DeleteDC(dc);}
    }
    public sealed class TextLayout
    {
        public string[] Lines;
        public int FontSize;
        public float Width,Height,LineHeight;
        public bool Fits;
    }
    static Vector2 MeasureAtDc(IntPtr dc,string text)
    {
        var rect=new NativeRect(100000,10000);
        DrawText(dc,string.IsNullOrEmpty(text)?" ":text,string.IsNullOrEmpty(text)?1:text.Length,ref rect,0x20u|0x800u|0x400u);
        return new Vector2(rect.right-rect.left,rect.bottom-rect.top);
    }
    static TextLayout LayoutAtDc(IntPtr dc,string text,int width,int height,int size,bool singleLine)
    {
        var lines=new List<string>();
        foreach(string paragraph in text.Replace("\r","").Split('\n'))
        {
            if(singleLine){lines.Add(paragraph);continue;}
            string line="";
            // Only explicit spaces/tabs are break opportunities. In scripts with
            // no spaces, retain the entire span and reduce size rather than invent
            // a linguistic boundary or split a combining sequence.
            foreach(string word in System.Text.RegularExpressions.Regex.Split(paragraph,"[ \\t]+"))
            {
                if(word.Length==0)continue;
                string candidate=line.Length==0?word:line+" "+word;
                if(line.Length>0 && MeasureAtDc(dc,candidate).x>width-8){lines.Add(line);line=word;}
                else line=candidate;
            }
            lines.Add(line);
        }
        float maxWidth=0,lineHeight=MeasureAtDc(dc,"Ag").y;
        foreach(string line in lines){var measured=MeasureAtDc(dc,line);maxWidth=Math.Max(maxWidth,measured.x);lineHeight=Math.Max(lineHeight,measured.y);}
        float totalHeight=lineHeight*lines.Count;
        return new TextLayout{Lines=lines.ToArray(),FontSize=size,Width=maxWidth,Height=totalHeight,LineHeight=lineHeight,Fits=maxWidth<=width-8 && totalHeight<=height-4};
    }
    public static TextLayout MeasureLayout(string text,string face,int width,int height,int size,bool singleLine=false)
    {
        EnsureFonts();IntPtr dc=CreateCompatibleDC(IntPtr.Zero);
        IntPtr font=CreateFont(-size,0,0,0,400,0,0,0,1,0,0,4,0,face),previous=SelectObject(dc,font);
        try {return LayoutAtDc(dc,text,width,height,size,singleLine);}
        finally {SelectObject(dc,previous);DeleteObject(font);DeleteDC(dc);}
    }
    public static TextLayout FitLayout(string text,string face,int width,int height,int maxSize,bool singleLine=false)
    {
        TextLayout layout=null;
        for(int size=maxSize;size>=8;size--){layout=MeasureLayout(text,face,width,height,size,singleLine);if(layout.Fits)return layout;}
        return layout;
    }
    public static Texture2D Rasterize(string text,string face,int width,int height,int requestedSize,bool rtl,bool center,bool fixedSize=false)
    {
        EnsureFonts();
        IntPtr dc=CreateCompatibleDC(IntPtr.Zero),bitmap=IntPtr.Zero,oldBitmap=IntPtr.Zero,font=IntPtr.Zero,oldFont=IntPtr.Zero;
        if(dc==IntPtr.Zero)throw new InvalidOperationException("Cannot create text drawing context.");
        try
        {
            var info=new BitmapInfo{size=40,width=width,height=-height,planes=1,bits=32};IntPtr bits;
            bitmap=CreateDIBSection(dc,ref info,0,out bits,IntPtr.Zero,0);
            if(bitmap==IntPtr.Zero)throw new InvalidOperationException("Cannot create text bitmap.");
            oldBitmap=SelectObject(dc,bitmap);
            byte[] data=new byte[checked(width*height*4)];Marshal.Copy(data,0,bits,data.Length);
            SetBkMode(dc,1);SetTextColor(dc,0x00ffffff);
            uint flags=0x20u|0x800u|(rtl?0x20000u:0u)|(center?1u:rtl?2u:0u);
            TextLayout layout=null;
            // A size of zero is never used; fixed-size settings captions were
            // measured as a group before drawing, so do not resize one separately.
            int minSize=fixedSize?requestedSize:8,fontSize=requestedSize;
            for(;;)
            {
                font=CreateFont(-fontSize,0,0,0,400,0,0,0,1,0,0,4,0,face);
                if(font==IntPtr.Zero)throw new InvalidOperationException("Cannot create font: "+face);
                IntPtr previous=SelectObject(dc,font);if(oldFont==IntPtr.Zero)oldFont=previous;
                layout=LayoutAtDc(dc,text,width,height,fontSize,false);
                if(layout.Fits || fontSize<=minSize)break;
                SelectObject(dc,oldFont);DeleteObject(font);font=IntPtr.Zero;fontSize--;
            }
            int top=Math.Max(1,(height-(int)layout.Height)/2);
            foreach(string line in layout.Lines)
            {
                var target=new NativeRect(width-4,height-2);target.left=4;target.top=top;
                DrawText(dc,line,line.Length,ref target,flags);top+=(int)layout.LineHeight;
            }
            GdiFlush();Marshal.Copy(bits,data,0,data.Length);
            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {int i=(y*width+x)*4;byte alpha=Math.Max(data[i],Math.Max(data[i+1],data[i+2]));pixels[(height-1-y)*width+x]=new Color32(255,255,255,alpha);}
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Localized text",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,false);return texture;
        }
        finally
        {
            if(oldFont!=IntPtr.Zero)SelectObject(dc,oldFont);if(font!=IntPtr.Zero)DeleteObject(font);
            if(oldBitmap!=IntPtr.Zero)SelectObject(dc,oldBitmap);if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);DeleteDC(dc);
        }
    }
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool GdiFlush();
    [DllImport("gdi32.dll")] static extern uint GetFontData(IntPtr dc,uint table,uint offset,IntPtr data,uint length);
    [DllImport("gdi32.dll")] static extern int SetBkMode(IntPtr dc,int mode);
    [DllImport("gdi32.dll")] static extern uint SetTextColor(IntPtr dc,uint color);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
    [DllImport("gdi32.dll",CharSet=CharSet.Unicode,EntryPoint="CreateFontW")] static extern IntPtr CreateFont(int height,int width,int escape,int orientation,int weight,uint italic,uint underline,uint strike,uint charset,uint output,uint clip,uint quality,uint family,string face);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="DrawTextW")] static extern int DrawText(IntPtr dc,string text,int count,ref NativeRect rect,uint flags);
    [DllImport("gdi32.dll",CharSet=CharSet.Unicode,EntryPoint="AddFontResourceExW")] static extern int AddFontResourceEx(string path,uint flags,IntPtr reserved);
    [DllImport("gdi32.dll",CharSet=CharSet.Unicode,EntryPoint="RemoveFontResourceExW")] static extern bool RemoveFontResourceEx(string path,uint flags,IntPtr reserved);
    [DllImport("gdi32.dll",CharSet=CharSet.Unicode,EntryPoint="GetTextFaceW")] static extern int GetTextFace(IntPtr dc,int count,System.Text.StringBuilder face);
    [DllImport("gdi32.dll",CharSet=CharSet.Unicode,EntryPoint="GetGlyphIndicesW")] static extern uint GetGlyphIndices(IntPtr dc,string text,int count,[Out] ushort[] glyphs,uint flags);
#endif
}
