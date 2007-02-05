/*
 * RunningAnimation.cs
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

    public class RunningAnimation
    {
        private ModelAnimation animation;
        private double speedFactor = 1.0;
        private long elapsedTime = 0;
        private int defaultFrameNum;
        private string name;
        private long elapsed;
        private bool blendAnimation = false;
        private BoneAnimationCollection bones;
        private SortedList<string, BoneAnimation> affectedBones
            = new SortedList<string, BoneAnimation>();

        internal RunningAnimation(
            ModelAnimation anim,
            BoneAnimationCollection bones,
            bool isBlendAnim)
        {
            blendAnimation = isBlendAnim;
            this.bones = bones;
            animation = anim;
            foreach (string boneName in anim.AnimationChannels.Keys)
            {
                affectedBones.Add(boneName, bones[boneName]);
                if (isBlendAnim)
                    bones[boneName].SetBlendAnimation(this);
                else
                    bones[boneName].SetRunningAnimation(this);
            }
            name = anim.Name;
        }


        internal RunningAnimation(
            ModelAnimation anim,
            BoneAnimationCollection bones,
            IList<string> affectedBoneList,
            bool isBlendAnim)
        {
            blendAnimation = isBlendAnim;
            this.bones = bones;
            animation = anim;
            foreach (string boneName in affectedBoneList)
            {
                affectedBones.Add(boneName, bones[boneName]);
                if (isBlendAnim)
                    bones[boneName].SetBlendAnimation(this);
                else
                    bones[boneName].SetRunningAnimation(this);
            }
            name = anim.Name;
        }

        internal RunningAnimation(
            ModelAnimation anim,
            BoneAnimationCollection bones)
            : this(anim, bones, false)
        {
        }
        internal RunningAnimation(
            ModelAnimation anim,
            BoneAnimationCollection bones,
            IList<string> affectedBoneList)
            : this(anim, bones, affectedBoneList, false)
        {

        }




        public IList<string> AffectedBoneNames
        {
            get { return affectedBones.Keys; }
        }

        public void AddAffectedBone(string boneName)
        {
            affectedBones.Add(boneName, bones[boneName]);
            if (blendAnimation)
                bones[boneName].SetBlendAnimation(this);
            else
                bones[boneName].SetRunningAnimation(this);
        }

        public void RemoveAffectedBone(string boneName)
        {
            affectedBones.Remove(boneName);
            if (blendAnimation)
                bones[boneName].SetBlendAnimation(null);
            else
                bones[boneName].SetRunningAnimation(null);
        }

        public void RemoveAllBones()
        {
            foreach (BoneAnimation anim in affectedBones.Values)
            {
                if (blendAnimation)
                    anim.SetBlendAnimation(null);
                else
                    anim.SetRunningAnimation(null);
            }
            affectedBones.Clear();
        }





        public void AdvanceTime(GameTime time)
        {
            elapsed = (long)(speedFactor * time.ElapsedRealTime.Ticks);
            if (elapsed != 0)
            {
                elapsedTime = (elapsedTime + elapsed) % animation.Duration;
                if (elapsedTime < 0)
                    elapsedTime = animation.Duration;
            }
            defaultFrameNum = (int)(elapsedTime / Util.TICKS_PER_60FPS);
        }



        internal int DefaultFrameNum
        {
            get { return defaultFrameNum; }
        }

        public ModelAnimation AnimationSource
        {
            get { return animation; }
        }

        public long ElapsedTime
        {
            get { return elapsedTime; }
            set
            {
                elapsedTime = value % animation.Duration;
                defaultFrameNum = (int)(elapsedTime / Util.TICKS_PER_60FPS);
            }
        }

        public double SpeedFactor
        {
            get { return speedFactor; }
            set { speedFactor = value; }
        }
    }

}
