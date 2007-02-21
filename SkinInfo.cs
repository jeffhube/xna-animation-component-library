/*
 * SkinTransform.cs
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
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace XCLNA.XNA.Animation
{

    /// <summary>
    /// A structure that contains information for a bindpose skin offset.
    /// Represents the inverse bind pose for a bone.
    /// </summary>
    public struct SkinInfo
    {
        public SkinInfo(string name, Matrix transform,
            int paletteIndex, int boneIndex)
        {
            BoneName = name;
            Transform = transform;
            PaletteIndex = paletteIndex;
            BoneIndex = boneIndex;
        }
        /// <summary>
        /// The name of the bone attached to the transform
        /// </summary>
        public readonly string BoneName;
        /// <summary>
        /// The transform for the bone
        /// </summary>
        public readonly Matrix Transform;
        public readonly int PaletteIndex;
        public readonly int BoneIndex;
    }

    public class SkinInfoCollection : ReadOnlyCollection<SkinInfo>
    {
        private Model model;

        public SkinInfoCollection(Model model)
            : base(FromModel(model))
        {
            this.model = model;

        }

        private static SkinInfo[] FromModel(Model model)
        {
            Dictionary<string, object> modelTagData =
                    (Dictionary<string, object>)model.Tag;
            string[] skinnedBones;
            // An AnimationLibrary processor was not used if this is null
            if (modelTagData == null || !modelTagData.ContainsKey("SkinnedBones"))
            {
                skinnedBones = new string[] { };
            }
            else
            {
                skinnedBones = (string[])modelTagData["SkinnedBones"];
            }

            Matrix[] pose = new Matrix[model.Bones.Count];
            Matrix[] skinTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(pose);
            SkinInfo[] skinInfoArray = new SkinInfo[skinnedBones.Length];

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                if (Util.IsSkinned(model.Meshes[i]))
                {
                    Matrix absoluteMeshTransform = pose[model.Meshes[i].ParentBone.Index];
                    for (int j = 0; j < skinnedBones.Length; j++)
                    {
                        ModelBone bone = model.Bones[skinnedBones[j]];
                        skinInfoArray[j] =
                            new SkinInfo(
                            bone.Name,
                            absoluteMeshTransform * Matrix.Invert(pose[bone.Index]),
                            j,
                            bone.Index);
                    }
                    break;
                }
            }


            return skinInfoArray;
        }






        public Model Model
        {
            get { return model; }
        }

    }





}
