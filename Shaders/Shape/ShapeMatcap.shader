Shader "Rayman/ShapeMatcap"
{
    Properties
    {
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_FresnelColor ("Fresnel Color", Color) = (0.3, 0.3, 0.3, 1)
    	_FresnelPow ("Fresnel Power", Float) = 0.3
    	
    	[Header(Raymarching)][Space]
    	_EpsilonMin("Epsilon Min", Float) = 0.001
    	_EpsilonMax("Epsilon Max", Float) = 0.01
    	_MaxSteps("Max Steps", Int) = 64
    	_MaxDistance("Max Distance", Float) = 100.0
    	
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
			
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeSurface.hlsl"
			
			#pragma vertex Vert
            #pragma fragment Frag
			#include "Packages/com.davidkimighty.rayman/Shaders/Shape/ShapeMatcapForwardPass.hlsl"
            ENDHLSL
		}
    }
    FallBack "Diffuse"
}
