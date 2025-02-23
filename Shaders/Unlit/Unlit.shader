	Shader "Rayman/Unlit"
{
    Properties
    {
        [Header(Shade)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.01)) = 0.006
    	
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
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
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
        	float blend;
			float roundness;
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

            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE
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
