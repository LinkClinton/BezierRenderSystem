#pragma pack_matrix(row_major)

#define eps 1e-8
#define pi 3.1415926535898
#define no_root -1

cbuffer Transform : register(b0)
{
	matrix world;
	matrix projection;
}

cbuffer Equation : register(b1)
{
    float4 coefficient0;
    float4 coefficient1;
    float4 coefficient2;
}

struct Output 
{
	float4 projection : SV_POSITION;
	float2 texcoord : TEXCOORD;
	float4 color : COLOR;
};

float cube_root(float x)
{
    float y = pow(abs(x), 1.0f / 3.0f);
    return x < 0 ? -y : y;
}

//see ref https://stackoverflow.com/questions/27176423/function-to-solve-cubic-equation-analytically
void solve_equation(
    float a, float b, float c, float d, 
    out float root0, out float root1, out float root2)
{
	root0 = no_root;
	root1 = no_root;
	root2 = no_root;

    if (abs(a) < eps) { //Quadratic case, bx^2 + cx + d = 0
        if (abs(b) < eps) { //Linear case, cx + d = 0
            if (abs(c) < eps) //Degenerate case
                return;
            
            //linear case, root = -d / c
            root0 = -d / c;
            return;
        }
        
        float D = c * c - 4 * b * d;

        //delta = 0, only one root, root = -c / (2 * b)
        if (abs(D) < eps) {
            root0 = -c / (2 * b); return;
        }else if (D > 0){

            //delta > 0, two root
            //root0 = (-c + sqrt(D)) / (2 * b)
            //root1 = (-c - sqrt(D)) / (2 * b)
            float sqrtD = sqrt(D);

            root0 = (-c + sqrtD) / (2 * b);
            root1 = (-c - sqrtD) / (2 * b);

            return;
        }

        //delta < 0, no root
        return;
    }

    float p = (3 * a * c - b * b) / (3 * a * a);
    float q = (2 * b * b * b - 9 * a * b * c + 27 * a * a * d) / (27 * a * a * a);
    
    if (abs(p) < eps){
        root0 = cube_root(-q);
    }else if (abs(q) < eps){
        root0 = 0;
        
        if (p < 0){
            root1 = sqrt(-p);
            root2 = -sqrt(-p);
        }
    }else {
        float D = q * q / 4 + p * p * p / 27;

        if (abs(D) < eps){
            root0 = -1.5f * q / p;
            root1 = 3 * q / p;
        }else if (D > 0){
            float u = cube_root(-q / 2 - sqrt(D));
            
            root0 = u - p / (3 * u);
        }else {
            float u = 2 * sqrt(-p / 3);
            float t = acos(3 * q / p / u) / 3;
            float k = 2 * pi / 3;

            root0 = u * cos(t);
            root1 = u * cos(t - k);
            root2 = u * cos(t - 2 * k);
        }
    }

    root0 = root0 - b / (3 * a);
    root1 = root1 - b / (3 * a);
    root2 = root2 - b / (3 * a);
}

float2 bezier_curve_point(float2 A, float2 B, float2 C, float t)
{
    return A * t * t + B * t + C;
}

float msaa_sample(float2 position, float alpha) 
{
	//solve equation (-2A^2)t^3 + (-3AB)t^2 + (2AP - 2AC - B^2)t + B(P - C) = 0
    //A = (-2A^2) = coefficient0.x
    //B = (-3AB) = coefficient0.y
    //C = (2AP - 2AC - B^2) = 2 * dot(coefficient1.xy, project.xy) + coefficient0.z
    //D =  B(P - C) = dot(coefficient1.zw, project.xy) + coefficient0.w

	float A = coefficient0.x;
	float B = coefficient0.y;
	float C = dot(coefficient1.xy, position.xy) * 2 + coefficient0.z;
	float D = dot(coefficient1.zw, position.xy) + coefficient0.w;

	float root0 = no_root;
	float root1 = no_root;
	float root2 = no_root;

	solve_equation(A, B, C, D, root0, root1, root2);

	if (root0 >= 0 && root0 <= 1.0f) {
		float2 Q = bezier_curve_point(coefficient1.xy, coefficient1.zw, coefficient2.xy, root0);

		if (distance(position.xy, Q) <= coefficient2.z) return alpha;
	}

	if (root1 >= 0 && root1 <= 1.0f) {
		float2 Q = bezier_curve_point(coefficient1.xy, coefficient1.zw, coefficient2.xy, root1);

		if (distance(position.xy, Q) <= coefficient2.z) return alpha;
	}

	if (root2 >= 0 && root2 <= 1.0f) {
		float2 Q = bezier_curve_point(coefficient1.xy, coefficient1.zw, coefficient2.xy, root2);

		if (distance(position.xy, Q) <= coefficient2.z) return alpha;
	}

	return 0;
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
	float2 center = project.xy;

	if (msaa_sample(center, color.a) == 0) discard;

	float sample0 = msaa_sample(center + float2(0.25f, 0.25f), color.a);
	float sample1 = msaa_sample(center + float2(0.25f, -0.25f), color.a);
	float sample2 = msaa_sample(center + float2(-0.25f, 0.25f), color.a);
	float sample3 = msaa_sample(center + float2(-0.25f, -0.25f), color.a);

	return float4(color.xyz, (sample0 + sample1 + sample2 + sample3) * 0.25f);
}