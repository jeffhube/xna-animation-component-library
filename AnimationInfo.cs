/*
 * ModelAnimationInfo.cs
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


    public struct BoneKeyframe
    {
        public readonly Matrix Transform;
        public readonly long Time;
        public BoneKeyframe(Matrix transform, long time)
        {
            this.Transform = transform;
            this.Time = time;
            
        }
    }

    public class AnimationInfo
    {
        private long duration;
        private string animationName;
        private int maxNumFrames = 1;
        
        private SortedList<string, BoneKeyframeCollection> boneAnimations =
            new SortedList<string, BoneKeyframeCollection>();
        

        internal AnimationInfo(string animationName)
        {
            this.animationName = animationName;
        }

        internal void AddAnimationChannel(BoneKeyframeCollection collection)
        {
            boneAnimations.Add(collection.BoneName,collection);
            if (collection.Duration > duration)
                duration = collection.Duration;
            if (collection.Count > maxNumFrames)
                maxNumFrames = collection.Count;
        }

        public SortedList<string, BoneKeyframeCollection> AnimationChannels
        { get { return boneAnimations; } }

        public IList<string> AffectedBones
        { get { return boneAnimations.Keys; } }


        public long Duration
        {
            get { return duration; }
        }
        public string Name
        {
            get { return animationName; }
        }
        public int MaxNumFrames
        {
            get { return maxNumFrames; }
        }
    }

    public class AnimationInfoCollection : SortedList<string, AnimationInfo>
    {
        internal AnimationInfoCollection()
        {
        }

        public AnimationInfo this[int index]
        {
            get
            {
                return this.Values[index];
            }
        }

    }




}
