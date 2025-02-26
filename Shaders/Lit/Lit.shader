	Shader "Rayman/Lit"
{
    Properties
    {
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_GradientScaleY("Gradient Scale Y", Range(0.5, 5.0)) = 1.0
    	_GradientOffsetY("Gradient Offset Y", Range(0.0, 1.0)) = 0.5
    	_GradientAngle("Gradient Angle", Float) = 0.0
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
		#include "Packages/com.davidkimighty.rayman/Shaders/Shared/Shape.hlsl"
        
		struct Shape
		{
        	int type;
			float4x4 transform;
			half3 size;
        	half3 pivot;
        	int operation;
        	float blend;
			float roundness;
			half4 color;
#ifdef GRADIENT_COLOR
			half4 gradientColor;
#endif
		};

        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		float _GradientScaleY;
		float _GradientOffsetY;
		float _GradientAngle;
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];
		half4 baseColor;

		inline float2 CombineDistance(float3 posWS, Shape shape, float totalDist)
		{
			float3 posOS = mul(shape.transform, float4(posWS, 1.0)).xyz;
			posOS -= GetPivotOffset(shape.type, shape.pivot, shape.size);
			
			float3 scale = GetScale(shape.transform);
	        float scaleFactor = min(scale.x, min(scale.y, scale.z));

			float dist = GetShapeSdf(posOS, shape.type, shape.size, shape.roundness) / scaleFactor;
			return SmoothOperation(shape.operation, totalDist, dist, shape.blend);
		}
		
		float Map(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			baseColor = _ShapeBuffer[hitIds[0]].color;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float2 combined = CombineDistance(positionWS, shape, totalDist);
				totalDist = combined.x;
#ifdef GRADIENT_COLOR
				float3 posOS = mul(shape.transform, float4(positionWS, 1.0)).xyz;
				float2 uv = (posOS.xy - 0.5 + _GradientOffsetY) * _GradientScaleY + 0.5;
				uv = GetRotatedUV(uv, float2(0.5, 0.5), radians(_GradientAngle));
				uv.y = 1.0 - uv.y;
				uv = saturate(uv);
				half4 color = lerp(shape.color, shape.gradientColor, uv.y);
#else
				half4 color = shape.color;
#endif
				baseColor = lerp(baseColor, color, combined.y);
			}
			return totalDist;
		}

		float NormalMap(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
				totalDist = CombineDistance(positionWS, _ShapeBuffer[hitIds[i]], totalDist).x;
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

//	    Pass
//		{
//			Name "Depth Only"
//		    Tags { "LightMode" = "DepthOnly" }
//
//		    ZTest LEqual
//		    ZWrite On
//		    Cull [_Cull]
//
//		    HLSLPROGRAM
//		    #pragma target 2.0
//		    
//		    #pragma shader_feature_local _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//		    #pragma multi_compile _ LOD_FADE_CROSSFADE
//		    
//		    #pragma multi_compile_instancing
//		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//
//		    #pragma vertex Vert
//		    #pragma fragment Frag
//		    
//			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitDepthOnlyPass.hlsl"
//		    ENDHLSL
//		}
//
//       Pass
//       {
//       		Name "Depth Normals"
//		    Tags { "LightMode" = "DepthNormals" }
//
//		    ZWrite On
//		    Cull [_Cull]
//
//		    HLSLPROGRAM
//		    #pragma target 2.0
//		    
//		    #pragma multi_compile _ LOD_FADE_CROSSFADE
//		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
//		    #pragma multi_compile_instancing
//		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//
//			#pragma vertex Vert
//		    #pragma fragment Frag
//
//			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitDepthNormalsPass.hlsl"
//		    ENDHLSL
//       }

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
