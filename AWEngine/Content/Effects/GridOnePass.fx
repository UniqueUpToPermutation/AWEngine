float4x4 View;
float4x4 Projection;

float3 TextureSize;
float2 MaskUVOffsetLL;
float2 MaskUVOffsetLC;
float2 MaskUVOffsetLR;

float TransMaskURCount;
float TransMaskUCCount;
float TransMaskULCount;

sampler GroundSampler;
sampler TransMaskUR;
sampler TransMaskUC;
sampler TransMaskUL;

struct VertexShaderInput
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
	float2 UV : TEXCOORD0;
	float4 Aux1 : COLOR1;
	float4 Aux2 : COLOR2;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 GroundUV : TEXCOORD0;
	float2 MaskUV : TEXCOORD1;
	float4 TextureIds : TEXCOORD2;
	float3 MaskIdsLC : TEXCOORD3;
	float4 MaskIdsLRLL : TEXCOORD4;
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

	float biasFactor = 250;

	// Calculate texture Ids
	output.TextureIds.r = input.Color.r * biasFactor / (TextureSize.z - 1);
	output.TextureIds.g = input.Color.g * biasFactor / (TextureSize.z - 1);
	output.TextureIds.b = input.Color.b * biasFactor / (TextureSize.z - 1);
	output.TextureIds.a = input.Color.a * biasFactor / (TextureSize.z - 1);

	// Calculate mask Ids
	output.MaskIdsLC.x = input.Aux1.r * biasFactor / (TransMaskULCount - 1);
	output.MaskIdsLC.y = input.Aux1.g * biasFactor / (TransMaskUCCount - 1);
	output.MaskIdsLC.z = input.Aux1.b * biasFactor / (TransMaskURCount - 1);
	output.MaskIdsLRLL.x = input.Aux2.r * biasFactor / (TransMaskUCCount - 1);
	output.MaskIdsLRLL.y = input.Aux2.g * biasFactor / (TransMaskURCount - 1);
	output.MaskIdsLRLL.z = input.Aux2.b * biasFactor / (TransMaskUCCount - 1);
	output.MaskIdsLRLL.w = input.Aux2.a * biasFactor / (TransMaskULCount - 1);

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
	float2 uvMaskLL = input.MaskUV + MaskUVOffsetLL;
	float2 uvMaskLC = input.MaskUV + MaskUVOffsetLC;
	float2 uvMaskLR = input.MaskUV + MaskUVOffsetLR;

	float3 uvMaskLC_UL = float3(uvMaskLC, input.MaskIdsLC.x);
	float3 uvMaskLC_UC = float3(uvMaskLC, input.MaskIdsLC.y);
	float3 uvMaskLC_UR = float3(uvMaskLC, input.MaskIdsLC.z);

	float3 uvMaskLL_UC = float3(uvMaskLL, input.MaskIdsLRLL.x);
	float3 uvMaskLL_UR = float3(uvMaskLL, input.MaskIdsLRLL.y);

	float3 uvMaskLR_UC = float3(uvMaskLR, input.MaskIdsLRLL.z);
	float3 uvMaskLR_UL = float3(uvMaskLR, input.MaskIdsLRLL.w);

	float maskLL_UR = tex3D(TransMaskUR, uvMaskLL_UR).r;
	float maskLL_UC = tex3D(TransMaskUC, uvMaskLL_UC).r;
	float maskLC_UR = tex3D(TransMaskUR, uvMaskLC_UR).r;
	float maskLC_UC = tex3D(TransMaskUC, uvMaskLC_UC).r;
	float maskLC_UL = tex3D(TransMaskUL, uvMaskLC_UL).r;
	float maskLR_UL = tex3D(TransMaskUL, uvMaskLR_UL).r;
	float maskLR_UC = tex3D(TransMaskUC, uvMaskLR_UC).r;

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
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}