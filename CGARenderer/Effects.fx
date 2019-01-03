cbuffer matrices : register(b1)
{
	float4x4 Model;
	float4x4 InverseTransposeModelView;
	float4x4 ModelViewProjection;
	float3 Eye;
};

Texture2D ObjTexture;
SamplerState ObjSamplerState;

struct VS_INPUT
{
	float4 Pos: POSITION;
	float3 Normal: NORMAL;
	float4 Color: COLOR;
	float2 TexCoord: TEXCOORD;
};

struct PS_INPUT
{
	float4 Pos: SV_POSITION;
	float4 WorldPos: POSITION;
	float3 Normal: NORMAL;
	float4 Color: COLOR;
	float2 TexCoord: TEXCOORD;
};

PS_INPUT Vertex_Shader(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;
	output.Pos = mul(input.Pos, ModelViewProjection);
	output.WorldPos = mul(input.Pos, Model);
	output.Normal = mul(float4(input.Normal, 0.0f), InverseTransposeModelView).xyz;
	output.Color = input.Color;
	output.TexCoord = input.TexCoord;
	return output;
}

float4 Pixel_Shader_SolidGreen(PS_INPUT input) : SV_TARGET
{
	return float4(0, 1, 0, 1);
}

float4 Pixel_Shader_Lambert(PS_INPUT input) : SV_TARGET
{
	float3 L = normalize(input.WorldPos - Eye);
	float3 N = input.Normal;
	return 0.1 + max(dot(L, N), 0) * 0.9;
}

float4 Pixel_Shader_Texture(PS_INPUT input) : SV_TARGET
{
	return ObjTexture.Sample(ObjSamplerState, input.TexCoord);
}
