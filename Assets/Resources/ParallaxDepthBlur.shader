Shader "Custom/ParallaxDepthBlur" {
 Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} _Radius ("Blur radius in source pixels", Float) = 0 }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
 Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex; float4 _MainTex_TexelSize; float _Radius;
 struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 output vert(input v) { output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
 float4 samplePremult(float2 uv) { float4 c=tex2D(_MainTex,saturate(uv)); c.rgb*=c.a; return c; }
 fixed4 frag(output i):SV_Target {
 // Dense 9x9 Gaussian kernel: support +/- radius, sigma = radius / 3.
 // At the largest authored radius (7px), adjacent taps are only 1.75px apart.
 float2 stepUV = _MainTex_TexelSize.xy * (_Radius / 4.0);
 float4 c = 0;
 float totalWeight = 0;
 [unroll] for (int y = -4; y <= 4; y++) {
     [unroll] for (int x = -4; x <= 4; x++) {
         float weight = exp(-0.28125 * (x*x + y*y));
         c += samplePremult(i.uv + float2(x,y) * stepUV) * weight;
         totalWeight += weight;
     }
 }
 c /= totalWeight;
 c.rgb *= i.color.rgb * i.color.a;
 c.a *= i.color.a;
 return c;
 }
 ENDCG
 }
 }
}
