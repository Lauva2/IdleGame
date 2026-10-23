#ifndef FOREST_SOFT_SHADOW_INCLUDED
#define FOREST_SOFT_SHADOW_INCLUDED

#include "AutoLight.cginc"

// GLES forward rendering samples directional shadows directly, bypassing the
// desktop screen-space soft-shadow resolve. Filter the same depth map here.
#if defined(UNITY_NO_SCREENSPACE_SHADOWS) && defined(SHADOWS_SCREEN) && defined(DIRECTIONAL)
float _ForestShadowTexelSize;
half ForestDirectionalShadow(float3 worldPos)
{
    float3 coord = mul(unity_WorldToShadow[0], float4(worldPos, 1)).xyz;
    if (any(coord.xy < 0) || any(coord.xy > 1)) return 1;
    half result = 0;
    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            float3 tap = coord + float3(float2(x, y) * (_ForestShadowTexelSize * 2.25), 0);
            half weight = (x == 0 ? 2 : 1) * (y == 0 ? 2 : 1);
            #if defined(SHADOWS_NATIVE)
                result += UNITY_SAMPLE_SHADOW(_ShadowMapTexture, tap) * weight;
            #else
                result += (SAMPLE_DEPTH_TEXTURE(_ShadowMapTexture, tap.xy) >= tap.z ? 1 : 0) * weight;
            #endif
        }
    }
    return lerp(_LightShadowData.r, 1, result * (1.0 / 16.0));
}
#undef UNITY_SHADOW_ATTENUATION
#define UNITY_SHADOW_ATTENUATION(input, worldPos) ForestDirectionalShadow(worldPos)
#endif

#endif
