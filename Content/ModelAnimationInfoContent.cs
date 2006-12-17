/*
 * ModelAnimationInfoContent.cs
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
using System.Runtime.Serialization;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
#endregion

namespace Animation.Content
{

    /// <summary>
    /// Contains animation info for a model.
    /// </summary>
    public class ModelAnimationInfoContent
    {
        private ModelContent model;


        public AnimationContentDictionary animations;
        public AnimationContentDictionary Animations
        {
            get { return animations; }
        }

        private MeshInfo meshInfo;
        public MeshInfo MeshInfo
        {
            get { return meshInfo; }
        }

        private List<InterpolatedAnimation> interpedAnims = new List<InterpolatedAnimation>();
        public List<InterpolatedAnimation> InterpolatedAnimations
        { get { return interpedAnims; } }

        public ModelAnimationInfoContent(
            ModelContent model,
            AnimationContentDictionary animations,
            List<SkinTransform[]> skinTransforms)
        {
            this.model = model;
            this.animations = animations;

            int[] meshBoneIndices = new int[model.Meshes.Count];
            for (int i = 0; i < meshBoneIndices.Length; i++)
            {
                meshBoneIndices[i] = model.Meshes[i].ParentBone.Index;
            }
            MeshInfo meshInfo = new MeshInfo(meshBoneIndices,skinTransforms);
            ModelAnimationBuilder builder = new ModelAnimationBuilder();
            this.meshInfo = new MeshInfo(meshBoneIndices, skinTransforms);
            AnimationInterpolator animInterp = new AnimationInterpolator(
                new ModelBoneManager(CreateAnimationBones(model.Bones)),
                this.meshInfo);

            foreach (KeyValuePair<string, AnimationContent> k in animations)
            {
                builder.StartAnimation(k.Key);
                foreach (KeyValuePair<string, AnimationChannel> chan in k.Value.Channels)
                {
                    foreach (AnimationKeyframe keyFr in chan.Value)
                    {
                        builder.AddKeyframe(chan.Key, keyFr.Transform, keyFr.Time.Ticks);
                    }
                }
                ModelAnimation modelAnim = builder.FinishAnimation();

                animInterp.Reset();
                animInterp.Animation = modelAnim;
                InterpolatedAnimation interpolatedAnim = new InterpolatedAnimation(
                    TimeSpan.FromSeconds(1).Ticks / 60,
                    animInterp);
                interpedAnims.Add(interpolatedAnim);
            }

            
        }

        public List<IBone> CreateAnimationBones(ModelBoneContentCollection boneContent)
        {
            List<IBone> bones = new List<IBone>();
            foreach (ModelBoneContent b in boneContent)
                bones.Add(new AnimationBone(b));
            return bones;
        }
        public class AnimationBone : IBone
        {
            private ModelBoneContent bone;

            public AnimationBone(ModelBoneContent bone)
            {
                this.bone = bone;
            }

            #region IBone Members

            public IList<IBone> Children
            {
                get
                {
                    List<IBone> children = new List<IBone>();
                    foreach (ModelBoneContent content in bone.Children)
                        children.Add(new AnimationBone(content));
                    return children;
                }
            }

            public IBone Parent
            {
                get { return bone.Parent == null ? null : new AnimationBone(bone.Parent); }
            }

            public Matrix Transform
            {
                get { return bone.Transform; }
            }

            public int Index
            {
                get { return bone.Index; }
            }

            public string Name
            {
                get { return bone.Name; }
            }

            #endregion
        }
    }


}
