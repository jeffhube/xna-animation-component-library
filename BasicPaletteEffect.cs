/*
 * BasicPaletteEffect.cs
 * A basic matrix palette effect
 * Part of XNA Animation Component library, which is a library for animation
 * in XNA
 * 
 * Copyright (C) 2006 David Astle
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
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
            specColorParam, specPowerParam,texEnabledParam, texParam, paletteParam;
        private BasicDirectionalLight light0, light1, light2;
        private Vector3 eye;
        private static Vector3 zero = Vector3.Zero;


        internal BasicPaletteEffect(GraphicsDevice device,
            Effect cloneSource)
            : base(device, cloneSource)
        {
            paletteParam = Parameters["BonePalette"];
            texParam = Parameters["Texture"];
            texEnabledParam = Parameters["TextureEnabled"];
            worldParam = Parameters["World"];
            viewParam = Parameters["View"];
            projectionParam = Parameters["Projection"];
            ambientParam = Parameters["AmbientLightColor"];
            eyeParam = Parameters["EyePosition"];
            emissiveParam = Parameters["EmissiveColor"];
            lightEnabledParam = Parameters["LightingEnabled"];
            diffuseParam = Parameters["DiffuseColor"];
            specColorParam = Parameters["SpecularColor"];
            specPowerParam = Parameters["SpecularPower"];
            light0 = new BasicDirectionalLight(this, 0);
            light1 = new BasicDirectionalLight(this, 1);
            light2 = new BasicDirectionalLight(this, 2);
        }

        /// <summary>
        /// Creates a new instance of BasicPaletteEffect.
        /// </summary>
        /// <param name="content">The content containg the games services.</param>
        /// <returns>A new instance of BasicPaletteEffect</returns>
        public static BasicPaletteEffect FromContent(ContentManager content)
        {
            IGraphicsDeviceService deviceService = (IGraphicsDeviceService)content.ServiceProvider.GetService(
                typeof(IGraphicsDeviceService));
            Effect cloneSource = content.Load<Effect>(AssetName);
            BasicPaletteEffect effect = new BasicPaletteEffect(deviceService.GraphicsDevice,
                cloneSource);
            return effect;
        }

        /// <summary>
        /// Clones the current BasicPaletteEffect class.
        /// </summary>
        /// <param name="device">The device to contain the new instance.</param>
        /// <returns>A clone of the current instance.</returns>
        public override Effect Clone(GraphicsDevice device)
        {
            return new BasicPaletteEffect(device,base.Clone(device));
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
        /// Replaces all instances of BasicEffect with BasicPaletteEffect for meshes
        /// that contain skinning info.
        /// </summary>
        /// <param name="content">The content manager containg the game's services.</param>
        /// <param name="model">The model</param>
        public static void ReplaceBasicEffects(ContentManager content, Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    if (!(part.Effect is BasicEffect))
                        continue;
                    VertexDeclaration decl = part.VertexDeclaration;
                    VertexElement[] elements = decl.GetVertexElements();
                    bool isSkinned = false;
                    foreach (VertexElement e in elements)
                    {
                        if (e.VertexElementUsage == VertexElementUsage.BlendWeight ||
                            e.VertexElementUsage == VertexElementUsage.BlendIndices)
                        {
                            isSkinned = true;
                            break;
                        }
                    }
                    if (isSkinned)
                    {
                        BasicPaletteEffect effect = FromContent(content);
                        BasicEffect basic = (BasicEffect)part.Effect;
                        effect.SetParamsFromBasicEffect(basic);
                        part.Effect = effect;
                    }
                }
            }
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
                this.lightDirParam = effect.Parameters["LightDirection" + lightNum.ToString()];
                this.difColorParam = effect.Parameters["LightDiffuseColor" + lightNum.ToString()];
                this.specColorParam = effect.Parameters["LightSpecularColor" + lightNum.ToString()];
                this.lightEnabledParam = effect.Parameters["LightEnabled" + lightNum.ToString()];
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
        public Matrix[] BonePalette
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
        public const int PALETTE_SIZE = 50;

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
                Vector3.Transform(ref zero, ref value, out eye);
                eye.Z *= -1;
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

        internal static string AssetName
        {
            get
            {
                return "_____BasicPaletteEffect_____";
            }
        }

        internal static string RelativeFilename
        {
            get
            {
                return "_____BasicPaletteEffect.fx";
            }
        }

        internal static string SourceCode
        {
            get
            {
                return @"

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
}";
            
            }
        }
    }
}