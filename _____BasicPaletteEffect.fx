
#ifndef MATRIX_PALETTE_SIZE_DEFAULT
#define MATRIX_PALETTE_SIZE_DEFAULT 50
#endif
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
uniform extern float4x4 BonePalette[MATRIX_PALETTE_SIZE_DEFAULT];
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
	// This is the influence of bone one on the current vertex; which is the first weight of the
	// current vertex (between 0.0f and 1.0f) time the vertex, multiplied by the bone
	float4 v0 = input.weights[0] * mul(input.position,BonePalette[input.indices[0]]);
	
	// This is the influence of bone two on the current vertex; which is the second weight of the
	// current vertex (between 0.0f and 1.0f) times the vertex, multiplied by the bone
	float4 v1 = input.weights[1] * mul(input.position,BonePalette[input.indices[1]]);
	
	// This is the influence of bone three on the current vertex; which is the third weight of the
	// current vertex (between 0.0f and 1.0f) times the vertex, multiplied by the bone
	float4 v2 = input.weights[2] * mul(input.position,BonePalette[input.indices[2]]);
	
	// This is the influence of bone four on the current vertex; which is (1.0f - (sum of weights 1-3))
	// times the vertex, multiplied by the bone.  The formula for 1.0f - (sum of weights 1-3) normalizes
	// the weight so that the sum of the weights is 1.0f.
	float4 v3 = (1-(input.weights[3]+input.weights[2]+input.weights[1]+input.weights[0]))
		 * mul(input.position,BonePalette[input.indices[3]]);
		 
	// Now the objects position in local space as defined by all bones is just the sum of
	// v0-v4
    output.position = v0+v1+v2+v3;
      
    // This is the vector between the camera and the object in world space, which is used
    // for phong lighting calculation in the pixel shader
	output.viewDirection = mul(output.position, World) - EyePosition;
	
	// Now we apply the same formula as above for each bone's influence, except this time we
	// calculate the new normal
	v0 = input.weights[0] * mul(input.normal, BonePalette[input.indices[0]]);
	v1 = input.weights[1] * mul(input.normal, BonePalette[input.indices[1]]);
	v2 = input.weights[2] * mul(input.normal, BonePalette[input.indices[2]]);
	v3 = (1-(input.weights[3]+input.weights[2]+input.weights[1]+input.weights[0]))
		 * mul(input.normal,BonePalette[input.indices[3]]);

	// This is the final position of the vertex, and where it will be drawn on the screen
	output.position = mul(output.position,mul(World,mul(View,Projection)));

	// Same for the normal
    output.normal = mul(v0+v1+v2+v3,World);
	output.color = input.color;
	output.texcoord = input.texcoord;

}

// This takes the transformed normal as influenced by the bones (all the matrix palette transformations
// occur in TransformVertex), and applies 3 directional phong lights to them
void TransformPixel (in VS_OUTPUT input, out PS_OUTPUT output)
{
	// The general formula for the final color without lights is (original color + diffuse color +
	// emissive color).  When textures are active, we multiply this by the color of the texture
	// at the current texture coordinate for this vertex.
	if (LightingEnabled == false && TextureEnabled)
    {
		output.color.xyz = tex2D(BaseSampler,input.texcoord).xyz
            * saturate(EmissiveColor + DiffuseColor);
    }
    // Same as above, except no texture
    else if (LightingEnabled == false)
    {
        output.color.xyz = saturate(EmissiveColor + DiffuseColor);
    }
	else
	{
		// We need to normalize the normal and vector between the camera and object position
		// It won't even work if we normalize it in the vertex shader.
		float3 viewDirection = normalize(input.viewDirection);
		float3 normal = normalize(input.normal);
		
		// For phong shading, the final color of a pixel is equal to 
		// (sum of influence of lights + ambient constant) * texture color at given tex coord
		// First we find the diffuse light, which is simply the dot product of -1*light direction
		// and the normal.  This gives us the component of the reverse light direction in the
		// direction of the normal.  We then multiply the sum of each lights influence by a 
		// diffuse constant.
		float3 totalDiffuse = DiffuseColor*
             ((LightEnabled0 ? dot(-LightDirection0,normal) * LightDiffuseColor0 : 0) +
			 (LightEnabled1 ? dot(-LightDirection1,normal) * LightDiffuseColor1 : 0) +
			 (LightEnabled2 ?  dot(-LightDirection2,normal) * LightDiffuseColor2 : 0));
		
		// Now we do a similar strategy for specular light; sum the lights then multiply by
		// a specular constant.  In this formula, for each light, we find the dot product between
		// our viewDirection vector and the vector of reflection for the light ray.  This simulates
		// the "glare" or shinyness that occurs when looking at an object with a reflective surface
		// and when light can bounce of the surface and hit our eyes.
		// We need to be careful with what values we saturate and clamp, otherwise both sides
		// of the object will be lit, or other strange phenomenon will occur.
		float3 totalSpecular = 
			 SpecularColor *
			((LightEnabled0 ? 
				pow(saturate(-dot(LightDirection0-2*normal*clamp(dot(LightDirection0,normal),-1,0)
					,viewDirection)),SpecularPower) * LightSpecularColor0 : 0) +
			(LightEnabled1 ? 
				pow(saturate(-dot(LightDirection1-2*normal*clamp(dot(LightDirection2,normal),-1,0)
					,viewDirection)),SpecularPower) * LightSpecularColor1 : 0) +
			(LightEnabled2 ? 
				pow(saturate(-dot(LightDirection2-2*normal*clamp(dot(LightDirection2,normal),-1,0)
					,viewDirection)),SpecularPower) * LightSpecularColor2 : 0));

		// Now we apply the aforementioned phong formulate to get the final color
		output.color.xyz = TextureEnabled ? tex2D(BaseSampler, input.texcoord).xyz 
            * saturate(AmbientLightColor+totalDiffuse + saturate(totalSpecular))
            : saturate(AmbientLightColor+totalDiffuse);
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