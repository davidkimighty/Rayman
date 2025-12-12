	Shader "Rayman/MengerSponge"
{
    Properties
    {
    	[Header(Menger Sponge)][Space]
    	[MainColor] _BaseColor("Color", Color) = (1,1,1,1)
    	_Size("Size", Float) = 0.5
    	_Iterations("Iterations", Int) = 3
    	_Scale("Scale", Float) = 3.0
    	_ScaleMultiplier("Scale Multiplier", Float) = 4.0
    	
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    	_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.01)) = 0.006
    	
    	[Header(Raymarching)][Space]
    	_EpsilonMin("Epsilon Min", Float) = 0.001
    	_EpsilonMax("Epsilon Max", Float) = 0.01
    	_MaxSteps("Max Steps", Int) = 64
    	_MaxDistance("Max Distance", Float) = 100.0
    	_ShadowMaxSteps("Shadow Max Steps", Int) = 16
    	_ShadowMaxDistance("Shadow Max Distance", Float) = 30.0
    	
    	[Header(Blending)][Space]
    	[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend", Float) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend ", Float) = 0.0
	    [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2.0
	    [Toggle][KeyEnum(Off, On)] _ZWrite("ZWrite", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
        	"RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        	"UniversalMaterialType" = "Lit"
        	"IgnoreProjector" = "True"
        	"DisableBatching" = "True"
        }
        LOD 100
        
        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchLighting.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

		CBUFFER_START(RaymarchPerGroup)
		float _EpsilonMin;
		float _EpsilonMax;
        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		CBUFFER_END

		float4x4 _Transform;
		float _Size;
		int _Iterations;
		float _Scale;
		float _ScaleMultiplier;
		half4 color;

		inline float SpongeDistance(float3 positionWS)
		{
			float3 posOS = mul(_Transform, float4(positionWS, 1.0)).xyz;
			float box = BoxSdf(posOS, _Size);
			float scale = _Scale;
			
			for (int i = 0; i < _Iterations; i++)
			{
				float3 a = abs(fmod(posOS * scale + scale * 2.0, 2.0) - 1.0);
				scale *= _ScaleMultiplier;
				float3 r = abs(1.0 - 3.0 * abs(a));
				
				float da = max(r.x, r.y);
				float db = max(r.y, r.z);
				float dc = max(r.z, r.x);
				float c = (min(da, min(db, dc)) - 1.0) / scale;
				if (c > box)
					box = c;
			}
			return box;
		}
		
		float Map(inout Ray ray)
		{
			float3 posOS = mul(_Transform, float4(ray.hitPoint, 1.0)).xyz;
			float box = BoxSdf(posOS, _Size);
			float scale = _Scale;
			color = _BaseColor;
			ray.data = float4(1.0, 0, 0, 0);
			
			for (int i = 0; i < _Iterations; i++)
			{
				float3 a = abs(fmod(posOS * scale + scale * 2.0, 2.0) - 1.0);
				scale *= _ScaleMultiplier;
				float3 r = abs(1.0 - 3.0 * abs(a));
				
				float da = max(r.x, r.y);
				float db = max(r.y, r.z);
				float dc = max(r.z, r.x);
				float c = (min(da, min(db, dc)) - 1.0) / scale;
				if (c > box)
				{
					box = c;
					ray.data.x = min(ray.data.x, 0.2 * da * db * dc);
					ray.data.y = (1.0 + float(i)) / _Iterations;
				}
			}
			return box;
		}

		float NormalMap(const float3 positionWS)
		{
			return SpongeDistance(positionWS);
		}

		float ShadowMap(const float3 positionWS)
		{
			return SpongeDistance(positionWS);
		}
        
        ENDHLSL

        Pass
		{
			Name "Forward"
			Tags
			{
				"LightMode" = "UniversalForward"
			}
			
			Blend [_SrcBlend] [_DstBlend]
		    ZWrite [_ZWrite]
		    Cull [_Cull]
		    
			HLSLPROGRAM
			#pragma target 2.0
			
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
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

		    #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
		    
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile_fragment _ DEBUG_MODE
			#pragma multi_compile_fragment _ GRADIENT_COLOR
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Samples/Fractal/MengerSpongeForwardPass.hlsl"
            ENDHLSL
		}

		Pass
		{
			Name "Shadow Caster"
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 2.0
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			
			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Samples/Fractal/MengerSpongeShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
