Shader "Rayman/RaymarchTextureUnlit"
{
    Properties
    {
        [Header(Shade)][Space]
    	_MainTex ("Main Texture", 2D) = "white" {}
    	_FresnelColor ("Fresnel Color", Color) = (0.3, 0.3, 0.3, 1)
    	_FresnelPow ("Fresnel Power", Float) = 0.3
    	
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
        	"UniversalMaterialType" = "Unlit"
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
		};

        int _MaxSteps;
		float _MaxDistance;
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];

		inline float GetDst(const float3 pos)
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
				totalDist = CombineShapes(totalDist, dist, shape.operation, shape.smoothness, blend);
			}
			return totalDist;
		}

		inline float Map(const float3 pos)
		{
			return GetDst(pos);
		}

		inline float NormalMap(const float3 pos)
		{
			return GetDst(pos);
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

            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _FORWARD_PLUS

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
		    
            #pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma instancing_options renderinglayer
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/RaymarchTextureUnlitForward.hlsl"
            ENDHLSL
		}
    }
    FallBack "Diffuse"
}
