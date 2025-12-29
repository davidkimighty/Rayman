Shader "Rayman/ShapeChrome"
{
    Properties
    {
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    	_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
    	_GradientScaleY("Gradient Scale Y", Range(0.01, 3.0)) = 1.0
    	_GradientOffsetY("Gradient Offset Y", Range(-1.0, 1.0)) = 0.0
    	_GradientAngle("Gradient Angle", Float) = 0.0
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.1)) = 0.006
    	
    	[Header(Chrome)][Space]
    	_SpecularPower ("Specular Power", Float) = 5.0
    	_SpecularIntensity ("Specular Intensity", Float) = 1.0
    	_FresnelPower ("Fresnel Power", Float) = 2.0
    	_GISharpness ("GI Sharpness", Float) = 2.0
    	_GIIntensity ("GI Intensity", Float) = 1.0
    	_EnvPower ("Env Power", Float) = 1.0
    	_EnvIntensity ("Env Intensity", Float) = 2.0
    	
    	[Header(Raymarching)][Space]
    	_EpsilonMin("Epsilon Min", Float) = 0.001
    	_EpsilonMax("Epsilon Max", Float) = 0.01
    	_MaxSteps("Max Steps", Int) = 64
    	_MaxDistance("Max Distance", Float) = 100.0
    	_DepthNormalMaxSteps("DepthNormal Max Steps", Int) = 16
    	_DepthNormalMaxDistance("DepthNormal Max Distance", Float) = 100.0
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
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
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
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
		    
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile_fragment _ _SHAPE_GROUP
			#pragma multi_compile_fragment _ _GRADIENT_COLOR
			
			#define SHAPE_BLENDING
			#include "Packages/com.davidkimighty.rayman/Shaders/Sdf/ShapeSurface.hlsl"
			
			#pragma vertex Vert
            #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Sdf/ShapeChromeForwardPass.hlsl"
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

		    #pragma multi_compile_fragment _ _SHAPE_GROUP
		    #include "Packages/com.davidkimighty.rayman/Shaders/Sdf/ShapeSurface.hlsl"
		    
			#pragma vertex Vert
		    #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Sdf/SdfDepthNormalPass.hlsl"
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

			#pragma multi_compile_fragment _ _SHAPE_GROUP
			#include "Packages/com.davidkimighty.rayman/Shaders/Sdf/ShapeSurface.hlsl"
			
			#pragma vertex Vert
		    #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Sdf/SdfShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
