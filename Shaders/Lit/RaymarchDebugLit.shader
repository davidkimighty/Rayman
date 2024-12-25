Shader "Rayman/RaymarchDebugLit"
{
    Properties
    {
        [Header(Shade)][Space]
    	_F0 ("Fresnel F0", Float) = 0.4
    	_SpecularPow ("Specular Power", Float) = 10.0
    	_RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1)
    	_RimPow ("Rim Power", Float) = 0.1
    	_ShadowBiasVal ("Shadow Bias", Float) = 0.015
    	
        [Header(Raymarching)][Space]
    	_MaxSteps ("MaxSteps", Int) = 128
    	_MaxDistance ("MaxDist", Float) = 100.0
    	
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
        #pragma shader_feature _DISTORTION_FEATURE
        
		#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Distortion.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
        
		struct Shape
		{
			float4x4 transform;
			int type;
			float3 size;
			float roundness;
			int operation;
			float smoothness;
			half4 color;
			half4 emissionColor;
			float emissionIntensity;
			int distortionEnabled;
		};

		struct Distortion
		{
			int id;
			int type;
			float amount;
		};

        int _DebugMode;
        int _BoundsDisplayThreshold;
        
        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
		float _ShadowMaxDistance;
        int _DistortionCount;
		StructuredBuffer<Shape> _ShapeBuffer;
		StructuredBuffer<Distortion> _DistortionBuffer;
        StructuredBuffer<NodeAABB> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];
		float4 finalColor;

        inline void ApplyDistortionPositionById(inout float3 pos, const int id)
		{
		    for (int i = 0; i < _DistortionCount; i++)
		    {
		        Distortion o = _DistortionBuffer[i];
		        if (o.id != id) continue;
				                
		        pos = ApplyDistortion(pos, o.type, o.amount);
		        break;
		    }
		}
        
		inline float Map(const Ray ray)
		{
			float totalDist = _MaxDistance;
			finalColor = _ShapeBuffer[hitIds[0]].color;
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 pos = ApplyMatrix(ray.hitPoint, shape.transform);
				float3 scale = GetScale(shape.transform);
		        float scaleFactor = min(scale.x, min(scale.y, scale.z));
#ifdef _DISTORTION_FEATURE
				if (shape.distortionEnabled > 0)
					ApplyDistortionPositionById(pos, i);
#endif
				float dist = GetShapeSDF(pos, shape.type, shape.size, shape.roundness) / scaleFactor;
				float blend = 0;
				totalDist = CombineShapes(totalDist, dist, shape.operation, shape.smoothness, blend);
				finalColor = lerp(finalColor, shape.color + shape.emissionColor * shape.emissionIntensity, blend);
			}
			return totalDist;
		}

		inline float NormalMap(const float3 rayPos)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 pos = ApplyMatrix(rayPos, shape.transform);
				float3 scale = GetScale(shape.transform);
		        float scaleFactor = min(scale.x, min(scale.y, scale.z));
#ifdef _DISTORTION_FEATURE
				if (shape.distortionEnabled > 0)
					ApplyDistortionPositionById(pos, i);
#endif
				float dist = GetShapeSDF(pos, shape.type, shape.size, shape.roundness) / scaleFactor;
				float blend = 0;
				totalDist = CombineShapes(totalDist, dist, shape.operation, shape.smoothness, blend);
			}
			return totalDist;
		}

        inline NodeAABB GetNode(const int index)
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

			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/RaymarchDebugLitForward.hlsl"
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
			#pragma target 5.0
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/RaymarchLitShadowCaster.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
