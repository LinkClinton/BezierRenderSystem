#pragma pack_matrix(row_major)

cbuffer Transform : register(b0)
{
	matrix world;
	matrix projection;
}

cbuffer TrianglePoints : register(b1) 
{
	float4 position0;
	float4 position1;
	float4 position2;
}

cbuffer TriangleColors : register(b2) 
{
	float4 color0;
	float4 color1;
	float4 color2;
}

struct Output 
{
	float4 projection : SV_POSITION;
	float2 texcoord : TEXCOORD;
	float4 color : COLOR;
};

float msaa_cross(float2 u, float2 v) 
{
	return u.x * v.y - u.y * v.x;
}

float msaa_area_function(float2 u, float2 v, float2 p)
{
	return msaa_cross(v - u, p - u);
}

float msaa_sample(float2 position) 
{
	float inv_triangle_area = abs(1.0f / msaa_area_function(position0.xy, position1.xy, position2.xy));

	float sub_area0 = abs(msaa_area_function(position1.xy, position2.xy, position.xy)) * inv_triangle_area;
	float sub_area1 = abs(msaa_area_function(position2.xy, position0.xy, position.xy)) * inv_triangle_area;
	float sub_area2 = abs(msaa_area_function(position0.xy, position1.xy, position.xy)) * inv_triangle_area;

	float2 uv = float2(0, 0) * sub_area0 + float2(0.5f, 0) * sub_area1 + float2(1, 1) * sub_area2;
	
	if (uv.x * uv.x - uv.y > 0) return 0;

	return color0.a * sub_area0 + color1.a * sub_area1 + color2.a * sub_area2;
}

Output vs_main(
	float3 position : POSITION,
	float2 texcoord : TEXCOORD,
	float4 color : COLOR) 
{
	Output result;

	result.projection = mul(float4(position, 1.0f), world);
	result.projection = mul(result.projection, projection);

	result.texcoord = texcoord;
	result.color = color;

	return result;
}

float4 ps_main(
	float4 project : SV_POSITION,
	float2 texcoord : TEXCOORD,
	float4 color : COLOR) : SV_TARGET
{
	float edge_function = texcoord.x * texcoord.x - texcoord.y;

	//only enable mass at the edge
	if (abs(edge_function) < 0.1) 
	{
		float2 center = project.xy;

		float sample0 = msaa_sample(center + float2(0.25f, 0.25f));
		float sample1 = msaa_sample(center + float2(0.25f, -0.25f));
		float sample2 = msaa_sample(center + float2(-0.25f, 0.25f));
		float sample3 = msaa_sample(center + float2(-0.25f, -0.25f));

		return float4(color.xyz, (sample0 + sample1 + sample2 + sample3) * 0.25f);
	}
	
	if (edge_function > 0) discard;

	return color;
}