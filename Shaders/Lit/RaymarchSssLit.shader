Shader "Rayman/RaymarchSssLit"
{
    Properties
    {
        [Header(Shade)][Space]
    	_ShadowBiasVal ("Shadow Bias", Float) = 0.006
    	_F0 ("Schlick F0", Float) = 0.04
    	_Roughness ("Roughness", Float) = 0.5
    	_SssDistortion ("SSS Distortion", Float) = 0.1
    	_SssPower ("SSS Power", Float) = 1.0
    	_SssScale ("SSS Scale", Float) = 0.5
    	_SssAmbient ("SSS Ambient", Float) = 0.1
    	
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
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Sdf.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Distortion.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"
        
		struct Shape
		{
        	int type;
			float4x4 transform;
			float3 size;
        	float3 pivot;
        	int operation;
        	float smoothness;
			float roundness;
			float4 color;
			float4 emissionColor;
			float emissionIntensity;
		};

        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];
		float4 finalColor;

		inline void ApplyShapeDistance(float3 pos, Shape shape, inout float totalDist, out float blend)
		{
			float3 p = ApplyMatrix(pos, shape.transform);
			p -= GetPivotOffset(shape.type, shape.pivot, shape.size);
			
			float3 scale = GetScale(shape.transform);
	        float scaleFactor = min(scale.x, min(scale.y, scale.z));

			float dist = GetShapeSdf(p, shape.type, shape.size, shape.roundness) / scaleFactor;
			blend = 0;
			totalDist = CombineShapes(totalDist, dist, shape.operation, shape.smoothness, blend);
		}

		inline float Map(const float3 pos)
		{
			float totalDist = _MaxDistance;
			finalColor = _ShapeBuffer[hitIds[0]].color;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float blend = 0;
				ApplyShapeDistance(pos, shape, totalDist, blend);
				finalColor = lerp(finalColor, shape.color + shape.emissionColor * shape.emissionIntensity, blend);
			}
			return totalDist;
		}

		inline float NormalMap(const float3 pos)
		{
			float totalDist = _MaxDistance;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float blend = 0;
				ApplyShapeDistance(pos, shape, totalDist, blend);
			}
			return totalDist;
		}

		inline float ShadowMap(const float3 pos)
		{
			float totalDist = _MaxDistance;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float blend = 0;
				ApplyShapeDistance(pos, shape, totalDist, blend);
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

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/RaymarchSssLitForward.hlsl"
            ENDHLSL
		}

	    Pass
		{
			Name "Depth Only"
		    Tags { "LightMode" = "DepthOnly" }

		    ZTest LEqual
		    ZWrite On
		    Cull [_Cull]

		    HLSLPROGRAM
		    #pragma target 5.0
		    #pragma shader_feature _ALPHATEST_ON
		    #pragma multi_compile_instancing

		    #pragma vertex Vert
		    #pragma fragment Frag
		    
			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/RaymarchLitDepthOnly.hlsl"
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

			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/RaymarchLitDepthNormals.hlsl"
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
