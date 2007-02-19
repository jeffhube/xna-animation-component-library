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
        // Used when no animation is set
        private Matrix defaultMatrix;
        // Buffers for interpolation when blending
        private static Matrix returnMatrix, blendMatrix, currentMatrixBuffer;
        private int index;
        private string name;
        private BonePose parent = null;
        private IAnimationController currentAnimation = null;
        private IAnimationController currentBlendAnimation = null;
        private float blendFactor = 0;
        private BonePoseCollection children;
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

        /// <summary>
        /// Gets the immediate children of the current bone.
        /// </summary>
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

        /// <summary>
        /// Gets a collection of bones that represents the tree of BonePoses with
        /// the current BonePose as the root.
        /// </summary>
        public BonePoseCollection Hierarchy
        {
            get
            {
                List<BonePose> poses = new List<BonePose>();
                FindHierarchy(poses);
                return new BonePoseCollection(poses);
            }
        }


        /// <summary>
        /// Gets the bone's parent.
        /// </summary>
        public BonePose Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// Gets the index of the bone.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        /// Gets the name of the bone.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets or sets the current animation that affects this bone.  If null,
        /// then DefaultTransform will be used for this bone's transform.
        /// </summary>
        public IAnimationController CurrentAnimation
        {
            get { return currentAnimation; }
            set
            {
                if (currentAnimation != value)
                {
                    if (value != null)
                    {
                        if (name != null)
                        {
                            doesAnimContainChannel = 
                                value.ContainsAnimationTrack(this);
                        }
                    }
                    else
                        doesAnimContainChannel = false;
                    currentAnimation = value;
                }
            }

        }

        /// <summary>
        /// Gets or sets the blend animation that affects this bone.  If the value
        /// is null, then no blending will occur.
        /// </summary>
        public IAnimationController CurrentBlendAnimation
        {
            get { return currentBlendAnimation; }
            set
            {
                if (currentBlendAnimation != value)
                {

                    if (value != null)
                    {
                        if (name != null)
                        {
                            doesBlendContainChannel =
                                value.ContainsAnimationTrack(this);
                        }
                    }
                    else
                        doesBlendContainChannel = false;
                    currentBlendAnimation = value;
                }
            }
        }


        /// <summary>
        /// Gets or sets the amount to interpolate between the current animation and
        /// the current blend animation, if the current blend animation is not null
        /// </summary>
        public float BlendFactor
        {
            get { return blendFactor; }
            set { blendFactor = value; }
        }
        
        /// <summary>
        /// Represents the matrix used by the BonePose when it is not affected by
        /// an animation or when the animation does not contain a track for the bone.
        /// </summary>
        public Matrix DefaultTransform
        {
            get { return defaultMatrix; }
            set { defaultMatrix = value; }
        }

        /// <summary>
        /// Returns the current transform, based on the animations, for the bone
        /// represented by the BonePose object.
        /// </summary>
        public Matrix CurrentTransform
        {
            get
            {
                // If the bone is not currently affected by an animation
                if (currentAnimation == null || !doesAnimContainChannel)
                {
                    // If the bone is affected by a blend animation,
                    // blend the defaultTransform with the blend animation
                    if (currentBlendAnimation != null && doesBlendContainChannel)
                    {
                        currentBlendAnimation.GetCurrentBoneTransform(this, out blendMatrix);
                        Util.SlerpMatrix(
                            ref defaultMatrix, 
                            ref blendMatrix, 
                            BlendFactor,
                            out returnMatrix);
                    }
                        // else return the default transform
                    else
                        return defaultMatrix;
                }
                    // The bone is affected by an animation
                else
                {
                    // Find the current transform in the animation for the bone
                    currentAnimation.GetCurrentBoneTransform(this, 
                        out currentMatrixBuffer);
                    // If the bone is affected by a blend animation, blend the
                    // current animation transform with the current blend animation
                    // transform
                    if (currentBlendAnimation != null && doesBlendContainChannel)
                    {
                        currentBlendAnimation.GetCurrentBoneTransform(this,
                            out blendMatrix);
                        Util.SlerpMatrix(
                            ref currentMatrixBuffer,
                            ref blendMatrix, 
                            BlendFactor,
                            out returnMatrix);
                    }
                        // Else just return the current animation transform
                    else
                        return currentMatrixBuffer;
                }
                
                return returnMatrix;
            }
        }
    }
}
