	Shader "Rayman/Unlit"
{
    Properties
    {
        [Header(Shade)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.01)) = 0.006
    	
    	[Header(Raymarching)][Space]
    	_EpsilonMin("Epsilon Min", Float) = 0.001
    	_EpsilonMax("Epsilon Max", Float) = 0.01
    	_MaxSteps("Max Steps", Int) = 64
    	_MaxDistance("Max Distance", Float) = 100.0
    	_ShadowMaxSteps("Shadow Max Steps", Int) = 16
    	_ShadowMaxDistance("Shadow Max Distance", Float) = 30.0
    	
    	[Header(Blending)][Space]
    	[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend", Float) = 5.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend ", Float) = 10.0
	    [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2.0
	    [Toggle][KeyEnum(Off, On)] _ZWrite("ZWrite", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
        	"RenderType" = "Transparent"
        	"Queue" = "Transparent"
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
		};

		CBUFFER_START(RaymarchPerGroup)
		float _EpsilonMin;
		float _EpsilonMax;
        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		CBUFFER_END
		
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
		
		inline float Map(inout Ray ray)
		{
			float totalDist = _MaxDistance;
			baseColor = _ShapeBuffer[hitIds[0]].color;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float2 combined = CombineDistance(ray.hitPoint, shape, totalDist);
				totalDist = combined.x;
				baseColor = lerp(baseColor, shape.color, combined.y);
			}
			return totalDist;
		}

		inline float NormalMap(const float3 positionWS)
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

            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Unlit/UnlitForwardPass.hlsl"
            ENDHLSL
		}
    }
    FallBack "Diffuse"
}
