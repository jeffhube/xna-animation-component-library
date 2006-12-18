/*
 * InterpolatedAnimation.cs
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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Animation;
#endregion

namespace Animation
{

    public class InterpolatedAnimation
    {
        public readonly Matrix[][][] Frames;
        public readonly long TimeStep;
        public readonly long Duration;
        public readonly int NumFrames;

        internal InterpolatedAnimation(
            Matrix[][][] frames,
            long timeStep,
            long duration,
            int numFrames)
        {
            this.Frames = frames;
            this.TimeStep = timeStep;
            this.Duration = duration;
            this.NumFrames = numFrames;
        }

        public InterpolatedAnimation(
            long timeStep,
            AnimationInterpolator creator)
        {
            MeshInfo meshInfo = creator.MeshInfo;
            int numMeshes = meshInfo.NumMeshes;
            List<SkinTransform[]> skinTransforms = meshInfo.SkinTransforms;
            this.Frames = new Matrix[numMeshes][][];
            this.TimeStep = timeStep;
            this.Duration = creator.Animation.Duration;
            this.NumFrames = timeStep == 0 ? 1 : (int)(Duration / timeStep);
            if (NumFrames == 0)
                throw new Exception("Error: Animation named \"" + creator.Animation.Name + "\" is too fast for 60 fps.  If " +
                    "animation was loaded from a directx file, please add an AnimationTicksPerSecond node to the .X file " +
                    "such as : AnimTicksPerSecond { 25; }\n"); 
            for (int i = 0; i < numMeshes; i++)
            {
                Frames[i] = new Matrix[NumFrames][];
            }
            Matrix[] modelBones = new Matrix[creator.ModelBoneManager.Count];
            for (int curFrame = 0; curFrame < NumFrames; curFrame++)
            {
                creator.CreateModelPoseSet(modelBones);
                for (int i = 0; i < numMeshes; i++)
                {
                    Frames[i][curFrame] =
                        new Matrix[skinTransforms[i] == null ? 1 : skinTransforms[i].Length];
                    creator.CreateMeshPoseSet(
                        Frames[i][curFrame],
                        modelBones,
                        skinTransforms[i],
                        creator.MeshInfo.MeshBoneIndices[i]);
                }
                creator.AdvanceTime(timeStep);
            }
        }


    }




    
}


