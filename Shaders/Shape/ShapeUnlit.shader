Shader "Rayman/ShapeUnlit"
{
    Properties
    {
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_OutlineThickness ("Outline Thickness", Float) = 0.01
    	_OutlineColor ("Outline Color", Color) = (1, 0.83, 0.0, 1)
    	_FresnelPow ("Fresnel Power", Float) = 6.3
    	_GradientScaleY("Gradient Scale Y", Range(0.01, 3.0)) = 1.0
    	_GradientOffsetY("Gradient Offset Y", Range(-1.0, 1.0)) = 0.0
    	_GradientAngle("Gradient Angle", Float) = 0.0
    	
    	[Header(Raymarching)][Space]
    	_EpsilonMin("Epsilon Min", Float) = 0.001
    	_EpsilonMax("Epsilon Max", Float) = 0.01
    	_MaxSteps("Max Steps", Int) = 64
    	_MaxDistance("Max Distance", Float) = 100.0
    	_DepthOnlyMaxSteps("DepthOnly Max Steps", Int) = 32
    	_DepthOnlyMaxDistance("DepthOnly Max Distance", Float) = 100.0
    	
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
		    
			#pragma multi_compile_fragment _ _SHAPE_GROUP
			#pragma multi_compile_fragment _ _GRADIENT_COLOR
			
			#define SHAPE_BLENDING
			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeSurface.hlsl"
			
			#pragma vertex Vert
            #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeUnlitForwardPass.hlsl"
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
		    #pragma target 2.0

		    #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		    
		    #pragma multi_compile _ LOD_FADE_CROSSFADE
		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #pragma multi_compile_instancing
		    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

		    #pragma multi_compile_fragment _ _SHAPE_GROUP
		    #include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeSurface.hlsl"
		    
			#pragma vertex Vert
		    #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeDepthOnlyPass.hlsl"
		    ENDHLSL
        }
    }
    FallBack "Diffuse"
}
