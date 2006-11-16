/*
 * ModelInfoWriter.cs
 * Writes animation and vertex blending information to XNB format
 * a ModelInfo object
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
#endregion

namespace Animation.Content
{

    /// <summary>
    /// Writes ModelInfo data so it can be read into an object during runtime
    /// </summary>
    [ContentTypeWriter]
    public class ModelInfoWriter : ContentTypeWriter<ModelInfo>
    {
        /// <summary>
        /// Writes a ModelInfo object into XNB data
        /// </summary>
        /// <param name="output">The stream that contains the written data</param>
        /// <param name="value">The instance to be serialized</param>
        protected override void Write(ContentWriter output, ModelInfo value)
        {
            // This contains all the animations
            AnimationContentDictionary dict = value.Animations;
            // First write hte number of animations (0 if the dictionary is null)
            if (dict == null)
                output.Write(0);
            else
            {
                output.Write(dict.Count);
                // Write each animation set
                foreach (KeyValuePair<string, AnimationContent> k in dict)
                {
                    output.Write(k.Key);
                    output.Write(k.Value.Name);
                    output.WriteRawObject<TimeSpan>(k.Value.Duration);
                    // Write the number of channels
                    output.Write(k.Value.Channels.Count);
                    // Write each channel
                    foreach (KeyValuePair<string, AnimationChannel> c in k.Value.Channels)
                    {
                        output.Write(c.Key);
                        // Write the number of key frames for the current animation
                        // channel
                        output.Write(c.Value.Count);
                        // Write each key frame
                        foreach (AnimationKeyframe frame in c.Value)
                        {
                            output.WriteRawObject<TimeSpan>(frame.Time);
                            output.Write(frame.Transform);
                        }
                    }
                }
            }
            // Write the blend transforms
            output.WriteRawObject<Dictionary<string, Matrix>>(value.BlendTransforms);
        }

        /// <summary>
        /// Returns the string that describes the reader used to convert the
        /// stream of data into a ModelInfo object
        /// </summary>
        /// <param name="targetPlatform">The current platform</param>
        /// <returns>The string that describes the reader used for a ModelInfo object</returns>
        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(ModelInfoReader).AssemblyQualifiedName;
        }
        
        /// <summary>
        /// Returns the string that describes what type of object the stream
        /// will be converted into at runtime (ModelInf)
        /// </summary>
        /// <param name="targetPlatform">The current platform</param>
        /// <returns>The string that describes the run time type for the object written into
        /// the stream</returns>
        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(ModelInfo).AssemblyQualifiedName;
        }
    }
 
}