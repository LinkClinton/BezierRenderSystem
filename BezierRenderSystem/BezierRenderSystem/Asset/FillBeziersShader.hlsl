#pragma pack_matrix(row_major)

struct TrianglePoints
{
	float4 position[3];
};

struct TriangleColors
{
	float4 color[3];
};

struct Output 
{
	float4 projection : SV_POSITION;
	float2 texcoord : TEXCOORD;
	float4 color : COLOR;
	uint instance_id : SV_InstanceID;
};

StructuredBuffer<TrianglePoints> triangle_points : register(t0);
StructuredBuffer<TriangleColors> triangle_colors : register(t1);
StructuredBuffer<TrianglePoints> triangle_points_canvas : register(t2);


float msaa_cross(float2 u, float2 v) 
{
	return u.x * v.y - u.y * v.x;
}

float msaa_area_function(float2 u, float2 v, float2 p)
{
	return msaa_cross(v - u, p - u);
}

float msaa_sample(float2 position, 
	float4 position0, float4 position1, float4 position2,
	float4 color0, float4 color1, float4 color2) 
{
	float inv_triangle_area = abs(1.0f / msaa_area_function(position0.xy, position1.xy, position2.xy));

	float sub_area0 = abs(msaa_area_function(position1.xy, position2.xy, position.xy)) * inv_triangle_area;
	float sub_area1 = abs(msaa_area_function(position2.xy, position0.xy, position.xy)) * inv_triangle_area;
	float sub_area2 = abs(msaa_area_function(position0.xy, position1.xy, position.xy)) * inv_triangle_area;

	float2 uv = float2(0, 0) * sub_area0 + float2(0.5f, 0) * sub_area1 + float2(1, 1) * sub_area2;
	
	if (uv.x * uv.x - uv.y > 0) return 0;

	return color0.a * sub_area0 + color1.a * sub_area1 + color2.a * sub_area2;
}

Output vs_main(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
{
	Output result;

	result.projection = triangle_points[instance_id].position[vertex_id];
	result.color = triangle_colors[instance_id].color[vertex_id];
	result.instance_id = instance_id;

	if (vertex_id == 0) result.texcoord = float2(0, 0);
	if (vertex_id == 1) result.texcoord = float2(0.5f, 0);
	if (vertex_id == 2) result.texcoord = float2(1, 1);
	
	return result;
}

float4 ps_main(
	float4 project : SV_POSITION,
	float2 texcoord : TEXCOORD,
	float4 color : COLOR,
	uint instance_id : SV_InstanceID) : SV_TARGET
{
	float edge_function = texcoord.x * texcoord.x - texcoord.y;

	//only enable mass at the edge
	if (abs(edge_function) < 0.01) 
	{
		float2 center = project.xy;

		float2 offset[4];
		float  sample[4];

		offset[0] = float2(0.25f, 0.25f);
		offset[1] = float2(0.25f, -0.25f); 
		offset[2] = float2(-0.25f, 0.25f); 
		offset[3] = float2(-0.25f, -0.25f);

		for (int i = 0; i < 4; i++) 
		{
			sample[i] = msaa_sample(center + offset[i],
				triangle_points_canvas[instance_id].position[0],
				triangle_points_canvas[instance_id].position[1],
				triangle_points_canvas[instance_id].position[2],
				triangle_colors[instance_id].color[0],
				triangle_colors[instance_id].color[1],
				triangle_colors[instance_id].color[2]);
		}

		return float4(color.xyz, (sample[0] + sample[1] + sample[2] + sample[3]) * 0.25f);
	}

	if (edge_function > 0) discard;

	return color;
}