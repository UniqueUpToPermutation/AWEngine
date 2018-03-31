float4x4 View;
float4x4 Projection;

float3 TextureSize;

sampler GroundSampler;

sampler TransMaskUR;
sampler TransMaskUC;
sampler TransMaskUL;

float TransMaskURCount;
float TransMaskUCCount;
float TransMaskULCount;

float2 MaskUVOffset;

struct VertexShaderInput
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderInputMaskUV
{
	float3 Position : POSITION0;
	float4 Color : COLOR0;
	float2 MaskUV : TEXCOORD0;
	float4 Aux1 : COLOR1;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 GroundUV : TEXCOORD0;
	float TextureId : TEXCOORD1;
};

struct VertexShaderOutputMaskUV
{
	float4 Position : POSITION0;
	float2 GroundUV : TEXCOORD0;
	float2 MaskUV : TEXCOORD1;
	float3 TextureIds : TEXCOORD2;
	float3 MaskIds : TEXCOORD3;
};

struct PixelShaderOutput
{
	float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderGroundPass(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(Projection, mul(View, float4(input.Position, 1.0f)));

	// Calculate UVs for ground textures
	output.GroundUV.x = input.Position.x / TextureSize.x;
	output.GroundUV.y = input.Position.y / TextureSize.y;

	float biasFactor = 250;

	// Calculate texture Ids
	output.TextureId = input.Color.r * biasFactor / (TextureSize.z - 1);

	return output;
}

PixelShaderOutput PixelShaderGroundPass(VertexShaderOutput input)
{
	PixelShaderOutput output;

	// Sample ground textures
	float3 uvwBase = float3(input.GroundUV, input.TextureId);
	
	float4 colorBase = tex3D(GroundSampler, uvwBase);

	output.Color = colorBase;
	return output;
}

VertexShaderOutputMaskUV VertexShaderTransitionPass(VertexShaderInputMaskUV input)
{
	VertexShaderOutputMaskUV output;
	output.Position = mul(Projection, mul(View, float4(input.Position, 1.0f)));

	// Calculate UVs for ground textures
	output.GroundUV.x = input.Position.x / TextureSize.x;
	output.GroundUV.y = input.Position.y / TextureSize.y;

	float biasFactor = 250;

	// Calculate texture Ids
	output.TextureIds.r = input.Color.r * biasFactor / (TextureSize.z - 1);
	output.TextureIds.g = input.Color.g * biasFactor / (TextureSize.z - 1);
	output.TextureIds.b = input.Color.b * biasFactor / (TextureSize.z - 1);

	// Calculate mask Ids
	output.MaskIds.r = input.Aux1.r * biasFactor / (TransMaskURCount - 1);
	output.MaskIds.g = input.Aux1.g * biasFactor / (TransMaskUCCount - 1);
	output.MaskIds.b = input.Aux1.b * biasFactor / (TransMaskULCount - 1);

	output.MaskUV = input.MaskUV + MaskUVOffset;

	return output;
}

PixelShaderOutput PixelShaderTransitionPass(VertexShaderOutputMaskUV input)
{
	PixelShaderOutput output;

	// Sample ground textures
	float3 uvwUR = float3(input.GroundUV, input.TextureIds.r);
	float3 uvwUC = float3(input.GroundUV, input.TextureIds.g);
	float3 uvwUL = float3(input.GroundUV, input.TextureIds.b);

	float4 colorUR = tex3D(GroundSampler, uvwUR);
	float4 colorUC = tex3D(GroundSampler, uvwUC);
	float4 colorUL = tex3D(GroundSampler, uvwUL);

	uvwUR = float3(input.MaskUV, input.MaskIds.r);
	uvwUC = float3(input.MaskUV, input.MaskIds.g);
	uvwUL = float3(input.MaskUV, input.MaskIds.b);
	
	float aUR = tex3D(TransMaskUR, uvwUR).r;
	float aUC = tex3D(TransMaskUC, uvwUC).r;
	float aUL = tex3D(TransMaskUL, uvwUL).r;

	float alpha = min(1, aUR + aUC + aUL);

	float4 color = (1 - aUC) * ((1 - aUR) *  colorUL + aUR * colorUR) + aUC * colorUC;

	output.Color.rgb = color.rgb;
	output.Color.a = alpha;

	return output;
}

technique Default
{
	pass GroundPass
	{
		VertexShader = compile vs_3_0 VertexShaderGroundPass();
		PixelShader = compile ps_3_0 PixelShaderGroundPass();
	}

	pass TransitionPass
	{
		VertexShader = compile vs_3_0 VertexShaderTransitionPass();
		PixelShader = compile ps_3_0 PixelShaderTransitionPass();
	}
}