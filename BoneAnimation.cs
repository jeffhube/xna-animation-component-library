/*
 * BoneAnimation.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace Animation
{



    public class BoneAnimationCollection 
        : System.Collections.ObjectModel.ReadOnlyCollection<BoneAnimation>
    {
        private Dictionary<string, BoneAnimation> boneDict 
            = new Dictionary<string, BoneAnimation>();

        internal BoneAnimationCollection(IList<BoneAnimation> anims)
            :
            base(anims)
        {
            for (int i = 0; i < anims.Count; i++)
            {
                if (anims[i].Name != null)
                {
                    boneDict.Add(anims[i].Name, anims[i]);
                }
            }
        }

        internal static BoneAnimationCollection FromModelBoneCollection(
            ModelBoneCollection bones)
        {
            BoneAnimation[] anims = new BoneAnimation[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Parent==null)
                {
                    BoneAnimation ba = new BoneAnimation(
                        bones[i],
                        bones,
                        anims);

                }
            }

            return new BoneAnimationCollection(anims);
        }



        public BoneAnimation this[string boneName]
        {
            get { return boneDict[boneName]; }
        }

    }

    public class BoneAnimation
    {
        private Matrix defaultMatrix;
        private int index;
        private string name;
        private BoneAnimation parent = null;
        private RunningAnimation currentAnimation = null;
        private RunningAnimation currentBlendAnimation = null;
        private float blendFactor = 0;
        private BoneAnimationCollection children;
        private int frameNum;


        internal BoneAnimation(ModelBone bone, 
            ModelBoneCollection bones,
            BoneAnimation[] anims)
        {
            index = bone.Index;
            name = bone.Name;
            defaultMatrix = bone.Transform;
            if (bone.Parent != null)
                parent = anims[bone.Parent.Index];
            anims[index] = this;
            List<BoneAnimation> childList = new List<BoneAnimation>();
            foreach (ModelBone child in bone.Children)
            {
                BoneAnimation newChild = new BoneAnimation(
                    bones[child.Index],
                    bones,
                    anims);
                childList.Add(newChild);
            }
            children = new BoneAnimationCollection(childList);
        }

        public BoneAnimationCollection Children
        {
            get { return children; }
        }
        public BoneAnimation Parent
        {
            get { return parent; }
        }
        public int Index
        {
            get { return index; }
        }

        public string Name
        {
            get { return name; }
        }

        public RunningAnimation CurrentAnimation
        {
            get { return currentAnimation; }
        }

        public RunningAnimation CurrentBlendAnimation
        {
            get { return currentBlendAnimation; }
        }

        public float BlendFactor
        {
            get { return blendFactor; }
            set { blendFactor = value; }
        }

        internal void SetRunningAnimation(RunningAnimation anim)
        {
            this.currentAnimation = anim;
        }

        internal void SetBlendAnimation(RunningAnimation anim)
        {
            this.currentBlendAnimation = anim;
        }

        public Matrix DefaultTransform
        {
            get { return defaultMatrix; }
            set { defaultMatrix = value; }
        }

        private void Blend(ref Matrix source)
        {
            frameNum = currentAnimation.DefaultFrameNum;

            BoneKeyframeCollection channel = this.currentBlendAnimation.AnimationSource.AnimationChannels[
                name];
            if (channel.Count <= currentBlendAnimation.AnimationSource.MaxNumFrames)
            {
                frameNum = channel.Count * (int)(currentBlendAnimation.ElapsedTime
                    / channel.Duration);
                if (frameNum >= channel.Count)
                    frameNum = channel.Count - 1;
                while (frameNum < channel.Count - 1
                    && channel[frameNum].Time < currentBlendAnimation.ElapsedTime)
                {
                    ++frameNum;
                }
                while (frameNum > 0 && channel[frameNum - 1].Time >
                    currentBlendAnimation.ElapsedTime)
                {
                    --frameNum;
                }
            }
            source = Util.SlerpMatrix(source, channel[frameNum].Transform, blendFactor);
        }



        public Matrix CurrentTransform
        {
            get
            {
                if (currentAnimation == null)
                {
                    if (currentBlendAnimation == null)
                    {
                        return defaultMatrix;
                    }
                    else
                    {
                        Matrix m = defaultMatrix;
                        Blend(ref m);
                        return m;

                    }

                }
                else
                {

                    frameNum = currentAnimation.DefaultFrameNum;
                    BoneKeyframeCollection channel = currentAnimation.AnimationSource.AnimationChannels[
                        name];
                    if (channel.Count <= currentAnimation.AnimationSource.MaxNumFrames)
                    {
                        frameNum = channel.Count * (int)(currentAnimation.ElapsedTime
                            / channel.Duration);
                        if (frameNum >= channel.Count)
                            frameNum = channel.Count - 1;
                        while (frameNum < channel.Count - 1
                            && channel[frameNum].Time < currentAnimation.ElapsedTime)
                        {
                            ++frameNum;
                        }
                        while (frameNum > 0 && channel[frameNum - 1].Time > 
                            currentAnimation.ElapsedTime)
                        {
                            --frameNum;
                        }
                    }

                    if (currentBlendAnimation == null)
                    {
                        return channel[frameNum].Transform;
                    }
                    else
                    {
                        Matrix m = channel[frameNum].Transform;
                        Blend(ref m);
                        return m;
                    }

                }
               

            }
        }
    }
}
