Shader "Rayman/CelLit"
{
    Properties
    {
        [Header(Shade)][Space]
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.01)) = 0.006
    	_Metallic("Metallic", Range(0.0, 1.0)) = 0
    	_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    	_MainCelCount ("Main Cel Count", Range(1.0, 10.0)) = 1.0
    	_AdditionalCelCount ("Additional Cel Count", Range(1.0, 10.0)) = 1.0
    	_CelSpread ("Cel Spread", Range(0.0, 1.0)) = 1.0
    	_CelSharpness ("Cel Sharpness", Float) = 80.0
    	_SpecularSharpness ("Specular Sharpness", Float) = 30.0
    	_RimAmount ("Rim Amount", Range(0.0, 1.0)) = 0.75
    	_RimSmoothness ("Rim Smoothness", Range(0.0, 1.0)) = 0.03
    	_BlendDiffuse ("Blend Diffuse", Range(0.0, 1.0)) = 0.9
    	_F0 ("Schlick F0", Float) = 0.04
    	
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
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
        
		struct Shape
		{
        	int type;
			float4x4 transform;
			half3 size;
        	half3 pivot;
        	int operation;
        	half blend;
			half roundness;
			half4 color;
		};

        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];
		half4 baseColor;

		inline float Map(const float3 pos)
		{
			float totalDist = _MaxDistance;
			baseColor = _ShapeBuffer[hitIds[0]].color;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 p = ApplyMatrix(pos, shape.transform);
				p -= GetPivotOffset(shape.type, shape.pivot, shape.size);
				
				float3 scale = GetScale(shape.transform);
		        float scaleFactor = min(scale.x, min(scale.y, scale.z));

				float dist = GetShapeSdf(p, shape.type, shape.size, shape.roundness) / scaleFactor;
				float blend = 0;
				totalDist = CombineShapes(totalDist, dist, shape.operation, shape.blend, blend);
				baseColor = lerp(baseColor, shape.color, blend);
			}
			return totalDist;
		}

		inline float NormalMap(const float3 pos)
		{
			float totalDist = _MaxDistance;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 p = ApplyMatrix(pos, shape.transform);
				p -= GetPivotOffset(shape.type, shape.pivot, shape.size);
				
				float3 scale = GetScale(shape.transform);
		        float scaleFactor = min(scale.x, min(scale.y, scale.z));

				float dist = GetShapeSdf(p, shape.type, shape.size, shape.roundness) / scaleFactor;
				float blend = 0;
				totalDist = CombineShapes(totalDist, dist, shape.operation, shape.blend, blend);
			}
			return totalDist;
		}

		inline NodeAabb GetNode(const int index)
		{
			return _NodeBuffer[index];
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
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
		    
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/CelLitForwardPass.hlsl"
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
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			
			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
