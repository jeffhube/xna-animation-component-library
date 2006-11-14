/*
 *  BonePoseCreator.cs
 *  Stores an elapsed time that is used to compute bone poses for
 *  an animation
 *  Copyright (C) 2006 XNA Animation Component Library CodePlex Project
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA
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
    /// <summary>
    /// Stores an elapsed time that is used to compute bone poses for
    /// an animation
    /// </summary>
    public class BonePoseCreator
    {
        #region Member Variables
        // true if model bones should not be changed
        private bool preserveBones;
        // The original bones of the animated model, used for resetting the pose
        // position and if preserveBones is true
        private Matrix[] originalBones;
        // A buffer used for storing the latest pose of the model if preserveBones
        // is set to true
        private Matrix[] bones = null;
        // Current animation time in milliseconds
        private double curTime;
        // Stores the current key frame while creating the table.
        // KeyFrameIndices[i] = the current frame for the bone with index i
        private int[] keyFrameIndices;
        private Model model;
        private InterpolationMethod interpMethod;
        // duration of animation
        private double totalMilliseconds;
        // Contains the data for the animation
        private AnimationContent animation;
        // Maps bone names to their blend transform, which, when applied to a bone,
        // creates a matrix that transforms vertices in the bones local space
        // empty if there is no skinning information
        private Dictionary<string, Matrix> blendTransforms;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of BonePoseCreator
        /// </summary>
        /// <param name="model">The model for which bone poses will be created</param>
        /// <param name="animation">The animation which has the keyframes that determine the
        /// bone poses</param>
        /// <param name="blendTransforms">The skin weight transforms for skinned models; empty
        /// if there is no skinning information</param>
        /// <param name="interpMethod">Method of interpolation</param>
        /// <param name="preserveBones">True if model bones should not be changed</param>
        public BonePoseCreator(Model model, AnimationContent animation,
            Dictionary<string, Matrix> blendTransforms,
            InterpolationMethod interpMethod,
            bool preserveBones)
        {
            this.model = model;
            this.animation = animation;
            this.blendTransforms = blendTransforms;
            keyFrameIndices = new int[model.Bones.Count];
            originalBones = new Matrix[model.Bones.Count];
            model.CopyBoneTransformsTo(originalBones);
            this.interpMethod = interpMethod;
            this.totalMilliseconds = animation.Duration.TotalMilliseconds;
            this.preserveBones = preserveBones;
            if (preserveBones)
            {
                bones = new Matrix[model.Bones.Count];
                model.CopyBoneTransformsTo(bones);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// The model for the animation
        /// </summary>
        public Model Model
        { get { return model; } }

        /// <summary>
        /// The duration of the animation in milliseconds
        /// </summary>
        public double TotalAnimationMilliseconds
        { get { return totalMilliseconds; } }

        /// <summary>
        /// True if model bones shouldn't be changed
        /// </summary>
        public bool PreserveBones
        {
            get { return preserveBones; }
            set { preserveBones = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a bone pose
        /// </summary>
        /// <param name="channel">The animation channel</param>
        /// <param name="boneIndex">The index of the bone attached to the channel</param>
        private void CreatePose(AnimationChannel channel, int boneIndex)
        {
            // Index in the channel of the current key frame
            int curFrameIndex = keyFrameIndices[boneIndex];
            // References to current and next frame
            AnimationKeyframe curFrame = channel[curFrameIndex], nextFrame =
                channel[curFrameIndex + 1];

            double interpAmount = (nextFrame.Time - curFrame.Time).TotalMilliseconds;
            interpAmount = (curTime - curFrame.Time.TotalMilliseconds) / interpAmount;

            if (interpMethod == InterpolationMethod.SphericalLinear)
            {
                Matrix curMatrix = curFrame.Transform;
                Matrix nextMatrix = nextFrame.Transform;
                // An expensive operation that decomposes both matrices and interpolates with
                // quaternions and vectors
                model.Bones[boneIndex].Transform = Util.SlerpMatrix(
                    curMatrix,
                    nextMatrix,
                    interpAmount);

            }
            else if (interpMethod == InterpolationMethod.Linear)
            {
                // simple linear interpolation
                model.Bones[boneIndex].Transform = Matrix.Lerp(curFrame.Transform,
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

            if (preserveBones)
                model.CopyBoneTransformsFrom(bones);
            // Create each pose
            foreach (KeyValuePair<string, AnimationChannel> k in animation.Channels)
                CreatePose(k.Value, model.Bones[k.Key].Index);

            model.CopyAbsoluteBoneTransformsTo(poseSet);
            // apply any skin transforms
            foreach (KeyValuePair<string, Matrix> skinTransform in blendTransforms)
            {
                int index = model.Bones[skinTransform.Key].Index;
                poseSet[index] = skinTransform.Value * poseSet[index];
            }
            // Put the bones back in their original position if we are preserving them
            if (preserveBones)
            {
                model.CopyBoneTransformsTo(bones);
                model.CopyBoneTransformsFrom(originalBones);
            }
        }

        /// <summary>
        /// Resets the bone pose to the initial state and the timer to 0
        /// </summary>
        public void Reset()
        {
            curTime = 0;
            model.CopyBoneTransformsFrom(originalBones);
            if (bones != null)
                model.CopyBoneTransformsTo(bones);
        }

        /// <summary>
        /// Advances the animation frame but does not do any interpolation
        /// </summary>
        /// <param name="time">The amount of time to advance the animation</param>
        public void AdvanceTime(double time)
        {
            curTime += time;
            time = curTime;
            // Update the key frame indices for each channel
            foreach (KeyValuePair<string, AnimationChannel> k in animation.Channels)
            {
                AnimationChannel channel = k.Value;
                if (time > channel[channel.Count - 1].Time.TotalMilliseconds)
                    time = (time %
                        channel[channel.Count - 1].Time.TotalMilliseconds);
                int boneIndex = model.Bones[k.Key].Index;
                int curFrameIndex = keyFrameIndices[boneIndex];
                while (time > channel[keyFrameIndices[boneIndex] + 1].Time.TotalMilliseconds)
                    keyFrameIndices[boneIndex]++;
            }
        }

        #endregion
    }
}
