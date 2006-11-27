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

        internal sealed class InterpolationTableCollection
        {
            private InterpolationTable[] tables;
            private BonePoseCreator creator;
            private int numFrames;
            private long timeStep;
            private long totalTime;
            private bool tablesCreated = false;
            private List<SkinTransform[]> skinTransforms;

            public InterpolationTableCollection(BonePoseCreator creator,
                List<SkinTransform[]> skinTransforms)
            {
                this.creator = creator;
                this.skinTransforms=skinTransforms;
                tables = new InterpolationTable[creator.Model.Meshes.Count];

            }

            public bool TablesCreated
            { get { return tablesCreated; } }

            public Matrix[] GetMeshPose(ref int meshIndex, ref int frameNum)
            {
                return tables[meshIndex].GetPose(ref frameNum);
            }

            public long TimeStep
            { get { return timeStep; } }

            public int NumFrames
            { get { return numFrames; } }

            public void CreateTables(long timeStep)
            {
                Matrix[] modelBones = new Matrix[creator.Model.Bones.Count];
                this.timeStep = timeStep;
                totalTime = creator.Animation.Duration.Ticks;
                numFrames = timeStep == 0 ? 1 : (int)(totalTime / timeStep);
                if (!tablesCreated)
                {
                    for (int i = 0; i < tables.Length; i++)
                    {
                        tables[i] = new InterpolationTable(this,
                            skinTransforms[i],
                            creator.Model.Meshes[i].ParentBone.Index);
                    }
                }

                for (int curFrame = 0; curFrame < numFrames; curFrame++)
                {
                    creator.CreateModelPoseSet(modelBones);
                    for (int i = 0; i < tables.Length; i++)
                    {
                        tables[i].CreateFrame(curFrame,modelBones);
                    }
                    creator.AdvanceTime(timeStep);
                }
                tablesCreated = true;
            }

            /// <summary>
            /// A table that contains the bone poses and normal bone poses based on an animation
            /// and the time step between the bone poses.
            /// </summary>
            internal sealed class InterpolationTable
            {

                #region Member Variables
                private InterpolationTableCollection tables;
                // Stores all of the bone pose sets by frame index
                private Matrix[][] bonePoses;
                private SkinTransform[] skinTransforms;
                private int meshBoneIndex;
                #endregion

                #region Constructors
                // Creates a table that contains the bone poses and inverse transpose bone poses
                internal InterpolationTable(
                    InterpolationTableCollection tables,
                    SkinTransform[] skinTransforms,
                    int meshBoneIndex)
                {
                    this.meshBoneIndex = meshBoneIndex;
                    this.tables = tables;
                    this.skinTransforms = skinTransforms;
                    bonePoses = new Matrix[tables.numFrames][];
                }
                #endregion


                #region Methods and Properties

                public Matrix[] GetPose(ref int frameNum)
                {
                    return bonePoses[frameNum];
                }
                
                /// <summary>
                /// Creates the table by using the BonePoseCreator for each frame, and simulates
                /// stepping through each frame
                /// </summary>
                internal void CreateFrame(int frameNum, Matrix[] modelPoses)
                {
                    bonePoses[frameNum] = new Matrix[skinTransforms == null ? 1 : skinTransforms.Length];
                    tables.creator.CreateMeshPoseSet(
                        bonePoses[frameNum],
                        modelPoses,
                        skinTransforms,
                        meshBoneIndex);
                }
            }


                #endregion
        }


    
}


