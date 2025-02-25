	Shader "Rayman/LineLit"
{
    Properties
    {
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    	_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.01)) = 0.006
    	
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
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/BVH.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Shared/Lines.hlsl"
		
		struct Line
		{
        	int type;
			float4x4 transform;
			int operation;
        	half blend;
			half2 radius;
			int pointStartIndex;
			int pointsCount;
		};

		struct Point
		{
			float3 position;
		};

        int _MaxSteps;
		half _MaxDistance;
        int _ShadowMaxSteps;
        half _ShadowMaxDistance;
		StructuredBuffer<Line> _LineBuffer;
		StructuredBuffer<Point> _PointBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int hitCount = 0;
		int hitIds[RAY_MAX_HITS];
		half4 baseColor;

		inline void GetPoints(Line entity, out float3 points[MAX_POINTS])
		{
			for (int i = entity.pointStartIndex; i < entity.pointStartIndex + entity.pointsCount; i++)
		       points[i - entity.pointStartIndex] = _PointBuffer[i].position;
		}
		
		inline float Map(const float3 pos)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount; i++)
			{
				Line entity = _LineBuffer[hitIds[i]];
				float3 p = ApplyMatrix(pos, entity.transform);
				
				float3 scale = GetScale(entity.transform);
		        float scaleFactor = min(scale.x, min(scale.y, scale.z));

				float3 points[MAX_POINTS];
				GetPoints(entity, points);
				float2 sdf = GetLineSdf(p, entity.type, points);
				sdf.x /= scaleFactor;
				float dist = ThickLine(sdf.x, sdf.y, entity.radius.x, entity.radius.y);
				float blend = 0;
				totalDist = Combine(totalDist, dist, 0, 0.1, blend);
			}
			return totalDist;
		}

		inline float NormalMap(const float3 pos)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount; i++)
			{
				Line entity = _LineBuffer[hitIds[i]];
				float3 p = ApplyMatrix(pos, entity.transform);
				
				float3 scale = GetScale(entity.transform);
		        float scaleFactor = min(scale.x, min(scale.y, scale.z));

				float3 points[MAX_POINTS];
				GetPoints(entity, points);
				float2 sdf = GetLineSdf(p, entity.type, points);
				sdf.x /= scaleFactor;
				float dist = ThickLine(sdf.x, sdf.y, entity.radius.x, entity.radius.y);
				float blend = 0;
				totalDist = Combine(totalDist, dist, 0, 0.1, blend);
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

			#pragma multi_compile_fragment _ DEBUG_MODE
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LineLitForwardPass.hlsl"
            ENDHLSL
		}

//		Pass
//		{
//			Name "Shadow Caster"
//			Tags
//			{
//				"LightMode" = "ShadowCaster"
//			}
//
//			ZWrite On
//			ZTest LEqual
//			ColorMask 0
//			Cull [_Cull]
//
//			HLSLPROGRAM
//			#pragma target 2.0
//			#pragma multi_compile_instancing
//			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//			
//			#pragma multi_compile _ LOD_FADE_CROSSFADE
//			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
//			
//			#pragma vertex Vert
//		    #pragma fragment Frag
//
//			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitShadowCasterPass.hlsl"
//			ENDHLSL
//		}
    }
    FallBack "Diffuse"
}
