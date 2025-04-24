Shader "Rayman/ShapeSssLit"
{
    Properties
    {
        [Header(PBR)][Space]
    	[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    	_NoiseTex ("Noise Texture", 2D) = "white" {}
    	_GradientScaleY("Gradient Scale Y", Range(0.5, 5.0)) = 1.0
    	_GradientOffsetY("Gradient Offset Y", Range(0.0, 1.0)) = 0.5
    	_GradientAngle("Gradient Angle", Float) = 0.0
    	_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    	_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
    	_RayShadowBias("Ray Shadow Bias", Range(0.0, 0.01)) = 0.008
    	
    	[Header(SSS)][Space]
    	_SssDistortion ("SSS Distortion", Float) = 0.1
    	_SssPower ("SSS Power", Float) = 1.0
    	_SssScale ("SSS Scale", Float) = 0.5
    	_SssAmbient ("SSS Ambient", Float) = 0.1
    	
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
        	"RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        	"UniversalMaterialType" = "Unlit"
        	"IgnoreProjector" = "True"
        	"DisableBatching" = "True"
        }
        LOD 100
        
        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Math.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Operation.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Raymarch.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/RaymarchShadow.hlsl"
        #include "Packages/com.davidkimighty.rayman/Shaders/Library/Core/Bvh.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Camera.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Library/Geometry.hlsl"
		#include "Packages/com.davidkimighty.rayman/Shaders/Shared/Shape.hlsl"
        
		struct Shape
		{
        	int type;
			float3 position;
			float4 rotation;
			float3 scale;
			half3 size;
        	half3 pivot;
        	int operation;
        	half blend;
			half roundness;
			half4 color;
#ifdef GRADIENT_COLOR
			half4 gradientColor;
#endif
		};

        CBUFFER_START(RaymarchPerGroup)
		float _EpsilonMin;
		float _EpsilonMax;
        int _MaxSteps;
		float _MaxDistance;
        int _ShadowMaxSteps;
        float _ShadowMaxDistance;
		float _GradientScaleY;
		float _GradientOffsetY;
		float _GradientAngle;
		CBUFFER_END
		
		StructuredBuffer<Shape> _ShapeBuffer;
        StructuredBuffer<NodeAabb> _NodeBuffer;
        
        int2 hitCount; // x is leaf
		int hitIds[RAY_MAX_HITS];
		half4 baseColor;

		float mod289(float x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
		float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
		float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

		float4 permute(float4 x) { return mod289(((x * 34.0) + 1.0) * x); }

		float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

		float snoise(float3 v)
		{
		    const float2  C = float2(1.0 / 6.0, 1.0 / 3.0);
		    const float4  D = float4(0.0, 0.5, 1.0, 2.0);

		    // First corner
		    float3 i = floor(v + dot(v, C.yyy));
		    float3 x0 = v - i + dot(i, C.xxx);

		    // Other corners
		    float3 g = step(x0.yzx, x0.xyz);
		    float3 l = 1.0 - g;
		    float3 i1 = min(g.xyz, l.zxy);
		    float3 i2 = max(g.xyz, l.zxy);

		    float3 x1 = x0 - i1 + C.xxx;
		    float3 x2 = x0 - i2 + C.yyy;
		    float3 x3 = x0 - 0.5;

		    // Permutations
		    i = mod289(i);
		    float4 p = permute(permute(permute(
		        i.z + float4(0.0, i1.z, i2.z, 1.0)) +
		        i.y + float4(0.0, i1.y, i2.y, 1.0)) +
		        i.x + float4(0.0, i1.x, i2.x, 1.0));

		    // Gradients
		    float4 j = p - 49.0 * floor(p / 49.0);
		    float4 x_ = floor(j / 7.0);
		    float4 y_ = floor(j - 7.0 * x_);
		    float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
		    float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;

		    float4 h = 1.0 - abs(x) - abs(y);
		    float4 b0 = float4(x.xy, y.xy);
		    float4 b1 = float4(x.zw, y.zw);

		    float4 s0 = floor(b0) * 2.0 + 1.0;
		    float4 s1 = floor(b1) * 2.0 + 1.0;
		    float4 sh = -step(h, 0.0);

		    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
		    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

		    float3 g0 = float3(a0.x, a0.y, h.x);
		    float3 g1 = float3(a0.z, a0.w, h.y);
		    float3 g2 = float3(a1.x, a1.y, h.z);
		    float3 g3 = float3(a1.z, a1.w, h.w);

		    // Normalize gradients
		    float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
		    g0 *= norm.x;
		    g1 *= norm.y;
		    g2 *= norm.z;
		    g3 *= norm.w;

		    // Compute noise contributions
		    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
		    m = m * m;
		    return 42.0 * dot(m * m, float4(dot(g0, x0), dot(g1, x1), dot(g2, x2), dot(g3, x3)));
		}

		float Noise3D(float3 p)
		{
			// Use your favorite 3D noise function here
			return snoise(p * 300.0); // scale changes frequency
		}
		
		inline float2 CombineDistance(Shape shape, float3 localPos, float totalDist)
		{
			localPos = RotateWithQuaternion(localPos, shape.rotation);
			localPos /= shape.scale;
			localPos -= GetPivotOffset(shape.type, shape.pivot, shape.size);
			
			float uniformScale = max(max(shape.scale.x, shape.scale.y), shape.scale.z);
			float dist = GetShapeSdf(localPos, shape.type, shape.size, shape.roundness) * uniformScale;
			dist += Noise3D(localPos) * 0.0001;
			return SmoothOperation(shape.operation, totalDist, dist, shape.blend);
		}

#if defined(GRADIENT_COLOR)
		inline half4 GetShapeGradientColor(Shape shape, float3 localPos)
		{
			float2 uv = (localPos.xy - 0.5 + _GradientOffsetY) * _GradientScaleY + 0.5;
			uv = GetRotatedUV(uv, float2(0.5, 0.5), radians(_GradientAngle));
			uv.y = 1.0 - uv.y;
			uv = saturate(uv);
			return lerp(shape.color, shape.gradientColor, uv.y);
		}
#endif
		
		float Map(inout Ray ray)
		{
			float totalDist = _MaxDistance;
			baseColor = _ShapeBuffer[hitIds[0]].color;
			
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 localPos = ray.hitPoint - shape.position;
				float2 combined = CombineDistance(shape, localPos, totalDist);
				totalDist = combined.x;
#ifdef GRADIENT_COLOR
				half4 color = GetShapeGradientColor(shape, localPos);
#else
				half4 color = shape.color;
#endif
				baseColor = lerp(baseColor, color, combined.y);
			}
			return totalDist;
		}

		float NormalMap(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 localPos = positionWS - shape.position;
				totalDist = CombineDistance(shape, localPos, totalDist).x;
			}
			return totalDist;
		}

		float ShadowMap(const float3 positionWS)
		{
			float totalDist = _MaxDistance;
			for (int i = 0; i < hitCount.x; i++)
			{
				Shape shape = _ShapeBuffer[hitIds[i]];
				float3 localPos = positionWS - shape.position;
				totalDist = CombineDistance(shape, localPos, totalDist).x;
			}
			return totalDist;
		}

		inline NodeAabb GetNode(const int index)
		{
			return _NodeBuffer[index];
		}
        
        ENDHLSL

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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
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
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
		    
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile_fragment _ GRADIENT_COLOR
			
			#pragma vertex Vert
            #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/SssLitForwardPass.hlsl"
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
		    #pragma shader_feature _ALPHATEST_ON
		    #pragma multi_compile_instancing

		    #pragma vertex Vert
		    #pragma fragment Frag
		    
			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitDepthOnlyPass.hlsl"
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
		    #pragma shader_feature _ALPHATEST_ON
		    #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
		    #pragma multi_compile_instancing

			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitDepthNormalsPass.hlsl"
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
			
			#pragma vertex Vert
		    #pragma fragment Frag

			#include "Packages/com.davidkimighty.rayman/Shaders/Lit/LitShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
    FallBack "Diffuse"
}
