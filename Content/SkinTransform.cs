/*
 * SkinTransform.cs
 * Data structure that stores skin transforms passed from the content pipeline to the
 * client program
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
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Animation.Content
{

    /// <summary>
    /// A structure that contains information for a bindpose skin offset.
    /// </summary>
    public struct SkinTransform
    {
        /// <summary>
        /// The name of the bone attached to the transform
        /// </summary>
        public string BoneName;
        /// <summary>
        /// The transform for the bone
        /// </summary>
        public Matrix Transform;
    }

    [ContentTypeWriter]
    internal class SkinTransformWriter : ContentTypeWriter<SkinTransform>
    {
        protected override void Write(ContentWriter output, SkinTransform value)
        {
            output.Write(value.BoneName);
            output.Write(value.Transform);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(SkinTransformReader).AssemblyQualifiedName;
        }
        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(SkinTransform).AssemblyQualifiedName;
        }
    }

    internal class SkinTransformReader : ContentTypeReader<SkinTransform>
    {
        protected override SkinTransform Read(ContentReader input, SkinTransform existingInstance)
        {
            SkinTransform transform = new SkinTransform();
            transform.BoneName = input.ReadString();
            transform.Transform = input.ReadMatrix();
            return transform;
        }
    }
}
