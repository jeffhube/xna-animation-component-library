/*
 * BasicPaletteEffect.cs
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

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
#endregion

namespace Animation
{

    /// <summary>
    /// Provides functionality similar to that of BasicEffect, but uses phong shading
    /// and a matrix palette.
    /// </summary>
    public sealed class BasicPaletteEffect : Effect
    {

        private EffectParameter worldParam, viewParam, projectionParam,
            ambientParam, eyeParam, emissiveParam, diffuseParam, lightEnabledParam,
            specColorParam, specPowerParam, texEnabledParam, texParam, paletteParam;
        private BasicDirectionalLight light0, light1, light2;
        private Vector3 eye;
        private static Vector3 zero = Vector3.Zero;


        internal BasicPaletteEffect(GraphicsDevice device, byte[] byteCode)
            : base(device, byteCode, CompilerOptions.PreferFlowControl, null)
        {
            InitializeParameters();
        }

        internal BasicPaletteEffect(GraphicsDevice device, Effect cloneSource)
            : base(device, cloneSource)
        {
            InitializeParameters();
        }

        public void EnableDefaultLighting()
        {
            this.LightingEnabled = true;

            this.light0.DiffuseColor = Color.White.ToVector3();
            this.light0.SpecularColor = Color.Black.ToVector3();
            this.light0.Direction = Vector3.Normalize(new Vector3(-1, 0, -1));
            this.light0.SpecularColor = Color.White.ToVector3();
            this.light1.DiffuseColor = Color.Black.ToVector3();
            this.light1.SpecularColor = Color.Black.ToVector3();
            this.light2.DiffuseColor = Color.Black.ToVector3();
            this.light2.SpecularColor = Color.Black.ToVector3();
            this.SpecularPower = 8.0f;
            this.light0.Enabled = true;
            this.light1.Enabled = false;
            this.light2.Enabled = false;
        }

        private void InitializeParameters()
        {
            paletteParam = Parameters["MatrixPalette"];
            texParam = Parameters["BasicTexture"];
            texEnabledParam = Parameters["TextureEnabled"];
            worldParam = Parameters["World"];
            viewParam = Parameters["View"];
            projectionParam = Parameters["Projection"];
            ambientParam = Parameters["AmbientLightColor"];
            eyeParam = Parameters["EyePosition"];
            emissiveParam = Parameters["EmissiveColor"];
            lightEnabledParam = Parameters["LightingEnable"];
            diffuseParam = Parameters["DiffuseColor"];
            specColorParam = Parameters["SpecularColor"];
            specPowerParam = Parameters["SpecularPower"];
            light0 = new BasicDirectionalLight(this, 0);
            light1 = new BasicDirectionalLight(this, 1);
            light2 = new BasicDirectionalLight(this, 2);
        }



        /// <summary>
        /// Clones the current BasicPaletteEffect class.
        /// </summary>
        /// <param name="device">The device to contain the new instance.</param>
        /// <returns>A clone of the current instance.</returns>
        public override Effect Clone(GraphicsDevice device)
        {
            return new BasicPaletteEffect(device, this);
        }


        /// <summary>
        /// Sets the parameters of this effect from a BasicEffect instance.
        /// </summary>
        /// <param name="effect">An instance containing the parameters to be copied.</param>
        public void SetParamsFromBasicEffect(BasicEffect effect)
        {
            AmbientLightColor = effect.AmbientLightColor;
            DiffuseColor = effect.DiffuseColor;
            LightingEnabled = effect.LightingEnabled;
            Projection = effect.Projection;
            World = effect.World;
            View = effect.View;
            SpecularColor = effect.SpecularColor;
            EmissiveColor = effect.EmissiveColor;
            SpecularPower = effect.SpecularPower;
            this.Texture = effect.Texture;
            this.TextureEnabled = effect.TextureEnabled;
            SetParamsFromBasicLight(effect.DirectionalLight0, light0);
            SetParamsFromBasicLight(effect.DirectionalLight1, light1);
            SetParamsFromBasicLight(effect.DirectionalLight2, light2);
        }

        private void SetParamsFromBasicLight(Microsoft.Xna.Framework.Graphics.BasicDirectionalLight
            source,
            BasicPaletteEffect.BasicDirectionalLight target)
        {
            target.SpecularColor = source.SpecularColor;
            target.Enabled = source.Enabled;
            target.Direction = source.Direction;
            target.DiffuseColor = source.DiffuseColor;
        }

        /// <summary>
        /// A basic directional light that uses phong shading.
        /// </summary>
        public sealed class BasicDirectionalLight
        {
            private BasicPaletteEffect effect;
            private EffectParameter lightDirParam;
            private EffectParameter difColorParam;
            private EffectParameter lightEnabledParam;
            private EffectParameter specColorParam;

            internal BasicDirectionalLight(BasicPaletteEffect effect, int lightNum)
            {
                this.effect = effect;
                string lightString = "DirLight" + lightNum;
                this.lightDirParam = effect.Parameters[lightString + "Direction"];
                this.difColorParam = effect.Parameters[lightString + "DiffuseColor"];
                this.specColorParam = effect.Parameters[lightString + "SpecularColor"];
                this.lightEnabledParam = effect.Parameters[lightString + "Enable"];
            }

            /// <summary>
            /// Enables or disables this light.
            /// </summary>
            public bool Enabled
            {
                get
                {
                    return lightEnabledParam.GetValueBoolean();
                }
                set
                {
                    lightEnabledParam.SetValue(value);
                }
            }

            /// <summary>
            /// Gets or sets the direction of this light.
            /// </summary>
            public Vector3 Direction
            {
                get
                {
                    return lightDirParam.GetValueVector3();
                }
                set
                {
                    lightDirParam.SetValue(Vector3.Normalize(value));
                }
            }


            /// <summary>
            /// Gets or sets the specular color of this light.
            /// </summary>
            public Vector3 SpecularColor
            {
                get
                {
                    return specColorParam.GetValueVector3();
                }
                set
                {

                    specColorParam.SetValue(value);
                }
            }

            /// <summary>
            /// Gets or sets the diffuse color of this light.
            /// </summary>
            public Vector3 DiffuseColor
            {
                get
                {
                    return difColorParam.GetValueVector3();
                }
                set
                {
                    difColorParam.SetValue(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the texture associated with this effect.
        /// </summary>
        public Texture2D Texture
        {
            get
            {
                return texParam.GetValueTexture2D();
            }
            set
            {
                texParam.SetValue(value);
            }
        }

        /// <summary>
        /// True if textures are enabled.
        /// </summary>
        public bool TextureEnabled
        {
            get
            {
                return texEnabledParam.GetValueBoolean();
            }
            set
            {
                texEnabledParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the bone palette values.
        /// </summary>
        public Matrix[] MatrixPalette
        {
            get
            {
                return paletteParam.GetValueMatrixArray(PALETTE_SIZE);
            }
            set
            {
                paletteParam.SetValue(value);
            }

        }

        /// <summary>
        /// The max number of bones in the effects matrix palette.
        /// </summary>
        public const int PALETTE_SIZE = 56;

        /// <summary>
        /// Gets directional light zero.
        /// </summary>
        public BasicDirectionalLight DirectionalLight0
        {
            get
            {
                return light0;
            }
        }

        /// <summary>
        /// Gets directional light one.
        /// </summary>
        public BasicDirectionalLight DirectionalLight1
        {
            get
            {
                return light1;
            }
        }

        /// <summary>
        /// Gets directional light two.
        /// </summary>
        public BasicDirectionalLight DirectionalLight2
        {
            get
            {
                return light2;
            }
        }

        /// <summary>
        /// Gets or sets the additive ambient color of this effect.
        /// </summary>
        public Vector3 AmbientLightColor
        {
            get
            {
                return ambientParam.GetValueVector3();
            }
            set
            {
                ambientParam.SetValue(value);
            }

        }

        /// <summary>
        /// Gets or sets the specular color of this effect.
        /// </summary>
        public Vector3 SpecularColor
        {
            get
            {
                return specColorParam.GetValueVector3();
            }
            set
            {
                specColorParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the specular power of this effect.
        /// </summary>
        public float SpecularPower
        {
            get
            {
                return specPowerParam.GetValueSingle();
            }
            set
            {
                specPowerParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the diffuse color of this effect.
        /// </summary>
        public Vector3 DiffuseColor
        {
            get
            {
                return diffuseParam.GetValueVector3();
            }
            set
            {
                diffuseParam.SetValue(value);
            }
        }

        /// <summary>
        /// Enables or disables lighting.
        /// </summary>
        public bool LightingEnabled
        {
            get
            {
                return lightEnabledParam.GetValueBoolean();
            }
            set
            {
                lightEnabledParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the emissive color of this effect.
        /// </summary>
        public Vector3 EmissiveColor
        {
            get
            {
                return emissiveParam.GetValueVector3();
            }
            set
            {
                emissiveParam.SetValue(value);
            }
        }

        /// <summary>
        /// Gets or sets the world matrix of this effect.
        /// </summary>
        public Matrix World
        {
            get
            {
                return worldParam.GetValueMatrix();
            }
            set
            {
                worldParam.SetValue(value);
            }
        }


        /// <summary>
        /// Gets or sets the view matrix of this effect.
        /// </summary>
        public Matrix View
        {
            get
            {
                return viewParam.GetValueMatrix();
            }
            set
            {
                Matrix inverseView = Matrix.Invert(value);
                Vector3.Transform(ref zero, ref inverseView, out eye);

                viewParam.SetValue(value);
                eyeParam.SetValue(eye);
            }
        }

        /// <summary>
        /// Gets or sets the projection matrix of this effect.
        /// </summary>
        public Matrix Projection
        {
            get
            {
                return projectionParam.GetValueMatrix();
            }
            set
            {
                projectionParam.SetValue(value);
            }
        }

        private static string LightingCode
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
	// This is the final position of the vertex, and where it will be drawn on the screen
	output.position = mul(output.position,mul(World,mul(View,Projection)));
";

            }
        }


        private static string ShaderVariables
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
uniform extern float4x4 MatrixPalette[" + PALETTE_SIZE.ToString() + @"];
float SpecularPower;
bool TextureEnabled;
bool LightingEnable = false;
texture BasicTexture;

sampler TextureSampler = sampler_state
{
   Texture = (BasicTexture);
   ADDRESSU = WRAP;
   ADDRESSV = WRAP;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
};
";
            }

        }

        private static string PixelShaderCode
        {
            get
            {
                return @"
// This takes the transformed normal as influenced by the bones (all the matrix palette transformations
// occur in TransformVertex), and applies 3 directional phong lights to them
void TransformPixel (in VS_OUTPUT input, out PS_OUTPUT output)
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
		output.color.w   = input.color.w;
	}
}
";
            }
        }

        public static string SourceCode12BonesPerVertex
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

        public static string SourceCode8BonesPerVertex
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
        public static string SourceCode4BonesPerVertex
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
}