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
			
			#pragma vertex vert
            #pragma fragment frag
			//
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   //          #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   //          #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
   //          #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   //          #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
   //          #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
   //          #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   //          #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   //          #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
   //          #pragma multi_compile_fragment _ _LIGHT_COOKIES
   //          #pragma multi_compile _ _LIGHT_LAYERS
   //          #pragma multi_compile _ _FORWARD_PLUS
   //
		 //    #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
   //          #pragma multi_compile _ SHADOWS_SHADOWMASK
   //          #pragma multi_compile _ DIRLIGHTMAP_COMBINED
   //          #pragma multi_compile _ LIGHTMAP_ON
   //          #pragma multi_compile _ DYNAMICLIGHTMAP_ON
   //          #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
   //          #pragma multi_compile _ LOD_FADE_CROSSFADE
   //          #pragma multi_compile_fog
   //          #pragma multi_compile_fragment _ DEBUG_DISPLAY
		 //    
            // #pragma multi_compile_instancing
            // #pragma instancing_options renderinglayer

			//#include "Packages/com.davidkimighty.rayman/Shaders/RaymarchForwardLit.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			
			struct appdata
			{
			    float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			    float2 uv : TEXCOORD0;
			};

			struct output
			{
			    float4 color : SV_Target;
			    float depth : SV_Depth;
			};

			struct RaymarchResult
			{
			    float3 hitPoint;
			    float travelDistance;
			};

			StructuredBuffer<RaymarchResult> _ResultBuffer;

			inline float GetD(const float3 posWS)
			{
			    float4 csPos = TransformWorldToHClip(posWS);
			    float z = csPos.z / csPos.w;
			    return z;
			}
			
			inline float GetDepth(const float td, const float3 wsPos)
			{
			    float lengthToSurface = length(wsPos - UNITY_MATRIX_I_V._m03_m13_m23);
			    return td - lengthToSurface < 0.001 ? GetD(wsPos) : GetD(td);
			}

			v2f vert (appdata v)
			{
			    v2f o = (v2f)0;
				o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
			    return o;
			}

			output frag (v2f i)
			{
				// UNITY_SETUP_INSTANCE_ID(i);
				// UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				uint2 pixelCoord = uint2(i.vertex.xy * _ScreenParams.xy);
				RaymarchResult result = _ResultBuffer[pixelCoord.x + pixelCoord.y * _ScreenParams.x];

				float4 color = float4(result.travelDistance, 0, 0, 1);
				
				// float2 screenResolution = _ScreenParams.xy;
				// float2 uv = (2 * i.vertex.xy - screenResolution)/screenResolution.y;
				//
				// uint2 pixelIndex = uint2(uv * screenResolution);
			 //    RaymarchResult result = _ResultBuffer[pixelIndex];
				if (result.travelDistance < 0)
				{
					discard;
				}

				
				float depth = GetDepth(result.travelDistance, i.vertex);

				output o;
				o.color = color;
				o.depth = depth;
				return o;
			}
            ENDHLSL
		}

//		Pass
//		{
//			Name "Shadow Caster"
//			Tags
//			{
//				"LightMode" = "ShadowCaster"
//			}
//
//			ZWrite On
//			ZTest LEqual
//			ColorMask 0
//			Cull [_Cull]
//
//			HLSLPROGRAM
//			#pragma target 2.0
//
//		    #pragma vertex Vert
//		    #pragma fragment Frag
//
//			#pragma shader_feature_local _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//
//			#pragma multi_compile_instancing
//			#pragma multi_compile _ LOD_FADE_CROSSFADE
//			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
//
//			#include "Packages/com.davidkimighty.rayman/Shaders/RaymarchShadowCaster.hlsl"
//			ENDHLSL
//		}
    }
    FallBack "Diffuse"
}
