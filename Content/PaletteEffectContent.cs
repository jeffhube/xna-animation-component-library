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

namespace Animation.Content
{



    [ContentTypeWriter]
    internal class PaletteEffectWriter : ContentTypeWriter<PaletteMaterialContent>
    {
        protected override void Write(ContentWriter output, PaletteMaterialContent value)
        {

            output.WriteRawObject<byte[]>(value.ByteCode);
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
        private byte[] byteCode;
        public PaletteMaterialContent(BasicMaterialContent content, byte[] byteCode,
            ContentProcessorContext context)
        {
            this.byteCode = byteCode;
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

        public byte[] ByteCode
        { get { return byteCode; } }

    }

}
