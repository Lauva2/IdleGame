Shader "Forest/Soft Greenhouse Glass"
{
    Properties { _Color ("Glass tint", Color) = (0.77,0.91,0.84,0.15) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex); return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                half rim = pow(1 - abs(dot(normalize(i.normal), normalize(i.view))), 3);
                return fixed4(lerp(_Color.rgb, fixed3(1,.96,.82), rim*.35), _Color.a + rim*.12);
            }
            ENDCG
        }
    }
}
