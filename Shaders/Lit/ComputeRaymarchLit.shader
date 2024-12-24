Shader "Rayman/ComputeRaymarchLit"
{
    Properties
    {
        [Header(Shade)][Space]
    	_F0 ("Fresnel F0", Float) = 0.4
    	_SpecularPow ("Specular Power", Float) = 10.0
    	_RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1)
    	_RimPow ("Rim Power", Float) = 0.1
    	_ShadowBiasVal ("Shadow Bias", Float) = 0.015
    	
    	[Header(Blending)][Space]
    	[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend", Float) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend ", Float) = 0.0
	    [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2.0
    }
    SubShader
    {
        Tags
        {
        	"RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        	"UniversalMaterialType" = "Unlit"
        	"Queue"="AlphaTest"
        	"DisableBatching"="False"
        }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
        
        struct RaymarchResult
		{
		    float3 hitPoint;
		    float travelDistance;
			float lastHitDistance;
			float3 rayDirection;
        	float4 color;
			float3 normal;
		};

		StructuredBuffer<RaymarchResult> _ResultBuffer;
        ENDHLSL

        Pass
		{
			Name "Forward Lit"
			
			Blend [_SrcBlend] [_DstBlend]
		    ZWrite On
		    Cull [_Cull]
		    AlphaToMask On
		    
			HLSLPROGRAM
			#pragma target 5.0
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS

		    #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
		    
            #pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma instancing_options renderinglayer

			#pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/ComputeRaymarchLitForwardLit.hlsl"
            ENDHLSL
		}

		Pass
		{
			Name "Depth Only"
		    Tags { "LightMode" = "DepthOnly" }

		    ZTest LEqual
		    ZWrite On
		    ColorMask R
		    Cull [_Cull]

		    HLSLPROGRAM
		    #pragma target 5.0
		    #pragma shader_feature _ALPHATEST_ON
		    #pragma multi_compile_instancing

		    #pragma vertex vert
		    #pragma fragment frag
		    
			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/ComputeRaymarchLitDepthOnly.hlsl"
		    ENDHLSL
		}

        Pass
        {
        	Name "Depth Normals"
		    Tags { "LightMode" = "DepthNormals" }

		    ZWrite On
		    Cull [_Cull]

		    HLSLPROGRAM
		    #pragma target 5.0
		    #pragma shader_feature _ALPHATEST_ON
		    #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
		    #pragma multi_compile_instancing

			#pragma vertex vert
		    #pragma fragment frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/ComputeRaymarchLitDepthNormals.hlsl"
		    ENDHLSL
        }
    }
}
