Shader "Rayman/RaymarchSphere"
{
    Properties
    {
//        _Color ("Color", Color) = (1,1,1,1)
//        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    	[MainColor] _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
		[HideInInspector][MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.5
	    _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
	    [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
    	
	    [Header(ClearCoat (Forward Only))][Space]
	    [Toggle] _ClearCoat("Clear Coat", Float) = 0.0
	    [HideInInspector] _ClearCoatMap("Clear Coat Map", 2D) = "white" {}
	    _ClearCoatMask("Clear Coat Mask", Range(0.0, 1.0)) = 0.0
	    _ClearCoatSmoothness("Clear Coat Smoothness", Range(0.0, 1.0)) = 1.0
        
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

        void Raymarching(inout Ray ray)
        {
	        if (!RaymarchSphere(ray)) discard;
        }

        float3 GetNormal(const float3 pos)
		{
		    const float2 k = float2(1, -1) * 0.001;
		    return normalize(float3(
		        k.xyy * Circle(pos + k.xyy) +
		        k.yyx * Circle(pos + k.yyx) +
		        k.yxy * Circle(pos + k.yxy) +
		        k.xxx * Circle(pos + k.xxx)));
		}

        float3 GetNormal2(float3 pos)
		{
		    float epsilon = 0.001;
		    float3 dx = float3(epsilon, 0, 0);
		    float3 dy = float3(0, epsilon, 0);
		    float3 dz = float3(0, 0, epsilon);
		    
		    float distX = Circle(pos + dx) - Circle(pos - dx);
		    float distY = Circle(pos + dy) - Circle(pos - dy);
		    float distZ = Circle(pos + dz) - Circle(pos - dz);
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
			    float4 csPos : SV_POSITION;
				float4 ssPos : TEXCOORD0;
			    float3 wsPos : TEXCOORD1;
			    float3 wsNormal : TEXCOORD2;
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

			int _MaxSteps;
			float _MaxDist;

			Varyings Vert (Attributes input)
			{
			    Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
				output.csPos = vertexInput.positionCS;
				output.wsPos = vertexInput.positionWS;
				output.wsNormal = TransformObjectToWorldNormal(input.normal);

				half3 vertexLight = VertexLighting(output.wsPos, output.wsNormal);
				half fogFactor = ComputeFogFactor(output.csPos.z);
				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

				OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
				OUTPUT_SH(output.wsNormal.xyz, output.vertexSH);
			    return output;
			}

			FragOut Frag (Varyings input)
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				Ray ray = InitRay(input.wsPos, _MaxSteps, _MaxDist);
			    Raymarching(ray);
				
				const float3 normal = GetNormal2(ray.travelledPoint);
				
				/*
				InputData inputData = (InputData)0;
			    inputData.positionWS = ray.travelledPoint;
			    inputData.normalWS = 2.0 * normal - 1.0;;
			    inputData.viewDirectionWS = SafeNormalize(GetCameraPosition() - ray.travelledPoint);
			    inputData.shadowCoord = TransformWorldToShadowCoord(ray.travelledPoint);
			    inputData.fogCoord = input.fogFactorAndVertexLight.x;
			    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
			    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.csPos);
				inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
				
				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.metallic = _Metallic;
				surfaceData.smoothness = _Smoothness;
				InitializeStandardLitSurfaceData(float2(0, 0), surfaceData);*/

				// main light
				half4 shadowCoord = TransformWorldToShadowCoord(ray.travelledPoint);
				Light ml = GetMainLight(shadowCoord);
				float bias = 0.0135;
				float normalBias = bias * max(0.0, dot(ml.direction, normal));
				shadowCoord.z += normalBias;
				ml = GetMainLight(shadowCoord);
				float shadowAttenuation = ml.shadowAttenuation;
				
				const float md = GetDiffuse(ml.direction, normal);
				const float ms = GetSpecular(ray.dir, ml.direction, normal, 1000) * md;
				half3 shade = ml.color * (md + ms) * shadowAttenuation;
			 
				// other lights
				const int count = GetAdditionalLightsCount();
				for (int i = 0; i < count; ++i)
			    {
				    const Light sl = GetAdditionalLight(i, ray.travelledPoint);
				    const float sd = GetDiffuse(sl.direction, normal) * sl.distanceAttenuation;
				    const float ss = GetSpecular(ray.dir, ml.direction, normal, 1000) * sd;
					shade += sl.color * (sd + ss);
			    }

				half4 color = _BaseColor;
				color.rgb *= shade + SAMPLE_GI(input.lightmapUV, input.vertexSH, normal);
				/*color = UniversalFragmentPBR(inputData, surfaceData);
				color *= _BaseColor;
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				color.a = 1.;*/

				//color = GammaCorrection(color, ray.distTravelled);

				FragOut output;
				output.color = color;
				const float depth = GetDepth(ray, input.wsPos);
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
			    float4 csPos : SV_POSITION;
			    float3 wsPos : TEXCOORD0;
			    float3 wsNormal : TEXCOORD1;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			    UNITY_VERTEX_OUTPUT_STEREO
			};

			struct FragOut
			{
			    float4 color : SV_Target;
			    float depth : SV_Depth;
			};
			
			inline float4 GetShadowPositionHClip(float3 positionWS)
			{
			    //positionWS = CustomApplyShadowBias(positionWS, normalWS);
			    float4 positionCS = TransformWorldToHClip(positionWS);
			#if UNITY_REVERSED_Z
			    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
			#else
			    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
			#endif
			    return positionCS;
			}

			Varyings Vert(Attributes input)
			{
			    Varyings o;
			    UNITY_SETUP_INSTANCE_ID(i);
			    UNITY_TRANSFER_INSTANCE_ID(i, o);
			    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			    o.csPos = TransformObjectToHClip(input.vertex.xyz);
			    o.wsPos = TransformObjectToWorld(input.vertex.xyz);
			    o.wsNormal = TransformObjectToWorldNormal(input.normal);
			    return o;
			}
			
			FragOut Frag(Varyings input)
			{
			    UNITY_SETUP_INSTANCE_ID(i);
			    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

			    Ray ray;
				ray.origin = input.wsPos;
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
