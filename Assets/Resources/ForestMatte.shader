Shader "Forest/Matte"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _Glow ("Soft light", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows addshadow
        #pragma target 3.0
        #include "ForestSoftShadow.cginc"
        fixed4 _Color; half _Glow;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            // Gentle variation gives the clay/paint finish depth without external textures.
            float grain = sin(IN.worldPos.x * 31 + sin(IN.worldPos.z * 19)) * sin(IN.worldPos.y * 37) * .012;
            o.Albedo = _Color.rgb * .88 * (1 + grain); o.Emission = _Color.rgb * _Glow * .6; o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
