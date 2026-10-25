Shader "Forest/QuietWater"
{
    Properties { _Color ("Water tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 3.0
        #include "ForestSoftShadow.cginc"
        fixed4 _Color;
        struct Input { float4 color : COLOR; float3 worldPos; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float grain=sin(IN.worldPos.x*11+sin(IN.worldPos.z*7))*.006;
            o.Albedo=IN.color.rgb*_Color.rgb*.88*(1+grain);
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
