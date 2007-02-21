/*
 * PaletteEffectContent.cs
 * Copyright (c) 2006 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace XCLNA.XNA.Animation.Content
{



    [ContentTypeWriter]
    internal class PaletteMaterialWriter : ContentTypeWriter<PaletteMaterialContent>
    {
        protected override void Write(ContentWriter output, PaletteMaterialContent value)
        {

            output.WriteRawObject<byte[]>(value.ByteCode);
            output.Write(value.PaletteSize);
            bool hasTexture = value.Textures.ContainsKey("Texture");
            output.Write(hasTexture);
            if (hasTexture)
                output.WriteExternalReference<TextureContent>(value.Textures["Texture"]);
            output.Write(value.SpecularPower != null);
            if (value.SpecularPower != null)
                output.Write((float)value.SpecularPower);
            output.Write(value.SpecularColor != null);
            if (value.SpecularColor != null)
                output.Write((Vector3)value.SpecularColor);
            output.Write(value.EmissiveColor != null);
            if (value.EmissiveColor != null)
                output.Write((Vector3)value.EmissiveColor);
            output.Write(value.DiffuseColor != null);
            if (value.DiffuseColor != null)
                output.Write((Vector3)value.DiffuseColor);
            output.Write(value.Alpha != null);
            if (value.Alpha != null)
                output.Write((float)value.Alpha);

        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            if (targetPlatform == TargetPlatform.Xbox360)
            {
                return "XCLNA.XNA.Animation.Content.PaletteEffectReader, "
                    + "XCLNA.XNA.Animation360, "
                    + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            }
            else
            {
                return "XCLNA.XNA.Animation.Content.PaletteEffectReader, "
                    + "XCLNA.XNA.Animationx86, "
                    + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            }
            
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {

            if (targetPlatform == TargetPlatform.Xbox360)
            {
                return "XCLNA.XNA.Animation.BasicPaletteEffect, XCLNA.XNA.Animation360, "
                    + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            }
            else
            {
                return "XCLNA.XNA.Animation.BasicPaletteEffect, XCLNA.XNA.Animationx86, "
                    + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            }
        }
    }

    public  class PaletteSourceCode
    {
        public readonly int PALETTE_SIZE;
        public PaletteSourceCode(int size)
        {
            PALETTE_SIZE = size;
        }

        private string LightingCode
        {
            get
            {
                return @"


    normal = normalize(mul(normal,World));

    float3 totalDiffuse = DiffuseColor*
         ((DirLight0Enable ? dot(-DirLight0Direction,normal) * DirLight0DiffuseColor : 0) +
		 (DirLight1Enable ? dot(-DirLight1Direction,normal) * DirLight1DiffuseColor : 0) +
		 (DirLight2Enable ?  dot(-DirLight2Direction,normal) * DirLight2DiffuseColor : 0));


    // This is the vector between the camera and the object in world space, which is used
    // for phong lighting calculation in the pixel shader
	float3 viewDirection = normalize(EyePosition - mul(output.position,World));
    float3 spec0,spec1,spec2;
    if (DirLight0Enable)
    {
        float val = dot(-DirLight0Direction,normal);
        if (val < 0)
        {
            spec0 = float3(0,0,0);
        }
        else
        {
            spec0 = DirLight0SpecularColor *
                (pow(val*dot(reflect(DirLight0Direction,normal),viewDirection),SpecularPower));
        }
    }
    else
        spec0=float3(0,0,0);

    if (DirLight1Enable)
    {
        float val = dot(-DirLight1Direction,normal);
        if (val < 0)
        {
            spec1 = float3(0,0,0);
        }
        else
        {
            spec1 = DirLight1SpecularColor *
                (pow(val*dot(reflect(DirLight1Direction,normal),viewDirection),SpecularPower));
        }
    }
    else
        spec1=float3(0,0,0);

    if (DirLight2Enable)
    {
        float val = dot(-DirLight2Direction,normal);
        if (val < 0)
        {
            spec2 = float3(0,0,0);
        }
        else
        {
            spec2 = DirLight2SpecularColor *
                (pow(val*dot(reflect(DirLight2Direction,normal),viewDirection),SpecularPower));
        }
    }
    else
        spec2=float3(0,0,0);
    

	float3 totalSpecular = SpecularColor * (spec0+spec1+spec2);
	output.color.xyz = saturate(AmbientLightColor+totalDiffuse + totalSpecular);
    output.color.w=1.0;
	output.texcoord = input.texcoord;
    output.distance = distance(EyePosition, output.position.xyz);
	// This is the final position of the vertex, and where it will be drawn on the screen
	output.position = mul(output.position,mul(World,mul(View,Projection)));

";

            }
        }
        private string ShaderVariables
        {
            get
            {
                return @"

float4x4 World;
float4x4 View;
float4x4 Projection;
float3 DiffuseColor;
float3 SpecularColor;
float3 AmbientLightColor = float3(0,0,0);
float3 EmissiveColor;
float3 EyePosition;
float3 FogColor;
bool   FogEnable;
float FogStart;
float FogEnd;
bool   DirLight0Enable;
bool   DirLight1Enable;
extern bool    DirLight2Enable;
float3 DirLight0Direction;
float3 DirLight1Direction;
float3 DirLight2Direction;
float3 DirLight0DiffuseColor;
float3 DirLight1DiffuseColor;
float3 DirLight2DiffuseColor;
float3 DirLight0SpecularColor;
float3 DirLight1SpecularColor;
float3 DirLight2SpecularColor;
float Alpha;
uniform extern float4x4 MatrixPalette[" + this.PALETTE_SIZE.ToString() + @"];
float SpecularPower;
bool TextureEnabled;
bool LightingEnable = false;
texture BasicTexture;

sampler TextureSampler = sampler_state
{
   Texture = (BasicTexture);

};
";
            }

        }
        private string PixelShaderCode
        {
            get
            {
                return @"
// This takes the transformed normal as influenced by the bones (all the matrix palette transformations
// occur in TransformVertex), and applies 3 directional phong lights to them
void TransformPixel (in PS_INPUT input, out PS_OUTPUT output)
{
	// The general formula for the final color without lights is (original color + diffuse color +
	// emissive color).  When textures are active, we multiply this by the color of the texture
	// at the current texture coordinate for this vertex.
	if (LightingEnable == false && TextureEnabled)
    {
		output.color.xyz = tex2D(TextureSampler,input.texcoord).xyz * saturate(EmissiveColor + DiffuseColor);
    }
    // Same as above, except no texture
    else if (LightingEnable == false)
    {
       output.color.xyz = saturate(EmissiveColor + DiffuseColor);
    }
	else
	{
		
        // These comments are old but I left them in because they give some idea of how the lighting
        // works.  Lighting is now done in the vertex shader because older 2.0 cards didn't
        // support pixel shader lighting (like radeon 9800)

		// For phong shading, the final color of a pixel is equal to 
		// (sum of influence of lights + ambient constant) * texture color at given tex coord
		// First we find the diffuse light, which is simply the dot product of -1*light direction
		// and the normal.  This gives us the component of the reverse light direction in the
		// direction of the normal.  We then multiply the sum of each lights influence by a 
		// diffuse constant.
		// Now we do a similar strategy for specular light; sum the lights then multiply by
		// a specular constant.  In this formula, for each light, we find the dot product between
		// our viewDirection vector and the vector of reflection for the light ray.  This simulates
		// the glare or shinyness that occurs when looking at an object with a reflective surface
		// and when light can bounce of the surface and hit our eyes.
		// We need to be careful with what values we saturate and clamp, otherwise both sides
		// of the object will be lit, or other strange phenomenon will occur.
		// Now we apply the aforementioned phong formulate to get the final color
		output.color.xyz = TextureEnabled ? tex2D(TextureSampler, input.texcoord).xyz  * input.color.xyz
            : input.color.xyz;

	}
    output.color.w   = 
         TextureEnabled ? tex2D(TextureSampler, input.texcoord).w * Alpha : Alpha;
    

    if (FogEnable)
    {
        float dist = (input.distance - FogStart) / (FogEnd - FogStart);
        dist = saturate(dist);
        float3 distv = float3(dist,dist,dist);
        distv = lerp(output.color.xyz,FogColor,distv);
        output.color.xyz = distv;
    }
}
";
            }
        }

        public string SourceCode12BonesPerVertex
        {
            get
            {
                return ShaderVariables + @"


struct VS_INPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL0;
	half4 indices : BLENDINDICES0;
    half4 indices1: BLENDINDICES1;
    half4 indices2: BLENDINDICES2;
	float4 weights : BLENDWEIGHT0;
    float4 weights1: BLENDWEIGHT1;
    float4 weights2: BLENDWEIGHT2;
};

struct VS_OUTPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
};

struct PS_OUTPUT
{
	float4 color : COLOR;
};

struct SKIN_OUTPUT
{
    float4 position;
    float4 normal;
};

SKIN_OUTPUT Skin12( const VS_INPUT input)
{
    SKIN_OUTPUT output = (SKIN_OUTPUT)0;

    float lastWeight = 1.0;
    float weight = 0;
    for (int i = 0; i < 4; ++i)
    {
        weight = input.weights[i];
        lastWeight -= weight;
        output.position += mul( input.position, MatrixPalette[input.indices[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices[i]]) * weight;
    }
    for (int i = 0; i < 4; ++i)
    {
        weight = input.weights1[i];
        lastWeight -= weight;
        output.position += mul( input.position, MatrixPalette[input.indices1[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices1[i]]) * weight;
    }
    for (int i = 0; i < 3; ++i)
    {
        weight = input.weights2[i];
        lastWeight -= weight;
        output.position += mul(input.position,MatrixPalette[input.indices2[i]]) * weight;
        output.normal += mul(input.normal, MatrixPalette[input.indices2[i]]) * weight;
    }

    output.position += mul( input.position, MatrixPalette[input.indices2[3]])*lastWeight;
    output.normal       += mul( input.normal  , MatrixPalette[input.indices2[3]])*lastWeight;
    return output;
};



void TransformVertex (in VS_INPUT input, out VS_OUTPUT output)
{

    float3 inputN = normalize(input.normal);
    SKIN_OUTPUT skin = Skin12(input);
    output.position=skin.position;
    float3 normal = skin.normal;



    " + LightingCode + @"
}

" + PixelShaderCode + @"



technique TransformTechnique
{
	pass P0
	{
		VertexShader = compile vs_2_0 TransformVertex();
		PixelShader  = compile ps_2_0 TransformPixel();
	}
}";

            }
        }
        public string SourceCode8BonesPerVertex
        {
            get
            {
                return ShaderVariables + @"


struct VS_INPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL0;
	half4 indices : BLENDINDICES0;
    half4 indices1: BLENDINDICES1;
	float4 weights : BLENDWEIGHT0;
    float4 weights1: BLENDWEIGHT1;
};

struct VS_OUTPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
};

struct PS_OUTPUT
{
	float4 color : COLOR;
};

struct SKIN_OUTPUT
{
    float4 position;
    float4 normal;
};

SKIN_OUTPUT Skin8( const VS_INPUT input)
{
    SKIN_OUTPUT output = (SKIN_OUTPUT)0;

    float lastWeight = 1.0;
    float weight = 0;
    for (int i = 0; i < 4; ++i)
    {
        weight = input.weights[i];
        lastWeight -= weight;
        output.position += mul( input.position, MatrixPalette[input.indices[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices[i]]) * weight;
    }
    for (int i = 0; i < 3; ++i)
    {
        weight = input.weights1[i];
        lastWeight -= weight;
        output.position += mul( input.position, MatrixPalette[input.indices1[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices1[i]]) * weight;
    }

    output.position += mul( input.position, MatrixPalette[input.indices1[3]])*lastWeight;
    output.normal       += mul( input.normal  , MatrixPalette[input.indices1[3]])*lastWeight;
    return output;
};



void TransformVertex (in VS_INPUT input, out VS_OUTPUT output)
{

    float3 inputN = normalize(input.normal);
    SKIN_OUTPUT skin = Skin8(input);
    output.position=skin.position;
    float3 normal = skin.normal;



    " + LightingCode + @"
}

" + PixelShaderCode + @"



technique TransformTechnique
{
	pass P0
	{
		VertexShader = compile vs_2_0 TransformVertex();
		PixelShader  = compile ps_2_0 TransformPixel();
	}
}";

            }
        }

        /// <summary>
        /// Returns the source code for BasicPaletteEffect
        /// </summary>
        public string SourceCode4BonesPerVertex
        {
            get
            {
                return ShaderVariables + @"


struct VS_INPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL0;
	half4 indices : BLENDINDICES0;
	float4 weights : BLENDWEIGHT0;
};

struct VS_OUTPUT
{
	float4 position : POSITION;
	float4 color : COLOR;
	float2 texcoord : TEXCOORD0;
    float  distance : TEXCOORD1;
};

struct PS_INPUT
{
    float4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    float  distance : TEXCOORD1;
};

struct PS_OUTPUT
{
	float4 color : COLOR;
};

struct SKIN_OUTPUT
{
    float4 position;
    float4 normal;
};


SKIN_OUTPUT Skin4( const VS_INPUT input)
{
    SKIN_OUTPUT output = (SKIN_OUTPUT)0;


    float lastWeight = 1.0;
    float weight = 0;
    for (int i = 0; i < 3; ++i)
    {
        weight = input.weights[i];
        lastWeight -= weight;
        output.position     += mul( input.position, MatrixPalette[input.indices[i]]) * weight;
        output.normal       += mul( input.normal  , MatrixPalette[input.indices[i]]) * weight;
    }
    output.position     += mul( input.position, MatrixPalette[input.indices[3]])*lastWeight;
    output.normal       += mul( input.normal  , MatrixPalette[input.indices[3]])*lastWeight;
    return output;
};

void TransformVertex (in VS_INPUT input, out VS_OUTPUT output)
{

    float3 inputN = normalize(input.normal);
    SKIN_OUTPUT skin = Skin4(input);
    output.position=skin.position;
    float3 normal = skin.normal;



    " + LightingCode + @"
}

" + PixelShaderCode + @"



technique TransformTechnique
{
	pass P0
	{
		VertexShader = compile vs_2_0 TransformVertex();
		PixelShader  = compile ps_2_0 TransformPixel();
	}
}";
            }
        }
    }

    [ContentProcessor]
    public class PaletteInfoProcessor : ContentProcessor<PaletteInfo,
        PaletteMaterialContent>
    {

        public override PaletteMaterialContent Process(PaletteInfo input,
            ContentProcessorContext context)
        {
            EffectProcessor effectProcessor = new EffectProcessor();
            EffectContent effectContent = new EffectContent();
            effectContent.EffectCode = input.SourceCode;

            CompiledEffect compiled = effectProcessor.Process(effectContent, context);

            PaletteMaterialContent content = new PaletteMaterialContent();
            content.PaletteSize = input.PaletteSize;
            content.ByteCode = compiled.GetEffectCode();
            BasicMaterialContent basic = input.BasicContent;
            content.Alpha = basic.Alpha;
            content.DiffuseColor = basic.DiffuseColor;
            content.EmissiveColor = basic.EmissiveColor;
            content.Name = basic.Name;
            content.SpecularColor = basic.SpecularColor;
            content.SpecularPower = basic.SpecularPower;
            content.Texture = basic.Texture;
            content.VertexColorEnabled = basic.VertexColorEnabled;
            return content;

        }
    }

    public class PaletteInfo
    {
        private string sourceCode;
        private int paletteSize;
        private BasicMaterialContent basicContent;
        public PaletteInfo(string sourceCode, int paletteSize,
            BasicMaterialContent basicContent)
        {
            this.sourceCode = sourceCode;
            this.paletteSize = paletteSize;
            this.basicContent = basicContent;
        }

        public string SourceCode
        { get { return sourceCode; } }
        public int PaletteSize
        { get { return paletteSize; } }
        public BasicMaterialContent BasicContent
        { get { return basicContent; } }
    }

    public class PaletteMaterialContent : BasicMaterialContent
    {
        private byte[] byteCode;
        private int paletteSize;

        public PaletteMaterialContent()
        {
        }

        public int PaletteSize
        {
            get { return paletteSize; }
            set 
            { 
                paletteSize = value; 
            }
        }

        public byte[] ByteCode
        {
            get { return (byte[])byteCode.Clone(); }
            set { byteCode = value; }
        }



    }

}
