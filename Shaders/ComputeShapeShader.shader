Shader "Rayman/RaymarchShapeCs"
{
    Properties
    {
        [Header(Shade)][Space]
    	_F0 ("Fresnel F0", Float) = 0.4
    	_SpecularPow ("Specular Power", Float) = 10.0
    	_RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1)
    	_RimPow ("Rim Power", Float) = 0.1
    	_ShadowBiasVal ("Shadow Bias", Float) = 0.015
    	
    	[Header(Blending)][Space]
    	[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("SrcBlend", Float) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("DstBlend ", Float) = 0.0
	    [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2.0
    }
    SubShader
    {
        Tags
        {
        	"RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        	"UniversalMaterialType" = "Unlit"
        	"Queue"="AlphaTest"
        	"DisableBatching"="False"
        }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
        
        struct RaymarchResult
		{
		    float3 hitPoint;
		    float travelDistance;
			float lastHitDistance;
			float3 rayDirection;
        	float4 color;
			float3 normal;
		};

		StructuredBuffer<RaymarchResult> resultBuffer;
        ENDHLSL

        Pass
		{
			Name "Forward Lit"
			
			Blend [_SrcBlend] [_DstBlend]
		    ZWrite On
		    Cull [_Cull]
		    AlphaToMask On
		    
			HLSLPROGRAM
			#pragma target 2.0

			#pragma multi_compile_instancing
	        #pragma multi_compile_fog
	        #pragma instancing_options renderinglayer

			#pragma multi_compile _ LIGHTMAP_ON
	        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
	        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
	        #pragma shader_feature _ _SAMPLE_GI
	        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
	        #pragma multi_compile_fragment _ DEBUG_DISPLAY
	        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

			#define ATTRIBUTES_NEED_NORMAL
	        #define ATTRIBUTES_NEED_TANGENT
	        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
	        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
	        #define VARYINGS_NEED_POSITION_WS
	        #define VARYINGS_NEED_NORMAL_WS
	        #define FEATURES_GRAPH_VERTEX

	        #define SHADERPASS SHADERPASS_UNLIT
	        #define _FOG_FRAGMENT 1
	        #define _ALPHATEST_ON 1

			#pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.davidkimighty.rayman/Shaders/ComputeShapeForwardLit.hlsl"
            ENDHLSL
		}

		Pass
		{
			Name "Depth Only"
		    Tags { "LightMode" = "DepthOnly" }

		    ZTest LEqual
		    ZWrite On
		    ColorMask R
		    Cull [_Cull]

		    HLSLPROGRAM
		    #pragma target 2.0
		    #pragma multi_compile_instancing

		    #pragma vertex vert
		    #pragma fragment frag
		    
			#include "Packages/com.davidkimighty.rayman/Shaders/ComputeShapeDepthOnly.hlsl"
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
		    #pragma multi_compile_instancing

			#pragma vertex vert
		    #pragma fragment frag

			#include "Packages/com.davidkimighty.rayman/Shaders/ComputeShapeDepthNormals.hlsl"
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

		    #pragma vertex vert
		    #pragma fragment frag

			#include "Packages/com.davidkimighty.rayman/Shaders/ComputeShapeShadowCaster.hlsl"
			ENDHLSL
		}
    }
}
