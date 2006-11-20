float4x4 World;
float4x4 View;
float4x4 Projection;
float3 DiffuseColor;
float3 SpecularColor;
float3 AmbientLightColor;
float3 EmissiveColor;
float3 EyePosition;
bool   LightEnabled0;
bool   LightEnabled1;
bool   LightEnabled2;
float3 LightDirection0;
float3 LightDirection1;
float3 LightDirection2;
float3 LightDiffuseColor0;
float3 LightDiffuseColor1;
float3 LightDiffuseColor2;
float3 LightSpecularColor0;
float3 LightSpecularColor1;
float3 LightSpecularColor2;
uniform extern float4x4 BonePalette[" + BasicPaletteEffect.PALETTE_SIZE.ToString()+@"];
float SpecularPower;
bool TextureEnabled;
bool LightingEnabled = false;
texture Texture;


	

sampler BaseSampler = sampler_state
{
   Texture = (Texture);
   ADDRESSU = WRAP;
   ADDRESSV = WRAP;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
};

struct VS_INPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL;
	half4 indices : BLENDINDICES;
	float4 weights : BLENDWEIGHT;
};

struct VS_OUTPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 viewDirection : TEXCOORD2;
};

struct PS_OUTPUT
{
	float4 color : COLOR;
};

void TransformVertex (in VS_INPUT input, out VS_OUTPUT output)
{
	float4 v0 = input.weights[0] * mul(input.position,BonePalette[input.indices[0]]);
	float4 v1 = input.weights[1] * mul(input.position,BonePalette[input.indices[1]]);
	float4 v2 = input.weights[2] * mul(input.position,BonePalette[input.indices[2]]);
	float4 v3 = (1-(input.weights[3]+input.weights[2]+input.weights[1]+input.weights[0]))
		 * mul(input.position,BonePalette[input.indices[3]]);
    output.position = v0+v1+v2+v3;
	float3 objectPosition = mul(output.position, World);
	v0 = input.weights[0] * mul(input.normal, BonePalette[input.indices[0]]);
	v1 = input.weights[1] * mul(input.normal, BonePalette[input.indices[1]]);
	v2 = input.weights[2] * mul(input.normal, BonePalette[input.indices[2]]);
	v3 = (1-(input.weights[3]+input.weights[2]+input.weights[1]+input.weights[0]))
		 * mul(input.normal,BonePalette[input.indices[3]]);
	output.position = mul(output.position,mul(World,mul(View,Projection)));

    output.normal = mul(v0+v1+v2+v3,World);
	output.color = input.color;
	output.texcoord = input.texcoord;
	output.viewDirection = objectPosition - EyePosition;
}

void TransformPixel (in VS_OUTPUT input, out PS_OUTPUT output)
{
	if (LightingEnabled == false && TextureEnabled)
    {
		output.color.xyz = tex2D(BaseSampler,input.texcoord).xyz
            * saturate(EmissiveColor + DiffuseColor);
    }
    else if (LightingEnabled == false)
    {
        output.color.xyz = saturate(EmissiveColor + DiffuseColor);
    }
	else
	{

		float3 viewDirection = normalize(input.viewDirection);
		float3 normal = normalize(input.normal);
		
		float3 totalDiffuse = 
             (LightEnabled0 ? DiffuseColor * dot(-LightDirection0,normal) * LightDiffuseColor0 : 0) +
			 (LightEnabled1 ? DiffuseColor * dot(-LightDirection1,normal) * LightDiffuseColor1 : 0) +
			 (LightEnabled2 ? DiffuseColor * dot(-LightDirection2,normal) * LightDiffuseColor2 : 0);
		float3 totalSpecular = 
			(LightEnabled0 ? SpecularColor * 
				pow(saturate(dot(LightDirection0-2*normal*clamp(dot(LightDirection0,normal),-1,0)
					,viewDirection)),SpecularPower) * LightSpecularColor0 : 0) +
			(LightEnabled1 ? SpecularColor * 
				pow(saturate(dot(LightDirection1-2*normal*clamp(dot(LightDirection2,normal),-1,0)
					,viewDirection)),SpecularPower) * LightSpecularColor1 : 0) +
			(LightEnabled2 ? SpecularColor * 
				pow(saturate(dot(LightDirection2-2*normal*clamp(dot(LightDirection2,normal),-1,0)
					,viewDirection)),SpecularPower) * LightSpecularColor2 : 0);

		output.color.xyz = TextureEnabled ? tex2D(BaseSampler, input.texcoord).xyz 
            * saturate(AmbientLightColor+totalDiffuse+totalSpecular)
            : saturate(AmbientLightColor+totalDiffuse+totalSpecular);
		output.color.w   = input.color.w;
	}
}

technique TransformTechnique
{
	pass P0
	{
		VertexShader = compile vs_2_0 TransformVertex();
		PixelShader  = compile ps_2_0 TransformPixel();
	}
}