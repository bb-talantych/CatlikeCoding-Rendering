#if !defined(MY_TRIPLANAR_MAPPING_INCLUDED)
#define MY_TRIPLANAR_MAPPING_INCLUDED

	#define NO_DEFAULT_UV

	#include "My Lighting Input.cginc"

	// it's not included in "My Lighting Input.cginc"
	sampler2D _TopMainTex, _TopMOHSMap, _TopNormalMap;
	sampler2D _MOHSMap;
	float _MapScale;
	float _BlendOffset, _BlendExponent, _BlendHeightStrength;

	struct TriplanarUV 
	{
		float2 x, y, z;
	};

	TriplanarUV GetTriplanarUV (SurfaceParameters parameters)
	{
		TriplanarUV triUV;

		// parameters.position is in world space
		float3 p = parameters.position * _MapScale;
		triUV.x = p.zy;
		triUV.y = p.xz;
		triUV.z = p.xy;
		if (parameters.normal.x < 0) 
		{
			triUV.x.x = -triUV.x.x;
		}
		if (parameters.normal.y < 0) 
		{
			triUV.y.x = -triUV.y.x;
		}
		// the face facing the camera has negative normal
		// because camera looks towards possitive Z axis
		// so we adjust for possitive normal
		if (parameters.normal.z >= 0) 
		{
			triUV.z.x = -triUV.z.x;
		}

		// the x side and z side are alligned on test texture
		triUV.x.y += 0.5;
		triUV.z.x += 0.5;

		return triUV;

	}
	float3 GetTriplanarWeights 
		(SurfaceParameters parameters, float heightX, float heightY, float heightZ) 
	{
		float3 triW = abs(parameters.normal);
		triW = saturate(triW - _BlendOffset);
		triW *= lerp(1, float3(heightX, heightY, heightZ), _BlendHeightStrength);
		triW = pow(triW, _BlendExponent);
		return triW / (triW.x + triW.y + triW.z);
	}
	float3 BlendTriplanarNormal (float3 mappedNormal, float3 surfaceNormal)
	{
		float3 n;
		// whiteout blending without normalization
		n.xy = mappedNormal.xy + surfaceNormal.xy;
		n.z = mappedNormal.z * surfaceNormal.z;
		return n;
	}

	void MyTriPlanarSurfaceFunction (inout SurfaceData surface, SurfaceParameters parameters) 
	{
		TriplanarUV triUV = GetTriplanarUV(parameters);
		
		float3 albedoX = tex2D(_MainTex, triUV.x).rgb;
		float3 albedoY = tex2D(_MainTex, triUV.y).rgb;
		float3 albedoZ = tex2D(_MainTex, triUV.z).rgb;

		float4 mohsX = tex2D(_MOHSMap, triUV.x);
		float4 mohsY = tex2D(_MOHSMap, triUV.y);
		float4 mohsZ = tex2D(_MOHSMap, triUV.z);

		float3 tangentNormalX = UnpackNormal(tex2D(_NormalMap, triUV.x));
		float4 rawNormalY = tex2D(_NormalMap, triUV.y);
		float3 tangentNormalZ = UnpackNormal(tex2D(_NormalMap, triUV.z));

		#if defined(_SEPARATE_TOP_MAPS)
		if (parameters.normal.y > 0) 
		{
			albedoY = tex2D(_TopMainTex, triUV.y).rgb;
			mohsY = tex2D(_TopMOHSMap, triUV.y);
			rawNormalY = tex2D(_TopNormalMap, triUV.y);
		}
		#endif
		float3 tangentNormalY = UnpackNormal(rawNormalY);

		// adjusting the normals
		if (parameters.normal.x < 0) 
		{
			// to prevent mirroring, because triUVs are negated
			tangentNormalX.x = -tangentNormalX.x;
		}
		if (parameters.normal.y < 0) 
		{
			// to prevent mirroring, because triUVs are negated
			tangentNormalY.x = -tangentNormalY.x;
		}
		if (parameters.normal.z >= 0) 
		{
			// to prevent mirroring, because triUVs are negated
			tangentNormalZ.x = -tangentNormalZ.x;
		}

		// whiteout blending assumes Z is pointing up
		// so we blend in tangent space and then convert to world space
		float3 worldNormalX = BlendTriplanarNormal(tangentNormalX, parameters.normal.zyx).zyx;
		float3 worldNormalY = BlendTriplanarNormal(tangentNormalY, parameters.normal.xzy).xzy;
		float3 worldNormalZ = BlendTriplanarNormal(tangentNormalZ, parameters.normal);

		float3 triW = GetTriplanarWeights(parameters, mohsX.z, mohsY.z, mohsZ.z);

		float3 finalAlbedoX = albedoX * triW.x;
		float3 finalAlbedoY = albedoY * triW.y;
		float3 finalAlbedoZ = albedoZ * triW.z;
		surface.albedo = finalAlbedoX + finalAlbedoY + finalAlbedoZ;

		float4 finalMohsX = mohsX * triW.x;
		float4 finalMohsY = mohsY * triW.y;
		float4 finalMohsZ = mohsZ * triW.z;
		float4 finalMohs = finalMohsX + finalMohsY + finalMohsZ;
		surface.metallic = finalMohs.x;
		surface.occlusion = finalMohs.y;
		surface.smoothness = finalMohs.a;

		float3 finalNormalX = worldNormalX * triW.x;
		float3 finalNormalY = worldNormalY * triW.y;
		float3 finalNormalZ = worldNormalZ * triW.z;

		surface.normal = normalize(finalNormalX + finalNormalY + finalNormalZ);
	}

	#define SURFACE_FUNCTION MyTriPlanarSurfaceFunction

#endif