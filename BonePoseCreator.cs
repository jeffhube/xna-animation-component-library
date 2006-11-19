/*
 * BonePoseCreator.cs
 * Stores an elapsed time that is used to compute bone poses
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
#endregion

namespace Animation
{
    partial class AnimationController
    {
        /// <summary>
        /// Stores an elapsed time that is used to compute bone poses for
        /// an animation
        /// </summary>
        internal class BonePoseCreator
        {
            #region Member Variables
            // Current animation time for the creator
            private long curTime;
            private AnimationController controller;
            // The original bones of the animated model
            private Matrix[] originalBones;
            // Stores the current key frame while creating the table.
            // KeyFrameIndices[i] = the current frame for the bone with index i
            private int[] keyFrameIndices;
            #endregion

            #region Constructors
            /// <summary>
            /// Creates a new instance of BonePoseCreator
            /// </summary>
            public BonePoseCreator(AnimationController controller)
            {
                this.controller = controller;
                keyFrameIndices = new int[controller.model.Bones.Count];
                originalBones = new Matrix[controller.model.Bones.Count];
                controller.model.CopyBoneTransformsTo(originalBones);

            }


            #endregion

            public Matrix[] OriginalBones
            {
                get
                {
                    return originalBones;
                }
            }

            #region Methods
            /// <summary>
            /// Creates a bone pose
            /// </summary>
            /// <param name="channel">The animation channel</param>
            /// <param name="boneIndex">The index of the bone attached to the channel</param>
            private void CreatePose(AnimationChannel channel, int boneIndex)
            {
                if (channel.Count == 1)
                {
                    return;
                }
                // Index in the channel of the current key frame
                int curFrameIndex = keyFrameIndices[boneIndex];
                Console.WriteLine(curFrameIndex);
                // References to current and next frame
                AnimationKeyframe curFrame = channel[curFrameIndex], nextFrame =
                    channel[curFrameIndex + 1];

                double interpAmount = (nextFrame.Time - curFrame.Time).Ticks;
                interpAmount = (curTime - curFrame.Time.Ticks) / interpAmount;

                if (controller.interpMethod == InterpolationMethod.SphericalLinear)
                {
                    Matrix curMatrix =  curFrame.Transform;
                    Matrix nextMatrix = nextFrame.Transform;
                    // An expensive operation that decomposes both matrices and interpolates with
                    // quaternions and vectors
                    controller.model.Bones[boneIndex].Transform = 
                        Util.SlerpMatrix(
                        curMatrix,
                        nextMatrix,
                        interpAmount);

                }
                else if (controller.interpMethod == InterpolationMethod.Linear)
                {
                    // simple linear interpolation

                    controller.model.Bones[boneIndex].Transform = Matrix.Lerp(curFrame.Transform,
                         nextFrame.Transform,
                        (float)interpAmount);
                }
            }

            /// <summary>
            /// Creates a bone pose set for the current animation at the current time
            /// </summary>
            /// <param name="poseSet">A buffer that will store the absolute bone transforms,
            /// should be size of the models bones</param>
            public void CreatePoseSet(Matrix[] poseSet)
            {

                // Create each pose
                foreach (KeyValuePair<string, AnimationChannel> k in controller.anim.Channels)
                {
                    CreatePose(k.Value, controller.model.Bones[k.Key].Index);
                }
                controller.model.CopyAbsoluteBoneTransformsTo(poseSet);
                // apply any skin transforms
                foreach (KeyValuePair<string, Matrix> skinTransform in controller.blendTransforms)
                {
                    int index = controller.model.Bones[skinTransform.Key].Index;
                    poseSet[index] = skinTransform.Value * poseSet[index];
                }
  
            
            }

            /// <summary>
            /// Resets the bone pose to the initial state
            /// </summary>
            public void Reset()
            {
                curTime = 0;
            }

            /// <summary>
            /// Advances the animation frame but does not do any interpolation
            /// </summary>
            /// <param name="time">The amount of time to advance the animation</param>
            public void AdvanceTime(long time)
            {
                if (time == 0)
                    return;

                curTime += time;
                if (curTime > controller.AnimationDuration)
                {
                    for (int i = 0; i < keyFrameIndices.Length; i++)
                        keyFrameIndices[i] = 0;
                    curTime = 0;
                }

                // Update the key frame indices for each channel
                foreach (KeyValuePair<string, AnimationChannel> k in controller.anim.Channels)
                {
                    if (k.Value.Count == 1)
                        continue;
                    time = curTime;
                    AnimationChannel channel = k.Value;
                    
                    if (time > channel[channel.Count - 1].Time.Ticks)
                        time = (time %
                            channel[channel.Count - 1].Time.Ticks);
                    int boneIndex = controller.model.Bones[k.Key].Index;
                    int curFrameIndex = keyFrameIndices[boneIndex];
                    while (time > channel[keyFrameIndices[boneIndex] + 1].Time.Ticks)
                        keyFrameIndices[boneIndex]++;
                }
            }

            #endregion
        }
    }
}
