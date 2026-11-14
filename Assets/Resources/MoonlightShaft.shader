Shader "Goombas/MoonlightShaft"
{
 Properties { _Color("Moonlight",Color)=(0.57,0.76,1,0.22) }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
 struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
 half4 _Color;
 V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
 half4 frag(V i):SV_Target {
 float edge=smoothstep(0,.16,i.uv.x)*smoothstep(0,.16,1-i.uv.x);
 float endFade=smoothstep(0,.22,i.uv.y)*smoothstep(0,.07,1-i.uv.y);
 // Uniform slow breathing only: intersecting UV waves made a visible checker pattern.
 float wisps=.90+.025*sin(_Time.y*.17);
 return half4(_Color.rgb,_Color.a*edge*endFade*wisps);
 }
 ENDHLSL
 }
 }
}
