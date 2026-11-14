Shader "Goombas/MoonlitDecoration" {
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" "DisableBatching"="True"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 Tags {"LightMode"="Universal2D"}
 HLSLPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 float4 _Color; int _MoonCount;
 float4 _MoonX[64],_MoonY[64];
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;float4 c:COLOR;};
 V vert(A a){V o;float3 w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(w);o.world=w.xy;o.uv=a.uv;o.c=a.c*_Color;return o;}
 half4 frag(V i):SV_Target {
 half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.c;
 float light=0;
 for(int b=0;b<_MoonCount;b++){
 float u=dot(_MoonX[b].xy,i.world)+_MoonX[b].z+.5;
 float v=dot(_MoonY[b].xy,i.world)+_MoonY[b].z;
 // Broad, continuous penumbra across the surface; zero outside the beam.
 float across=smoothstep(0,.42,u)*smoothstep(0,.42,1-u);
 float a=across*smoothstep(0,.28,v)*smoothstep(0,.20,1-v);
 // Broad translucent shaft tails must not accumulate into a uniform wash.
 // Keep the strongest local beam, avoiding accumulated ambient brightening.
 float core=a;
 light=max(light,core*_MoonX[b].w);
 }
 // Preserve the source texture and silhouette even where several shafts overlap.
 // Full-strength beam cores recover the exact authored sprite color.
 float exposure=smoothstep(0,.24,light);
 // Dark ambient base, with only the beam-covered pixels recovering their light.
 float3 shadow=c.rgb*.40;
 float3 moonlit=c.rgb;
 c.rgb=lerp(shadow,moonlit,exposure);
 return c;
 }
 ENDHLSL
 }
 }
}
