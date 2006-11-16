/*
 * BonePoseTable.cs
 * Creates and manages a bone pose table used for animation.
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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Animation.Content;
using Animation;
#endregion

namespace Animation
{

    /// <summary>
    /// A table that contains the bone poses and normal bone poses based on an animation
    /// and the time step between the bone poses.
    /// </summary>
    public class BonePoseTable
    {
        #region Member Variables
        // The time between pose sets
        private long timeStep;
        // The total time of the animation
        private long totalTime;
        private BonePoseCreator creator;
        // Stores all of the bone pose sets by frame index
        private Matrix[][] bonePoses;
        // See commented out method for ... comments
        // Stores all of the bone pose sets for vertex normals by frame index
        // private Matrix[][] normalBonePoses;
        // The number of frames in the animation
        private int numFrames;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a table that contains the bone poses and inverse transpose bone poses
        /// </summary>
        /// <param name="model">The model for which the animation is intended</param>
        /// <param name="animation">The animation data</param>
        /// <param name="timeStep">The amount of time in between each frame</param>
        public BonePoseTable(BonePoseCreator creator,
            long timeStep)
        {
            // Store the passed in data in the appropriate places
            this.timeStep = timeStep;
            this.creator = creator;
            totalTime = creator.TotalAnimationTicks;
            numFrames = (int)(totalTime / timeStep);
            bonePoses = new Matrix[numFrames][];
            //normalBonePoses = new Matrix[numFrames][];
            // After storing data and doing general initialization, create the table
            CreateTable();
        }
        #endregion

        #region Methods and Properties
        /// <summary>
        /// The number of frames in the animation represented by the bone pose table.
        /// </summary>
        public int NumFrames
        { get { return numFrames; } }

        public long TotalTicks
        { get { return totalTime; } }

        /// <summary>
        /// Returns a bone pose set of the animation at the given time
        /// </summary>
        /// <param name="elapsedTime">The elapsed animation time</param>
        /// <returns>A bone pose set of the animation at the given time</returns>
        public Matrix[] GetBonePoses(long elapsedTime)
        {
            // The total elapsed time in ticks, looping if greater than the animations
            // total time
            // The current frame index for the given time
            int frameNum = (int)((elapsedTime % (totalTime-timeStep))/ timeStep);
            return bonePoses[frameNum];
        }
        /* Will we ever need this?  I don't want to delete it in case a shader doesn't do
         * the inverse transpose calcutions
        /// <summary>
        /// Returns an inverse transpose bone pose set of the animation at the given time
        /// </summary>
        /// <param name="elapsedTime">The elapsed animation time</param>
        /// <returns>An inverse transpose bone pose set of the animation at the given time</returns>
        public Matrix[] GetNormalBonePoses(double elapsedTime)
        {
        }
         */


        /// <summary>
        /// Creates the table by using the BonePoseCreator for each frame, and simulates
        /// stepping through each frame
        /// </summary>
        private void CreateTable()
        {
            int numBones = creator.Model.Bones.Count;
            Matrix[] originalBones = new Matrix[numBones];
            creator.Model.CopyBoneTransformsTo(originalBones);

            // This loop does the actual creation. 
            for (int i = 0; i < numFrames; i++)
            {
                creator.AdvanceTime(timeStep);
                bonePoses[i] = new Matrix[numBones];
                creator.CreatePoseSet(bonePoses[i]);
          //      normalBonePoses[i] = new Matrix[numBones];
          //       for (int j = 0; j < bonePoses[i].Length; j++)
          //           normalBonePoses[i][j] = Matrix.Invert(Matrix.Transpose(bonePoses[i][j]));
            }
            creator.Model.CopyBoneTransformsFrom(originalBones);
        }



        #endregion

    }
}


