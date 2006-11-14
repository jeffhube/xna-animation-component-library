/*
 *  BonePoseTable.cs
 *  Creates and manages a bone pose table used for animation.
 *  Copyright (C) 2006  David Astle
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Animation.Content;
#endregion

namespace Animation
{

    /// <summary>
    /// A table that contains the bone poses and normal bone poses based on an animation
    /// and the time step between the bone poses.
    /// </summary>
    public class BonePoseTable
    {
        #region Creation Data Structure

        #endregion

        #region Member Variables
        // The time between pose sets
        private double timeStep;
        // The total time of the animation
        private double totalTime;
        // Stores all of the bone pose sets by frame index
        private Matrix[][] bonePoses;
        // Stores all of the bone pose sets for vertex normals by frame index
        private Matrix[][] normalBonePoses;
        // The number of frames in the animation
        private int numFrames;
        private InterpolationMethod interpMethod;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a table that contains the bone poses and inverse transpose bone poses
        /// </summary>
        /// <param name="model">The model for which the animation is intended</param>
        /// <param name="animation">The animation data</param>
        /// <param name="timeStep">The amount of time in between each frame</param>
        public BonePoseTable(Model model, AnimationContent animation,
            double timeStep, InterpolationMethod interpMethod)
        {
            // Store the passed in data in the appropriate places
            this.timeStep = timeStep;
            CreationData data = new CreationData();
            data.Model = model;
            data.KeyFrameIndices = new int[model.Bones.Count];
            data.Animation = animation;
            data.BlendTransforms = ((ModelInfo)model.Tag).BlendTransforms;
            totalTime = animation.Duration.TotalMilliseconds;
            numFrames = (int)(animation.Duration.TotalMilliseconds / timeStep);
            bonePoses = new Matrix[numFrames][];
            normalBonePoses = new Matrix[numFrames][];
            this.interpMethod = interpMethod;
            // After storing data and doing general initialization, create the table
            CreateTable(ref data);
        }
        #endregion

        #region Properties And Get Methods
        /// <summary>
        /// The number of frames in the animation represented by the bone pose table.
        /// </summary>
        public int NumFrames
        { get { return numFrames; } }

        public double TotalMilliseconds
        { get { return totalTime; } }

        /// <summary>
        /// Returns a bone pose set of the animation at the given time
        /// </summary>
        /// <param name="elapsedTime">The elapsed animation time</param>
        /// <returns>A bone pose set of the animation at the given time</returns>
        public Matrix[] GetBonePoses(double elapsedTime)
        {
            // The total elapsed time in milliseconds, looping if greater than the animations
            // total time
            double elapsedMilliseconds = elapsedTime % (totalTime
                - timeStep);
            // The current frame index for the given time
            int frameNum = (int)(elapsedMilliseconds / timeStep);
            return bonePoses[frameNum];
        }

        /// <summary>
        /// Returns an inverse transpose bone pose set of the animation at the given time
        /// </summary>
        /// <param name="elapsedTime">The elapsed animation time</param>
        /// <returns>An inverse transpose bone pose set of the animation at the given time</returns>
        public Matrix[] GetNormalBonePoses(double elapsedTime)
        {
            // The total elapsed time in milliseconds, looping if greater than the animations
            // total time
            double elapsedMilliseconds = elapsedTime % (totalTime
                - timeStep);
            // The current frame index for the given time
            int frameNum = (int)(elapsedMilliseconds / timeStep);
            return normalBonePoses[frameNum];
        }

        #endregion

        #region Table Creation Methods
        private void CreateTable(ref CreationData data)
        {
            Matrix[] originalBones = new Matrix[data.Model.Bones.Count];
            data.Model.CopyBoneTransformsTo(originalBones);
            double curTime = 0;
            for (int i = 0; i < numFrames; i++)
            {
                bonePoses[i] = CreatePoseSet(ref data, curTime);
                normalBonePoses[i] = new Matrix[bonePoses[i].Length];
                for (int j = 0; j < bonePoses[i].Length; j++)
                    normalBonePoses[i][j] = Matrix.Invert(Matrix.Transpose(bonePoses[i][j]));
                curTime += timeStep;
            }
            data.Model.CopyBoneTransformsFrom(originalBones);
        }


        private void CreatePose(ref CreationData data, double curTime,
            AnimationChannel channel, int boneIndex)
        {
            if (curTime > channel[channel.Count - 1].Time.TotalMilliseconds)
                curTime = (curTime %
                    channel[channel.Count - 1].Time.TotalMilliseconds);

            int curFrameIndex = data.KeyFrameIndices[boneIndex];
            while (curTime > channel[curFrameIndex + 1].Time.TotalMilliseconds)
                curFrameIndex++;
            data.KeyFrameIndices[boneIndex] = curFrameIndex;

            AnimationKeyframe curFrame = channel[curFrameIndex], nextFrame =
                channel[curFrameIndex + 1];

            double interpAmount = (nextFrame.Time - curFrame.Time).TotalMilliseconds;
            interpAmount = (curTime - curFrame.Time.TotalMilliseconds) / interpAmount;

            if (interpMethod == InterpolationMethod.SphericalLinear)
            {
                Matrix curMatrix = curFrame.Transform;
                Matrix nextMatrix = nextFrame.Transform;
                data.Model.Bones[boneIndex].Transform = Util.SlerpMatrix(
                    curMatrix,
                    nextMatrix,
                    interpAmount);
                
            }
            else if (interpMethod == InterpolationMethod.Linear)
            {
                data.Model.Bones[boneIndex].Transform = Matrix.Lerp(curFrame.Transform,
                     nextFrame.Transform,
                    (float)interpAmount);
            }

        }


        private Matrix[] CreatePoseSet(ref CreationData data, double curTime)
        {
            Matrix[] poseSet = new Matrix[data.Model.Bones.Count];
            foreach (KeyValuePair<string, AnimationChannel> k in data.Animation.Channels)
                CreatePose(ref data, curTime, k.Value, data.Model.Bones[k.Key].Index);

            data.Model.CopyAbsoluteBoneTransformsTo(poseSet);
            foreach (KeyValuePair<string, Matrix> skinTransform in data.BlendTransforms)
            {
                int index = data.Model.Bones[skinTransform.Key].Index;
                poseSet[index] = skinTransform.Value * poseSet[index];
            }
            return poseSet;
        }
        #endregion

    }
}

/// <summary>
/// Stores data that is used during creation of the bone pose table to improve
/// readability.
/// </summary>
public struct CreationData
{
    // The model for which the animation is intended
    private Model model;




    public Model Model
    { get { return model; } }
    public AnimationContent Animation
    { get { return animation; } }

}

public class BonePoseCreator
{
    private double curTime;
    // Stores the current key frame while creating the table.
    // KeyFrameIndices[i] = the current frame for the bone with index i
    private int[] keyFrameIndices;
    private Model model;
    // Contains the data for the animation
    private AnimationContent animation;
    // Maps bone names to their blend transform, which, when applied to a bone,
    // creates a matrix that transforms vertices in the bones local space
    private Dictionary<string, Matrix> blendTransforms;
    public BonePoseCreator(Model model, AnimationContent animation,
        Dictionary<string, Matrix> blendTransforms)
    {
        this.model = model;
        this.animation = animation;
        this.blendTransforms = blendTransforms;
        keyFrameIndices = new int[model.Bones.Count];
    }

    private void CreatePose(AnimationChannel channel)
    {
    }

    public void AdvanceTime(double time)
    {
        curTime += time;
        time = curTime;
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


}