	Shader "Rayman/LineLit"
{
    Properties
    {
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_GradientScaleY("Gradient Scale Y", Range(0.5, 5.0)) = 1.0
    	_GradientOffsetY("Gradient Offset Y", Range(0.0, 1.0)) = 0.5
    	_Metallic("Metallic", Range(0.0, 1.0)) = 0
    	_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
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
			half2 padding;
			int pointStartIndex;
			int pointsCount;
			half4 color;
#ifdef GRADIENT_COLOR
			half4 gradientColor;
#endif
		};

		struct Point
		{
			float3 position;
		};

        CBUFFER_START(RaymarchPerGroup)
		float _EpsilonMin;
		float _EpsilonMax;
        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		float _GradientScaleY;
		float _GradientOffsetY;
		CBUFFER_END
		
		StructuredBuffer<Line> _LineBuffer;
		StructuredBuffer<Point> _PointBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount = 0;
		int hitIds[RAY_MAX_HITS];
		half4 baseColor;

		float3 CombineDistance(float3 posWS, Line entity, float totalDist)
		{
			float3 posOS = mul(entity.transform, float4(posWS, 1.0)).xyz;
			float3 scale = GetScale(entity.transform);
	        float scaleFactor = min(scale.x, min(scale.y, scale.z));

			float3 points[MAX_POINTS];
			for (int i = entity.pointStartIndex; i < entity.pointStartIndex + entity.pointsCount; i++)
		       points[i - entity.pointStartIndex] = _PointBuffer[i].position;

			float2 lineSdf = GetLineSdf(posOS, entity.type, points);
			float dist = ThickLine(lineSdf.x / scaleFactor, lineSdf.y, entity.radius.x, entity.radius.y);
			return float3(SmoothOperation(entity.operation, totalDist, dist, entity.blend), lineSdf.y);
		}
		
		float Map(inout Ray ray)
		{
			float totalDist = _MaxDistance;
			baseColor = _LineBuffer[hitIds[0]].color;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Line entity = _LineBuffer[hitIds[i]];
				float3 combined = CombineDistance(ray.hitPoint, entity, totalDist);
				totalDist = combined.x;
#ifdef GRADIENT_COLOR
				float blend = saturate((combined.z + _GradientOffsetY - 1.0) * _GradientScaleY + 0.5);
				half4 color = lerp(entity.color, entity.gradientColor, blend);
#else
				half4 color = entity.color;
#endif
				baseColor = lerp(baseColor, color, combined.y);
			}
			return totalDist;
		}

		float NormalMap(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
				totalDist = CombineDistance(positionWS, _LineBuffer[hitIds[i]], totalDist).x;
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

			#pragma multi_compile_fragment _ DEBUG_MODE
			#pragma multi_compile_fragment _ GRADIENT_COLOR
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitForwardPass.hlsl"
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
