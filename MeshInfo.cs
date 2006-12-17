/*
 * MeshInfo.cs
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Animation
{
    public class MeshInfo
    {
        public readonly int NumMeshes;
        public readonly int[] MeshBoneIndices;
        // Maps bone names to their blend transform, which, when applied to a bone,
        // creates a matrix that transforms vertices in the bones local space
        // empty if there is no skinning information
        public List<SkinTransform[]> SkinTransforms;

        public MeshInfo(Model model, List<SkinTransform[]> skinTransforms)
        {
            NumMeshes = model.Meshes.Count;
            MeshBoneIndices = new int[NumMeshes];
            for (int i = 0; i < NumMeshes; i++)
            {
                MeshBoneIndices[i] = model.Meshes[i].ParentBone.Index;
            }
            this.SkinTransforms = skinTransforms;
        }
        public MeshInfo(int[] meshRootBoneIndices, List<SkinTransform[]> skinTransforms)
        {
            this.MeshBoneIndices = (int[])meshRootBoneIndices.Clone();
            this.SkinTransforms = skinTransforms;
            NumMeshes = meshRootBoneIndices.Length;
        }
    }
}
