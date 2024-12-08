Shader "Rayman/RaymarchShape"
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
    	_MaxDist ("MaxDist", Float) = 100.0
    	
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
        }
        LOD 200
        
        HLSLINCLUDE
        #pragma shader_feature _OPERATION_FEATURE
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
		
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
		
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"

		struct Shape
		{
			float4x4 transform;
        	float3 lossyScale;
			int type;
			float3 size;
			float roundness;
			int combination;
			float smoothness;
			half4 color;
			half4 emissionColor;
			float emissionIntensity;
			int operationEnabled;
		};

		struct Operation
		{
			int id;
			int type;
			float amount;
		};

        int _MaxSteps;
		float _MaxDist;
		int _ShapeCount;
        int _OperationCount;
		StructuredBuffer<Shape> _ShapeBuffer;
		StructuredBuffer<Operation> _OperationBuffer;
        float4 _Color;
		
		inline void ApplyOperationPositionById(inout float3 pos, const int id)
		{
			for (int i = 0; i < _OperationCount; i++)
			{
				Operation o = _OperationBuffer[i];
				if (o.id != id) continue;
		                
				pos = ApplyOperation(pos, o.type, o.amount);
				break;
			}
		}

		inline float BlendDistance(inout float totalDist, const float3 pos, const Shape shape)
		{
			float dist = GetShapeSDF(pos, shape.type, shape.size, shape.roundness);
			float blend = 0;
			totalDist = CombineShapes(totalDist, dist, shape.combination, shape.smoothness, blend);
			return blend;
		}

		inline float Map(inout Ray ray)
		{
			float totalDist = _MaxDist;
			_Color = _ShapeBuffer[0].color;
			for (int i = 0; i < _ShapeCount; i++)
			{
				Shape shape = _ShapeBuffer[i];
				float3 pos = NormalizeScale(ApplyMatrix(ray.hitPoint, shape.transform), shape.lossyScale);
#ifdef _OPERATION_FEATURE
				if (shape.operationEnabled > 0)
					ApplyOperationPositionById(pos, i);
#endif
				float blend = BlendDistance(totalDist, pos, shape);
				_Color = lerp(_Color, shape.color + shape.emissionColor * shape.emissionIntensity, blend);
			}
			return totalDist;
		}

		inline float NormalMap(const float3 rayPos)
		{
			float totalDist = _MaxDist;
			for (int i = 0; i < _ShapeCount; i++)
			{
				Shape shape = _ShapeBuffer[i];
				float3 pos = NormalizeScale(ApplyMatrix(rayPos, shape.transform), shape.lossyScale);
#ifdef _OPERATION_FEATURE
				if (shape.operationEnabled > 0)
					ApplyOperationPositionById(pos, i);
#endif
				BlendDistance(totalDist, pos, shape);
			}
			return totalDist;
		}
        ENDHLSL

        Pass
		{
			Name "Forward Lit"
			Tags
			{
				"LightMode" = "UniversalForward"
			}
			
			Blend [_SrcBlend] [_DstBlend]
		    ZWrite [_ZWrite]
		    Cull [_Cull]
		    
			HLSLPROGRAM
			#pragma target 2.0
			
			#pragma vertex Vert
            #pragma fragment Frag
			
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
            #pragma instancing_options renderinglayer

			#include "Packages/com.davidkimighty.rayman/Shaders/ShapeForwardLit.hlsl"
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

		    #pragma vertex Vert
		    #pragma fragment Frag

			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "Packages/com.davidkimighty.rayman/Shaders/ShapeShadowCaster.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
