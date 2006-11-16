/*
 * ModelInfoReader.cs
 * Reads animation and blend transform data from a stream and converts it to
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
    /// A class that reads in an XNB stream and converts it to a ModelInfo object
    /// </summary>
    public class ModelInfoReader : ContentTypeReader<ModelInfo>
    {
        /// <summary>
        /// Reads in an XNB stream and converts it to a ModelInfo object
        /// </summary>
        /// <param name="input">The stream from which the data will be read</param>
        /// <param name="existingInstance">Not used</param>
        /// <returns>The unserialized ModelInfo object</returns>
        protected override ModelInfo Read(ContentReader input, ModelInfo existingInstance)
        {
            // Create the new object
            ModelInfo info = new ModelInfo();
            int numAnimations = input.ReadInt32();
            AnimationContentDictionary dict = new AnimationContentDictionary();
            // Read each animation
            for (int i = 0; i < numAnimations; i++)
            {
                AnimationContent content = new AnimationContent();
                KeyValuePair<string, AnimationContent> animationKeyPair
                    = new KeyValuePair<string, AnimationContent>(input.ReadString(),
                    content);
                content.Name = input.ReadString();
                content.Duration = input.ReadRawObject<TimeSpan>();
                int numChannels = input.ReadInt32();
                // Read each channel in the current animation
                for (int j = 0; j < numChannels; j++)
                {
                    AnimationChannel channel = new AnimationChannel();
                    KeyValuePair<string, AnimationChannel> channelPair =
                        new KeyValuePair<string, AnimationChannel>(input.ReadString(),
                        channel);
                    int numFrames = input.ReadInt32();
                    // Read each keyframe in the current channel
                    for (int k = 0; k < numFrames; k++)
                    {
                        AnimationKeyframe frame = new AnimationKeyframe(
                            input.ReadRawObject<TimeSpan>(),
                            input.ReadMatrix());
                        channel.Add(frame);
                    }
                    content.Channels.Add(channelPair.Key, channelPair.Value);
                }
                dict.Add(animationKeyPair.Key, animationKeyPair.Value);
            }
            info.Animations = dict;
            // Read the blend transform data
            info.BlendTransforms = input.ReadRawObject<Dictionary<string, Matrix>>();
            return info;
        }
    }
}