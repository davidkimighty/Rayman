Shader "Rayman/TextureUnlit"
{
    Properties
    {
        [Header(Shade)][Space]
    	_MainTex ("Main Texture", 2D) = "white" {}
    	_FresnelColor ("Fresnel Color", Color) = (0.3, 0.3, 0.3, 1)
    	_FresnelPow ("Fresnel Power", Float) = 0.3
    	
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
        	"UniversalMaterialType" = "Unlit"
        	"IgnoreProjector" = "True"
        	"DisableBatching" = "True"
        }
        LOD 100
        
        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Shared/Shape.hlsl"
        
		struct Shape
		{
        	int type;
			float3 position;
			float4 rotation;
			float3 scale;
			half3 size;
        	half3 pivot;
        	int operation;
        	half blend;
			half roundness;
		};

		float _EpsilonMin;
		float _EpsilonMax;
        int _MaxSteps;
		float _MaxDistance;
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];

		inline float2 CombineDistance(Shape shape, float3 localPos, float totalDist)
		{
			localPos = RotateWithQuaternion(localPos, shape.rotation);
			localPos /= shape.scale;
			localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);
			
			float dist = GetShapeSdf(localPos, shape.type, shape.size, shape.roundness);
			return SmoothOperation(shape.operation, totalDist, dist, shape.blend);
		}

		inline float Map(inout Ray ray)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 localPos = ray.hitPoint - shape.position;
				totalDist = CombineDistance(shape, localPos, totalDist).x;
			}
			return totalDist;
		}

		inline float NormalMap(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 localPos = positionWS - shape.position;
				totalDist = CombineDistance(shape, localPos, totalDist).x;
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

			#include "Packages/com.davidkimighty.rayman/Shaders/Unlit/TextureUnlitForwardPass.hlsl"
            ENDHLSL
		}
    }
    FallBack "Diffuse"
}
