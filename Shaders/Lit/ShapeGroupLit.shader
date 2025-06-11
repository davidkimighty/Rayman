Shader "Rayman/ShapeGroupLit"
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
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.1)) = 0.006
    	
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
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Shared/Shape.hlsl"

		struct ShapeGroup
		{
			int operation;
        	float blend;
			int startIndex;
			int count;
		};
		
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
			half4 color;
#ifdef GRADIENT_COLOR
			half4 gradientColor;
#endif
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
		float _GradientAngle;
		CBUFFER_END
		
		StructuredBuffer<ShapeGroup> _ShapeGroupBuffer;
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount;
		int hitIds[RAY_MAX_HITS];
		half4 baseColor;

		inline float2 CombineDistance(Shape shape, float3 localPos, float totalDist)
		{
			localPos = RotateWithQuaternion(localPos, shape.rotation);
			localPos /= shape.scale;
			localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);

			float uniformScale = max(max(shape.scale.x, shape.scale.y), shape.scale.z);
			float dist = GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
			return SmoothOperation(shape.operation, totalDist, dist, shape.blend);
		}

#if defined(GRADIENT_COLOR)
		inline half4 GetShapeGradientColor(Shape shape, float3 localPos)
		{
			float2 uv = (localPos.xy - 0.5 + _GradientOffsetY) * _GradientScaleY + 0.5;
			uv = GetRotatedUV(uv, float2(0.5, 0.5), radians(_GradientAngle));
			uv.y = 1.0 - uv.y;
			uv = saturate(uv);
			return lerp(shape.color, shape.gradientColor, uv.y);
		}
#endif
		
		float Map(inout Ray ray)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				float localDist = _MaxDistance;
				ShapeGroup group = _ShapeGroupBuffer[hitIds[i]];
				int index = group.startIndex;
				half4 localColor = _ShapeBuffer[index].color;
				
				for (int j = 0; j < group.count; j++)
				{
					Shape shape = _ShapeBuffer[index++];
					float3 localPos = ray.hitPoint - shape.position;
					float2 combined = CombineDistance(shape, localPos, localDist);
					localDist = combined.x;
#ifdef GRADIENT_COLOR
					half4 color = GetShapeGradientColor(shape, localPos);
#else
					half4 color = shape.color;
#endif
					localColor = lerp(localColor, color, combined.y);
				}
				float2 combinedTotal = SmoothOperation(group.operation, totalDist, localDist, group.blend);
				totalDist = combinedTotal.x;
				baseColor = lerp(baseColor, localColor, combinedTotal.y);
			}
			return totalDist;
		}

		float NormalMap(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				float localDist = _MaxDistance;
				ShapeGroup group = _ShapeGroupBuffer[hitIds[i]];
				int index = group.startIndex;
				
				for (int j = 0; j < group.count; j++)
				{
					Shape shape = _ShapeBuffer[index++];
					float3 localPos = positionWS - shape.position;
					localDist = CombineDistance(shape, localPos, localDist).x;
				}
				totalDist = SmoothOperation(group.operation, totalDist, localDist, group.blend).x;
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
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
		    
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

			#pragma multi_compile_fragment _ DEBUG_MODE
			#pragma multi_compile_fragment _ GRADIENT_COLOR
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitForwardPass.hlsl"
            ENDHLSL
		}

       Pass
       {
       		Name "Depth Normals"
		    Tags { "LightMode" = "DepthNormals" }

		    ZWrite On
		    Cull [_Cull]

		    HLSLPROGRAM
		    #pragma target 2.0
		    
		    #pragma multi_compile _ LOD_FADE_CROSSFADE
		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #pragma multi_compile_instancing
		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitDepthNormalsPass.hlsl"
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

			#pragma multi_compile_fragment _ GRADIENT_COLOR
			
			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
