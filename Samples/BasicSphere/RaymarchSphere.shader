Shader "Rayman/RaymarchSphere"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
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
        	"IgnoreProjector" = "True"
        	"DisableBatching" = "True"
        }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        
        #include "Packages/com.davidkimighty.rayman/Runtime/Shaders/Raymarching.hlsl"
        #include "Packages/com.davidkimighty.rayman/Runtime/Shaders/Lighting.hlsl"
        
		float Circle(float3 pos)
		{
        	float r = 0.5;
			return length(ToObject(pos) * GetScale()) - r;
		}

        void RaymarchSphere(inout Ray ray)
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
		    if (!(ray.currentDist < 0.001)) discard;
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
        ENDHLSL

		Pass
		{
			Name "Forward Lit"
			Tags { "LightMode" = "UniversalForward" }
			
			Blend [_BlendSrc] [_BlendDst]
		    ZWrite [_ZWrite]
		    Cull [_Cull]
		    
			HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

		    #pragma multi_compile_instancing
		    #pragma instancing_options renderinglayer
		    #pragma multi_compile _ DOTS_INSTANCING_ON

		    #pragma prefer_hlslcc gles
		    #pragma exclude_renderers d3d11_9x
		    #pragma target 2.0

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
            float4 _Color;

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
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				Ray ray = InitRay(input.wsPos, _MaxSteps, _MaxDist);
			    RaymarchSphere(ray);

				const float depth = GetDepth(ray, input.wsPos);
				const float3 normal = GetNormal(ray.travelledPoint);

				const half4 shadowCoord = TransformWorldToShadowCoord(ray.travelledPoint);
				const Light mainLight = GetMainLight(shadowCoord);
				const float diffuse = GetDiffuse(mainLight.direction, normal);
				const float specular = GetSpecular(ray.dir, mainLight.direction, normal, 1000);
				float3 color = _Color.rgb;
				color *= diffuse + specular;
				//color = GammaCorrection(color, ray.distTravelled);
				color = MixFog(color, input.fogFactorAndVertexLight.x);
				
				FragOut output;
				output.color = float4(color, 1);
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
			#pragma vertex Vert
			#pragma fragment Frag

			#pragma multi_compile_instancing
			#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma target 2.0

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
			    RaymarchSphere(ray);

			    FragOut o;
			    float4 pos = ray.distTravelled;
			    o.color = o.depth = pos.z / pos.w;
			    return o;
			}
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
