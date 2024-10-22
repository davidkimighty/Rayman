Shader "Rayman/RaymarchSphereLit"
{
    Properties
    {
    	[Header(Sphere)][Space]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    	_F0 ("Fresnel F0", Float) = 0.4
    	_SpecularPow ("Specular Power", Float) = 1000.
    	_ShadowBiasVal ("Shadow Bias", Float) = 0.015
    	_RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1)
    	_RimPow ("Rim Power", Float) = 10.
    	
        [Header(Raymarching)][Space]
    	_MaxSteps ("MaxSteps", Int) = 64
    	_MaxDist ("MaxDist", Float) = 100.
    	
    	[Header(Pass)][Space]
    	[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc("Blend Src", Float) = 5 
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendDst("Blend Dst", Float) = 10
	    [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2
	    [Toggle][KeyEnum(Off, On)] _ZWrite("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        	"UniversalMaterialType" = "Lit"
        	"RenderType" = "Opaque"
			"Queue" = "Geometry"
        	"IgnoreProjector" = "True"
        	"DisableBatching" = "True"
        }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        
        #include "Packages/com.davidkimighty.rayman/Runtime/Shaders/Library/Raymarching.hlsl"
        #include "Packages/com.davidkimighty.rayman/Runtime/Shaders/Library/Lighting.hlsl"
        #include "Packages/com.davidkimighty.rayman/Runtime/Shaders/Library/Shadow.hlsl"
        
		float Circle(float3 pos)
		{
        	float r = 0.5;
			return length(ToObject(pos) * GetScale()) - r;
		}

        bool RaymarchSphere(inout Ray ray)
        {
	        float multiplier = 1;
			#ifdef OBJECT_SCALE
			    float3 localRayDir = normalize(mul(unity_WorldToObject, ray.dir));
			    multiplier *= length(mul(unity_ObjectToWorld, localRayDir));
			#endif
		    
		    for (int i = 0; i < ray.maxSteps; i++)
		    {
		        ray.currentDist = Circle(ray.travelledPoint) * multiplier;
		        ray.distTravelled += ray.currentDist;
		        ray.travelledPoint += ray.dir * ray.currentDist;
		        if (ray.currentDist < 0.001 || ray.distTravelled > ray.maxDist) break;
		    }
		    return ray.currentDist < 0.001;
        }

        float3 GetNormal(float3 pos)
		{
		    float epsilon = 0.001;
		    float3 x = float3(epsilon, 0, 0);
		    float3 y = float3(0, epsilon, 0);
		    float3 z = float3(0, 0, epsilon);
		    
		    float distX = Circle(pos + x) - Circle(pos - x);
		    float distY = Circle(pos + y) - Circle(pos - y);
		    float distZ = Circle(pos + z) - Circle(pos - z);
		    return normalize(float3(distX, distY, distZ));
		}
        ENDHLSL

		Pass
		{
			Name "Forward Lit"
			Tags { "LightMode" = "UniversalForward" }
			
			Blend [_BlendSrc] [_BlendDst]
		    ZWrite [_ZWrite]
		    Cull [_Cull]
		    
			HLSLPROGRAM
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
		    #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
		    #pragma shader_feature_local_fragment _ALPHATEST_ON
		    #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
		    #pragma shader_feature_local_fragment _EMISSION
		    #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
		    #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
		    #pragma shader_feature_local_fragment _CLEARCOAT_ON
		    #ifdef _CLEARCOAT_ON
		        #define _CLEARCOAT
		    #endif

		    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
		    #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
		    #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
		    #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
		    #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
		    #pragma multi_compile_fragment _ _SHADOWS_SOFT
		    #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
		    #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
		    #pragma multi_compile_fragment _ _LIGHT_LAYERS
		    #pragma multi_compile_fragment _ _LIGHT_COOKIES
		    #pragma multi_compile _ _CLUSTERED_RENDERING

		    #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
		    #pragma multi_compile _ SHADOWS_SHADOWMASK
		    #pragma multi_compile _ DIRLIGHTMAP_COMBINED
		    #pragma multi_compile _ LIGHTMAP_ON
		    #pragma multi_compile _ DYNAMICLIGHTMAP_ON
		    #pragma multi_compile_fog
		    #pragma multi_compile_fragment _ DEBUG_DISPLAY

		    #pragma multi_compile_instancing
		    #pragma instancing_options renderinglayer
		    #pragma multi_compile _ DOTS_INSTANCING_ON

		    #pragma prefer_hlslcc gles
		    #pragma exclude_renderers d3d11_9x
		    #pragma target 2.0
            
            #pragma vertex Vert
            #pragma fragment Frag

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

			struct FragOut
			{
				float4 color : SV_Target;
				float depth : SV_Depth;
			};

			half4 _Color;
			half4 _RimColor;
			float _RimPow;
			float _SpecularPow;
			float _F0;
			float _ShadowBiasVal;
			int _MaxSteps;
			float _MaxDist;

			// Schlick approximation
			float3 GetFresnelSchlick(float3 f0, float3 viewDir, float3 normalWS)
			{
			    float cosTheta = saturate(dot(normalWS, viewDir));
			    return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
			}

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

			FragOut Frag (Varyings input)
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				Ray ray = InitRay(input.posWS, _MaxSteps, _MaxDist);
			    if (!RaymarchSphere(ray)) discard;
				
				const float3 normal = GetNormal(ray.travelledPoint);
				const float depth = GetDepth(ray, input.posWS);
				const float3 viewDir = normalize(GetCameraPosition() - ray.travelledPoint);
				const float fresnel = GetFresnelSchlick(_F0, viewDir, normal);
				
				// main light
				half4 shadowCoord = TransformWorldToShadowCoord(ray.travelledPoint);
				const Light mainLight = GetMainLight(shadowCoord);
				
				const float mainDiffuse = GetDiffuse(mainLight.direction, normal);
				float mainSpecular = GetSpecular(ray.dir, mainLight.direction, normal, _SpecularPow);
				mainSpecular *= mainDiffuse * fresnel;
				
				const float normalBias = _ShadowBiasVal * max(0.0, dot(mainLight.direction, normal));
				shadowCoord.z += normalBias;
				const Light mainLightWithBias = GetMainLight(shadowCoord);
				half3 shade = mainLight.color * (mainDiffuse + mainSpecular) * mainLightWithBias.shadowAttenuation;
				
				// additional lights
				const int count = GetAdditionalLightsCount();
				for (int i = 0; i < count; ++i)
			    {
				    const Light light = GetAdditionalLight(i, ray.travelledPoint);
				    const float diffuse = GetDiffuse(light.direction, normal) * light.distanceAttenuation;
				    float specular = GetSpecular(ray.dir, light.direction, normal, _SpecularPow);
					specular *= diffuse * fresnel;
					shade += light.color * (diffuse + specular);
			    }

				float rimIntensity = pow(1.0 - saturate(dot(normal, viewDir)), _RimPow);
				float3 rim = _RimColor * rimIntensity;
				shade += rim;
				
				half4 color = _Color;
				color.rgb *= shade + SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
				color.rgb = MixFog(color.rgb, input.fogFactorAndVertexLight.x);
				
				FragOut output;
				output.color = color;
				output.depth = depth;
				return output;
			}
            ENDHLSL
		}

		Pass
		{
			Name "Shadow Caster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		    #pragma multi_compile_instancing
		    #pragma prefer_hlslcc gles
		    #pragma exclude_renderers d3d11_9x
		    #pragma target 2.0

		    #pragma vertex Vert
		    #pragma fragment Frag

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

			    Ray ray;
				ray.origin = input.posWS;
				ray.dir = GetCameraForward();
				ray.maxSteps = 10;
				ray.maxDist = 100;
			    ray.currentDist = 0.;
			    ray.travelledPoint = ray.origin;
			    ray.distTravelled = length(ray.travelledPoint - GetCameraPosition());
			    if (!RaymarchSphere(ray)) discard;
				
			    FragOut o;
				o.color = o.depth = GetDepth(ray.travelledPoint);
			    return o;
			}
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
