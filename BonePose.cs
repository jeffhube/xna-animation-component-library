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



    public class BonePoseCollection 
        : System.Collections.ObjectModel.ReadOnlyCollection<BonePose>
    {
        private Dictionary<string, BonePose> boneDict 
            = new Dictionary<string, BonePose>();

        internal BonePoseCollection(IList<BonePose> anims)
            :
            base(anims)
        {
            for (int i = 0; i < anims.Count; i++)
            {
                string boneName = anims[i].Name;
                if (boneName != null && boneName != "" && !boneDict.ContainsKey(boneName))
                {
                    boneDict.Add(boneName, anims[i]);
                }
            }
        }

        internal static BonePoseCollection FromModelBoneCollection(
            ModelBoneCollection bones)
        {
            BonePose[] anims = new BonePose[bones.Count];
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].Parent==null)
                {
                    BonePose ba = new BonePose(
                        bones[i],
                        bones,
                        anims);

                }
            }

            return new BonePoseCollection(anims);
        }



        public BonePose this[string boneName]
        {
            get { return boneDict[boneName]; }
        }

    }

    public class BonePose
    {
        private Matrix defaultMatrix;
        private int index;
        private string name;
        private BonePose parent = null;
        private AnimationController currentAnimation = null;
        private AnimationController currentBlendAnimation = null;
        private float blendFactor = 0;
        private BonePoseCollection children;
        private int frameNum;
        private bool doesAnimContainChannel = false;
        private bool doesBlendContainChannel = false;


        internal BonePose(ModelBone bone, 
            ModelBoneCollection bones,
            BonePose[] anims)
        {
            index = bone.Index;
            name = bone.Name;
            defaultMatrix = bone.Transform;
            if (bone.Parent != null)
                parent = anims[bone.Parent.Index];
            anims[index] = this;
            List<BonePose> childList = new List<BonePose>();
            foreach (ModelBone child in bone.Children)
            {
                BonePose newChild = new BonePose(
                    bones[child.Index],
                    bones,
                    anims);
                childList.Add(newChild);
            }
            children = new BonePoseCollection(childList);
        }

        public BonePoseCollection Children
        {
            get { return children; }
        }

        private void FindHierarchy(List<BonePose> poses)
        {
            poses.Add(this);
            foreach (BonePose child in children)
            {
                child.FindHierarchy(poses);
            }
        }

        public BonePoseCollection Hierarchy
        {
            get
            {
                List<BonePose> poses = new List<BonePose>();
                FindHierarchy(poses);
                return new BonePoseCollection(poses);
            }
        }


        public BonePose Parent
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

        public AnimationController CurrentAnimation
        {
            get { return currentAnimation; }
            set
            {
                if (currentAnimation != value)
                {
                    if (currentAnimation != null)
                    {
                        currentAnimation.AffectedBlendBones.InternalRemove(this);
                    }
                    if (value != null)
                    {
                        value.AffectedBlendBones.InternalAdd(this);
                        if (name != null)
                        {
                            doesAnimContainChannel =
                                value.AnimationSource.AffectedBones.Contains(name);
                        }
                    }
                    else
                        doesAnimContainChannel = false;
                    currentAnimation = value;
                }
            }

        }

        public AnimationController CurrentBlendAnimation
        {
            get { return currentBlendAnimation; }
            set
            {
                if (currentBlendAnimation != value)
                {
                    if (currentBlendAnimation != null)
                    {
                        currentBlendAnimation.AffectedBones.InternalRemove(this);
                    }
                    if (value != null)
                    {
                        value.AffectedBones.InternalAdd(this);
                        if (name != null)
                        {
                            doesBlendContainChannel =
                                value.AnimationSource.AffectedBones.Contains(name);
                        }
                    }
                    else
                        doesBlendContainChannel = false;
                    currentBlendAnimation = value;
                }
            }
        }



        public float BlendFactor
        {
            get { return blendFactor; }
            set { blendFactor = value; }
        }

        public Matrix DefaultTransform
        {
            get { return defaultMatrix; }
            set { defaultMatrix = value; }
        }

        private void Blend(ref Matrix source)
        {


            BoneKeyframeCollection channel = this.currentBlendAnimation.AnimationSource.AnimationChannels[
                name];
            frameNum = channel.GetIndexByTime(currentBlendAnimation.ElapsedTime);
            source = Util.SlerpMatrix(source, channel[frameNum].Transform, blendFactor);
        }



        public Matrix CurrentTransform
        {
            get
            {
                if (currentAnimation == null || !doesAnimContainChannel)
                {
                    if (currentBlendAnimation == null || !doesBlendContainChannel)
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
                    BoneKeyframeCollection channel = currentAnimation.AnimationSource.AnimationChannels[
                        name];

                    frameNum = channel.GetIndexByTime(currentAnimation.ElapsedTime);
                    if (currentBlendAnimation == null || !doesBlendContainChannel)
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
