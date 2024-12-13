Shader "Rayman/RaymarchShapeCustomTemplate"
{
    Properties
    {
        [Header(Sphere)][Space]
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
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/SDF.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
        
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Lighting.hlsl"

        half4 _Color;

        inline float Map(const Ray ray)
		{
			_Color = half4(0.9, 0.6, 0.3, 1.0);
        	float s = length(ToObject(ray.hitPoint)) - 0.4;
        	float d = sin(7.1 * ray.hitPoint.x) * sin(6.2 * ray.hitPoint.y) * sin(0.1 * ray.hitPoint.z);
			return s + d;
		}

		inline float NormalMap(const float3 rayPos)
		{
			float s = length(ToObject(rayPos)) - 0.4;
        	float d = sin(7.1 * rayPos.x) * sin(6.2 * rayPos.y) * sin(0.1 * rayPos.z);
			return s + d;
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

			#pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
			
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
			
			struct Attributes
			{
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			    float4 tangent : TANGENT;
			    float2 texcoord : TEXCOORD0;
			    float2 lightmapUV : TEXCOORD1;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
			    float4 posCS : SV_POSITION;
			    float4 posSS : TEXCOORD0;
			    float3 posWS : TEXCOORD1;
			    float3 normalWS : TEXCOORD2;
			    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
			    half4 fogFactorAndVertexLight : TEXCOORD4;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			    UNITY_VERTEX_OUTPUT_STEREO
			};

			struct FragOutput
			{
			    float4 color : SV_Target;
			    float depth : SV_Depth;
			};

			int _MaxSteps;
			float _MaxDist;

			Varyings Vert (Attributes input)
			{
			    Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
				output.posCS = vertexInput.positionCS;
				output.posWS = vertexInput.positionWS;
				output.normalWS = TransformObjectToWorldNormal(input.normal);

				half3 vertexLight = VertexLighting(output.posWS, output.normalWS);
				half fogFactor = ComputeFogFactor(output.posCS.z);
				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

				OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
				OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
			    return output;
			}

			FragOutput Frag (Varyings input)
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 rayDir = normalize(input.posWS - GetCameraPosition());
				Ray ray = CreateRay(input.posWS, rayDir, _MaxSteps, _MaxDist);
			    if (!Raymarch(ray)) discard;
				
				const float3 normal = GetNormal(ray.hitPoint);
				const float depth = GetDepth(input.posWS);
				const float3 viewDir = normalize(GetCameraPosition() - ray.hitPoint);
				const float fresnel = GetFresnelSchlick(viewDir, normal);
				
				half3 shade = MainLightShade(ray.hitPoint, ray.dir, normal, fresnel);
				AdditionalLightsShade(ray.hitPoint, ray.dir, normal, fresnel, shade);
				shade += RimLightShade(normal, viewDir);
				
				_Color.rgb *= shade + SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
				_Color.rgb = MixFog(_Color.rgb, input.fogFactorAndVertexLight.x);
				
				FragOutput output;
				output.color = _Color;
				output.depth = 0;
				return output;
			}
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

			#pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			#pragma multi_compile_instancing

			#pragma multi_compile _ LOD_FADE_CROSSFADE
			
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			struct Attributes
			{
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
			    float4 posCS : SV_POSITION;
			    float3 posWS : TEXCOORD0;
			    float3 normalWS : TEXCOORD1;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			    UNITY_VERTEX_OUTPUT_STEREO
			};

			struct FragOut
			{
			    float4 color : SV_Target;
			    float depth : SV_Depth;
			};

			Varyings Vert(Attributes input)
			{
			    Varyings o;
			    UNITY_SETUP_INSTANCE_ID(i);
			    UNITY_TRANSFER_INSTANCE_ID(i, o);
			    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			    o.posCS = TransformObjectToHClip(input.vertex.xyz);
			    o.posWS = TransformObjectToWorld(input.vertex.xyz);
			    o.normalWS = TransformObjectToWorldNormal(input.normal);
			    return o;
			}
						
			FragOut Frag(Varyings input)
			{
			    UNITY_SETUP_INSTANCE_ID(i);
			    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

			    Ray ray = CreateRay(input.posWS, GetCameraForward(), 32, 100);
				if (!Raymarch(ray)) discard;
							
			    FragOut o;
			    o.color = o.depth = GetDepth(ray.hitPoint);
			    return o;
			}
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
