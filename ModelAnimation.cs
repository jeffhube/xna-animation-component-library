/*
 * ModelAnimation.cs
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

namespace Animation
{
    public class BoneKeyframeCollection : List<BoneKeyframe>
    {
        private string boneName;

        private long duration;

        internal BoneKeyframeCollection(string boneName)
        {
            this.boneName = boneName;
            duration = 0;
        }

        public long Duration
        {
            get { return duration; }
        }
        public string BoneName
        { get { return boneName; } }

        
        public void AddKeyframe(BoneKeyframe frame)
        {
            Add(frame);
            if (frame.Time > duration)
                duration = frame.Time;
        }


    }

    public class BoneKeyframe
    {
        public readonly Matrix Transform;
        public readonly long Time;
        public BoneKeyframe(Matrix transform, long time)
        {
            this.Transform = transform;
            this.Time = time;
            
        }
    }


    public class ModelAnimationBuilder
    {
        private ModelAnimation animation;
        private SortedDictionary<string, BoneKeyframeCollection> boneAnimations;

        public void StartAnimation(string animationName)
        {
            animation = new ModelAnimation(animationName);
            boneAnimations = new SortedDictionary<string, BoneKeyframeCollection>();
        }

        public void AddKeyframe(string boneName, Matrix transform, long time)
        {
            if (!boneAnimations.ContainsKey(boneName))
                boneAnimations.Add(boneName, new BoneKeyframeCollection(boneName));
            boneAnimations[boneName].AddKeyframe(new BoneKeyframe(transform, time));

        }
        public ModelAnimation FinishAnimation()
        {
            if (animation == null)
                throw new Exception("Can not finish an animation if one was not started.");
            foreach (BoneKeyframeCollection keyFrames in boneAnimations.Values)
                animation.AddBoneAnimation(keyFrames);
            ModelAnimation tmp = animation;
            animation = null;
            return tmp;
        }

    }

    public class ModelAnimation
    {
        private long duration;
        private string animationName;
        private InterpolatedAnimation interpAnim = null;
        
        private SortedList<string, BoneKeyframeCollection> boneAnimations =
            new SortedList<string, BoneKeyframeCollection>();

        internal ModelAnimation(string animationName)
        {
            this.animationName = animationName;
        }
        internal void AddBoneAnimation(BoneKeyframeCollection collection)
        {
            boneAnimations.Add(collection.BoneName,collection);
            if (collection.Duration > duration)
                duration = collection.Duration;
        }

        public SortedList<string, BoneKeyframeCollection> BoneAnimations
        { get { return boneAnimations; } }

        public void Interpolate(long timeStep, AnimationInterpolator interpolator)
        {
            interpolator.Animation = this;
            interpolator.Reset();
            interpAnim = new InterpolatedAnimation(timeStep, interpolator);
        }

        internal void SetInterpolatedAnimation(InterpolatedAnimation anim)
        {
            this.interpAnim = anim;
        }

        public InterpolatedAnimation InterpolatedAnimation
        {
            get { return interpAnim; }
        }

        public long Duration
        {
            get { return duration; }
        }
        public string Name
        {
            get { return animationName; }
        }
    }

    public class ModelAnimationCollection : Dictionary<string, ModelAnimation>
    {
        private MeshInfo meshInfo;
        internal ModelAnimationCollection(MeshInfo meshInfo)
        {
            this.meshInfo = meshInfo;
        }

        public MeshInfo MeshInfo
        {
            get { return meshInfo; }
        }

        public ModelAnimation this[int index]
        {
            get
            {
                int i = 0;
                Dictionary<string,ModelAnimation>.ValueCollection.Enumerator enumer = base.Values.GetEnumerator();
                enumer.MoveNext();
                while (i < index)
                {
                    enumer.MoveNext();
                    i++;
                }
                return enumer.Current;

            }
        }

    }




}
