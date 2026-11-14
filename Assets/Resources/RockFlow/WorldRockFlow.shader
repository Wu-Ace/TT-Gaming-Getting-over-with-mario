Shader "Terrain/World Rock Flow" {
Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _FlowTex("Flow",2D)="white"{} _Opacity("Opacity",Range(0,1))=1.00 }
SubShader {
Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
Pass {
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex, _FlowTex; float4 _Color; float _Opacity;
struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; float4 color:COLOR; };
v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xy; o.uv=v.uv; o.color=v.color*_Color; return o; }
float4 frag(v2f i):SV_Target { float4 c=tex2D(_MainTex,i.uv)*i.color; float4 f=tex2D(_FlowTex,i.world/float2(3.2,3.2)-_Time.y*float2(0.025,0.008)); f.rgb=lerp(f.rgb,float3(1,1,1),0.65); c.rgb=lerp(c.rgb,f.rgb,_Opacity*f.a); return c; }
ENDHLSL
}
}
}





