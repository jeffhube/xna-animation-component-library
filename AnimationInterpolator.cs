/*
 * AnimationInterpolator.cs
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
#endregion

namespace Animation
{
    /// <summary>
    /// Stores an elapsed time that is used to compute bone poses for
    /// an animation
    /// </summary>
    public class AnimationInterpolator
    {
        #region Member Variables
        // private Model model;
        // Current animation time for the creator
        private long curTime = 0;
        ModelBoneManager manager;
        private ModelAnimation animation;
        private long animationDuration;
        // Stores the current key frame while creating the table.
        // KeyFrameIndices[i] = the current frame for the bone with index i
        private int[] keyFrameIndices;
        private InterpolationMethod interpMethod = InterpolationMethod.SphericalLinear;
        SortedList<string, BoneKeyframeCollection> boneAnims;
        private MeshInfo meshInfo;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of BonePoseCreator
        /// </summary>
        public AnimationInterpolator(ModelBoneManager manager, MeshInfo meshInfo)
        {
           // this.model = model;
            this.manager = manager;
            this.meshInfo = meshInfo;
            keyFrameIndices = new int[manager.Count];
        }
        public AnimationInterpolator(ModelBoneManager manager, MeshInfo meshInfo, InterpolationMethod interpMethod)
            : this(manager, meshInfo)
        {
            this.interpMethod = interpMethod;
        }
    


        #endregion

        public MeshInfo MeshInfo
        {
            get { return meshInfo; }
        }
        public long CurrentTime
        {
            get { return curTime; }
            set
            {
                long timeStep = TimeSpan.FromSeconds(1).Ticks / 80;
                while (Math.Abs(curTime - value) > Math.Abs((curTime+timeStep)-value))
                {
                    AdvanceTime(timeStep);
                }
                curTime = value;
            }
        }
        #region Methods
        /// <summary>
        /// Creates a bone pose
        /// </summary>
        /// <param name="channel">The animation channel</param>
        /// <param name="boneIndex">The index of the bone attached to the channel</param>
        private void CreatePose(BoneKeyframeCollection channel, int boneIndex)
        {
            if (channel.Count == 1)
            {
                return;
            }
            // Index in the channel of the current key frame
            int curFrameIndex = keyFrameIndices[boneIndex];
            // References to current and next frame
            BoneKeyframe curFrame = channel[curFrameIndex], nextFrame =
                channel[curFrameIndex + 1];

            double interpAmount = (nextFrame.Time - curFrame.Time);
            interpAmount = (curTime - curFrame.Time) / interpAmount;

            if (interpMethod == InterpolationMethod.SphericalLinear)
            {
                Matrix curMatrix = curFrame.Transform;
                Matrix nextMatrix = nextFrame.Transform;
                // An expensive operation that decomposes both matrices and interpolates with
                // quaternions and vectors
                manager[boneIndex].Transform =
                    Util.SlerpMatrix(
                    curMatrix,
                    nextMatrix,
                    interpAmount);

            }
            else if (interpMethod == InterpolationMethod.Linear)
            {
                // simple linear interpolation

                manager[boneIndex].Transform = Matrix.Lerp(curFrame.Transform,
                     nextFrame.Transform,
                    (float)interpAmount);
            }
        }


        internal void CreateModelPoseSet(Matrix[] modelPoseSet)
        {

            // Create each pose
            foreach (KeyValuePair<string,BoneKeyframeCollection> k in boneAnims)
            {
                CreatePose(k.Value, manager[k.Key].Index);
            }
            manager.CopyAbsoluteBoneTransformsTo(modelPoseSet);

        }

        public void CreateMeshPoseSet(
            Matrix[] poseSet, 
            Matrix[] modelPose,
            SkinTransform[] skinTransforms,
            int meshBoneIndex)
        {
            if (skinTransforms == null)
            {
                poseSet[0] = modelPose[meshBoneIndex];
            }
            else
            {
                for (int i = 0; i < skinTransforms.Length; i++)
                {
                    poseSet[i] = skinTransforms[i].Transform *
                        modelPose[manager[skinTransforms[i].BoneName].Index];
                }
            }
        }



        /// <summary>
        /// Resets the bone pose to the initial state
        /// </summary>
        public void Reset()
        {
            curTime = 0;
            System.Array.Clear(keyFrameIndices, 0, keyFrameIndices.Length);
        }

        public ModelAnimation Animation
        {
            get { return animation; }
            set
            {
                if (value == animation)
                    return;
                animation = value;
                animationDuration = value.Duration;
                boneAnims = animation.BoneAnimations;
                Reset();
            }
        }

        public ModelBoneManager ModelBoneManager
        {
            get { return manager; }
        }

        public InterpolationMethod InterpolationMethod
        {
            get
            {
                return interpMethod;
            }
            set
            {
                interpMethod = value;
            }
        }

        /// <summary>
        /// Advances the animation frame but does not do any interpolation
        /// </summary>
        /// <param name="time">The amount of time to advance the animation</param>
        public void AdvanceTime(long time)
        {
            if (time <= 0)
            {
                return;
            }

            curTime += time;
            if (curTime > animationDuration)
            {
                for (int i = 0; i < keyFrameIndices.Length; i++)
                    keyFrameIndices[i] = 0;
                curTime = 0;
            }

            // Update the key frame indices for each channel
            foreach (KeyValuePair<string, BoneKeyframeCollection> k in animation.BoneAnimations)
            {
                if (k.Value.Count == 1)
                    continue;
                time = curTime;
                BoneKeyframeCollection channel = k.Value;

                if (time > channel[channel.Count - 1].Time)
                    time = (time %
                        channel[channel.Count - 1].Time);
                int boneIndex = manager[k.Key].Index;
                int curFrameIndex = keyFrameIndices[boneIndex];
                while (time > channel[keyFrameIndices[boneIndex] + 1].Time)
                    keyFrameIndices[boneIndex]++;
            }
        }

        #endregion
    }

}
