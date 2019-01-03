cbuffer matrices : register(b1)
{
	float4x4 Model;
	float4x4 InverseTransposeModelView;
	float4x4 ModelViewProjection;
	float3 CameraPosition;
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
	output.Color = input.Color;
	output.TexCoord = input.TexCoord;
	output.WorldPos = mul(input.Pos, Model);
	output.Normal = mul(float4(input.Normal, 0.0f), InverseTransposeModelView).xyz;
	output.Pos = mul(input.Pos, ModelViewProjection);
	return output;
}

float4 Pixel_Shader_Lambert(PS_INPUT input) : SV_TARGET
{
	float3 L = input.WorldPos - CameraPosition;
	float3 N = input.Normal;
	return 0.3 + (1 - dot(L, N)) * 0.7;
}

float4 Pixel_Shader_Texture(PS_INPUT input) : SV_TARGET
{
	return ObjTexture.Sample(ObjSamplerState, input.TexCoord);
}
