Shader "CrashSimulator/UI/ReferencePhoto"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Grayscale ("Reference grayscale", Range(0,1)) = 0.7
        _Contrast ("Reference contrast", Float) = 1.12
        _Brightness ("Reference brightness", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _Grayscale, _Contrast, _Brightness;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float4 c=tex2D(_MainTex,i.uv);
                // CSS filter operations are defined in sRGB; preserve the project's linear rendering.
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                c.rgb=lerp(c.rgb,dot(c.rgb,float3(.2126,.7152,.0722)),_Grayscale);
                c.rgb=saturate((c.rgb-.5)*_Contrast+.5)*_Brightness;
                #ifndef UNITY_COLORSPACE_GAMMA
                    c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                return c*i.color;
            }
            ENDCG
        }
    }
}
