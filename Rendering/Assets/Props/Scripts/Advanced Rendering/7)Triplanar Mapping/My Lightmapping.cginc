#if !defined(MY_LIGHTMAPPING_INCLUDED)
#define MY_LIGHTMAPPING_INCLUDED

#include "My Lighting Input.cginc"
#include "UnityMetaPass.cginc"

struct appdata
{
    float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
};
struct Interpolators 
{
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
};

    

	Interpolators MyLightmappingVertexProgram(appdata v)
	{
		Interpolators i;

		i.pos = UnityMetaVertexPosition(v.vertex, v.uv1, v.uv2, unity_LightmapST, unity_DynamicLightmapST);

		#if defined(META_PASS_NEEDS_NORMALS)
		i.normal = UnityObjectToWorldNormal(v.normal);
		#else
			i.normal = float3(0, 1, 0);
		#endif
		#if defined(META_PASS_NEEDS_POSITION)
			i.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
		#else
			i.worldPos.xyz = 0;
		#endif

		#if !defined(NO_DEFAULT_UV)
			i.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
			i.uv.zw = TRANSFORM_TEX(v.uv, _DetailTex);
		#endif

		return i;
	}

	#if !defined(ALBEDO_FUNCTION)
		#define ALBEDO_FUNCTION GetAlbedo
	#endif

	float4 MyLightmappingFragmentProgram(Interpolators i) : SV_TARGET 
	{
		SurfaceData surface;
		surface.normal = normalize(i.normal);
		surface.albedo = 1;
		surface.alpha = 1;
		surface.emission = 0;
		surface.metallic = 0;
		surface.occlusion = 1;
		surface.smoothness = 0.5;
		#if defined(SURFACE_FUNCTION)
			SurfaceParameters sp;
			sp.normal = i.normal;
			sp.position = i.worldPos.xyz;
			sp.uv = UV_FUNCTION(i);

			SURFACE_FUNCTION(surface, sp);
		#else
			surface.albedo = ALBEDO_FUNCTION(i);
			surface.emission = GetEmission(i);
			surface.metallic = GetMetallic(i);
			surface.smoothness = GetSmoothness(i);
		#endif

		UnityMetaInput surfaceData;	

		surfaceData.Emission = surface.emission;
		float oneMinusReflectivity;
		surfaceData.Albedo = DiffuseAndSpecularFromMetallic(
		surface.albedo, surface.metallic,
		surfaceData.SpecularColor, oneMinusReflectivity);

		float roughness = SmoothnessToRoughness(surface.smoothness) * 0.5;
		surfaceData.Albedo += surfaceData.SpecularColor * roughness;

		return UnityMetaFragment(surfaceData);
	}

#endif
