/*
 * PaletteEffectContent.cs
 * Attaches BasicPaletteEffect instances to models and writes it in XNB format
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Animation.Content
{

    /// <summary>
    /// Reads a BasicPaletteEffect from the content pipeline
    /// </summary>
    public class PaletteEffectReader : ContentTypeReader<BasicPaletteEffect>
    {
        /// <summary>
        /// Reads a BasicPaletteEffect
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="existingInstance">N/A</param>
        /// <returns>A new instance of BasicPaletteEffect</returns>
        protected override BasicPaletteEffect Read(ContentReader input, BasicPaletteEffect existingInstance)
        {

            ContentManager manager = input.ContentManager;
            IGraphicsDeviceService graphics =
                (IGraphicsDeviceService)manager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            byte[] effectCode = input.ReadBytes(input.ReadInt32());
            BasicPaletteEffect effect = new BasicPaletteEffect(graphics.GraphicsDevice,
                effectCode);
            if (input.ReadBoolean())
                effect.Texture = input.ReadExternalReference<Texture2D>();
            if (input.ReadBoolean())
                effect.SpecularPower = input.ReadSingle();
            if (input.ReadBoolean())
                effect.SpecularColor = input.ReadVector3();
            if (input.ReadBoolean())
                effect.EmissiveColor = input.ReadVector3();
            if (input.ReadBoolean())
                effect.DiffuseColor = input.ReadVector3();


            return effect;

        }
    }

    [ContentTypeWriter]
    internal class PaletteEffectWriter : ContentTypeWriter<PaletteMaterialContent>
    {
        protected override void Write(ContentWriter output, PaletteMaterialContent value)
        {

            output.Write(AnimatedModelProcessor.paletteByteCode.Length);
            output.Write(AnimatedModelProcessor.paletteByteCode);
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

        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(PaletteEffectReader).AssemblyQualifiedName;
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(BasicPaletteEffect).AssemblyQualifiedName;
        }
    }


    internal class PaletteMaterialContent : BasicMaterialContent
    {
        public PaletteMaterialContent(BasicMaterialContent content, byte[] byteCode,
            ContentProcessorContext context)
        {
            this.Alpha = content.Alpha;
            this.DiffuseColor = content.DiffuseColor;
            this.EmissiveColor = content.EmissiveColor;
            this.Name = content.Name;
            this.SpecularColor = content.SpecularColor;
            this.SpecularPower = content.SpecularPower;
            this.Texture = content.Texture;
            this.VertexColorEnabled = content.VertexColorEnabled;

            OpaqueData.Add("EffectCode", byteCode);
        }

    }
}
