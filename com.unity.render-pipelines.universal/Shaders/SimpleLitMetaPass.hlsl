#ifndef UNIVERSAL_SIMPLE_LIT_META_PASS_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

half4 UniversalFragmentMetaSimple(MetaVaryings input) : SV_Target
{
    float2 uv = input.uv;
    UnityMetaInput metaInput;
    metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
    metaInput.SpecularColor = SampleSpecularSmoothness(uv, 1.0h, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap)).xyz;
    metaInput.Emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    return UniversalFragmentMeta(input, metaInput);
}

//LWRP -> Universal Backwards Compatibility

half4 LightweightFragmentMetaSimple(Varyings input) : SV_Target
{
    return UniversalFragmentMetaSimple(VaryingsToMetaVaryings(input));
}

#endif
