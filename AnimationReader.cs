/*
 * ModelAnimationInfoReader.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
#endregion

namespace Animation.Content
{

    /// <summary>
    /// A class that reads in an XNB stream and converts it to a ModelInfo object
    /// </summary>
    public sealed class AnimationReader : ContentTypeReader<ModelAnimationCollection>
    {
        /// <summary>
        /// Reads in an XNB stream and converts it to a ModelInfo object
        /// </summary>
        /// <param name="input">The stream from which the data will be read</param>
        /// <param name="existingInstance">Not used</param>
        /// <returns>The unserialized ModelAnimationCollection object</returns>
        protected override ModelAnimationCollection Read(ContentReader input, ModelAnimationCollection existingInstance)
        {
            ModelAnimationCollection dict = new ModelAnimationCollection();

            int numAnimations = input.ReadInt32();


            for (int i = 0; i < numAnimations; i++)
            {
                string animationName = input.ReadString();
                int numBoneAnimations = input.ReadInt32();

                ModelAnimation anim = new ModelAnimation(animationName);

                for (int j = 0; j < numBoneAnimations; j++)
                {
                    string boneName = input.ReadString();
                    int numKeyFrames = input.ReadInt32();

                    BoneKeyframeCollection boneAnimation = new BoneKeyframeCollection(boneName);
                    for (int k = 0; k < numKeyFrames; k++)
                    {
                        BoneKeyframe frame = new BoneKeyframe(
                            input.ReadMatrix(),
                            input.ReadInt64());
                        boneAnimation.AddKeyframe(frame);
                    }

                    anim.AddBoneAnimation(boneAnimation);
                }
                dict.Add(animationName, anim);
            }
            return dict;
        }
    }
}