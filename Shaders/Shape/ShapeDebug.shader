Shader "Rayman/ShapeDebug"
{
    Properties
    {
    	[Header(Debug)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_DebugMode("Debug Mode", Int) = 0
    	_BoundsDisplayThreshold("Bounds Display Threshold", Int) = 300

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
			
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

			#pragma multi_compile_fragment _ _SHAPE_GROUP
			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeSurface.hlsl"
			
			#pragma vertex Vert
            #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeDebugForwardPass.hlsl"
            ENDHLSL
		}
    }
    FallBack "Diffuse"
}
