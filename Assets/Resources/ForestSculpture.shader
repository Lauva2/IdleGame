Shader "Forest/Sculpture"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 3.0
        #include "ForestSoftShadow.cginc"
        fixed4 _Color;
        struct Input { float4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb * .92;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
