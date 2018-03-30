float4x4 View;
float4x4 Projection;

float3 TextureSize;
float2 MaskUVOffsetLL;
float2 MaskUVOffsetLC;
float2 MaskUVOffsetLR;

sampler GroundSampler;
sampler TransMaskUR;
sampler TransMaskUC;
sampler TransMaskUL;

struct VertexShaderInput
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 GroundUV : TEXCOORD0;
	float2 MaskUV : TEXCOORD1;
	float4 TextureIds : TEXCOORD2;
};

struct PixelShaderOutput
{
	float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(Projection, mul(View, float4(input.Position, 1.0f)));

	// Calculate UVs for ground textures
	output.GroundUV.x = input.Position.x / TextureSize.x;
	output.GroundUV.y = input.Position.y / TextureSize.y;

	// Calculate UVs for alpha masks
	output.MaskUV = input.UV;

	// Calculate texture Ids
	output.TextureIds.r = input.Color.r * 250 / (TextureSize.z - 1);
	output.TextureIds.g = input.Color.g * 250 / (TextureSize.z - 1);
	output.TextureIds.b = input.Color.b * 250 / (TextureSize.z - 1);
	output.TextureIds.a = input.Color.a * 250 / (TextureSize.z - 1);

	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
	PixelShaderOutput output;

	// Sample ground textures
	float3 uvwBase = float3(input.GroundUV, input.TextureIds.r);
	float3 uvwLL = float3(input.GroundUV, input.TextureIds.g);
	float3 uvwLC = float3(input.GroundUV, input.TextureIds.b);
	float3 uvwLR = float3(input.GroundUV, input.TextureIds.a);

	float4 colorBase = tex3D(GroundSampler, uvwBase);
	float4 colorLL = tex3D(GroundSampler, uvwLL);
	float4 colorLC = tex3D(GroundSampler, uvwLC);
	float4 colorLR = tex3D(GroundSampler, uvwLR);

	// Sample masks
	float3 uvMaskUV = float3(input.MaskUV, 0);
	float3 uvMaskLL = float3(input.MaskUV + MaskUVOffsetLL, 0);
	float3 uvMaskLC = float3(input.MaskUV + MaskUVOffsetLC, 0);
	float3 uvMaskLR = float3(input.MaskUV + MaskUVOffsetLR, 0);

	float maskLL_UR = tex3D(TransMaskUR, uvMaskLL).r;
	float maskLL_UC = tex3D(TransMaskUC, uvMaskLL).r;
	float maskLC_UR = tex3D(TransMaskUR, uvMaskLC).r;
	float maskLC_UC = tex3D(TransMaskUC, uvMaskLC).r;
	float maskLC_UL = tex3D(TransMaskUL, uvMaskLC).r;
	float maskLR_UL = tex3D(TransMaskUL, uvMaskLR).r;
	float maskLR_UC = tex3D(TransMaskUC, uvMaskLR).r;

	output.Color = colorBase;
	output.Color = lerp(output.Color, colorLL, min(1, maskLL_UR + maskLL_UC));
	output.Color = lerp(output.Color, colorLR, min(1, maskLR_UL + maskLR_UC));
	output.Color = lerp(output.Color, colorLC, min(1, maskLC_UR + maskLC_UC + maskLC_UL));

	return output;
}

technique Default
{
	pass Pass
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}