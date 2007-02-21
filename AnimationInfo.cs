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
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace XCLNA.XNA.Animation
{
    public class BoneKeyframeCollection : ReadOnlyCollection<BoneKeyframe>
    {
        private string boneName;

        private long duration;

        internal BoneKeyframeCollection(string boneName,
            IList<BoneKeyframe> list) : base(list)
        {
            this.boneName = boneName;
            duration = list[list.Count - 1].Time;
        }

        public long Duration
        {
            get { return duration; }
        }
        public string BoneName
        { get { return boneName; } }

        public int GetIndexByTime(long ticks)
        {
            int firstFrameIndexToCheck = (int)(ticks / Util.TICKS_PER_60FPS);
            if (firstFrameIndexToCheck > base.Count)
                firstFrameIndexToCheck = base.Count - 1;

            while (firstFrameIndexToCheck < base.Count
                    && base[firstFrameIndexToCheck].Time < ticks)
            {
                ++firstFrameIndexToCheck;
            }
            while (firstFrameIndexToCheck > 0 && base[firstFrameIndexToCheck - 1].Time >
                ticks)
            {
                --firstFrameIndexToCheck;
            }
            return firstFrameIndexToCheck;
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

    public class AnimationChannelCollection : ReadOnlyCollection<BoneKeyframeCollection>
    {
        private Dictionary<string, BoneKeyframeCollection> dict =
            new Dictionary<string, BoneKeyframeCollection>();
        private ReadOnlyCollection<string> affectedBones;
        public AnimationChannelCollection(IList<BoneKeyframeCollection> channels)
            : base(channels)
        {
            List<string> affected = new List<string>();
            foreach (BoneKeyframeCollection frames in channels)
            {
                dict.Add(frames.BoneName, frames);
                affected.Add(frames.BoneName);
            }
            affectedBones = new ReadOnlyCollection<string>(affected);

        }
        public BoneKeyframeCollection this[string boneName]
        {
            get { return dict[boneName]; }
        }

        internal ReadOnlyCollection<string> AffectedBones
        {
            get { return affectedBones; }
        }
    }

    public class AnimationInfo
    {
        private long duration = 0;
        private string animationName;
        private int maxNumFrames = 1;

        private AnimationChannelCollection boneAnimations;
        

        internal AnimationInfo(string animationName, AnimationChannelCollection 
            anims)
        {
            this.animationName = animationName;
            boneAnimations = anims;
            foreach (BoneKeyframeCollection channel in anims)
            {
                if (channel.Duration > duration)
                    duration = channel.Duration;
            }
        }


        public AnimationChannelCollection AnimationChannels
        { get { return boneAnimations; } }

        public ReadOnlyCollection<string> AffectedBones
        { get { return boneAnimations.AffectedBones; } }


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

        public static AnimationInfoCollection FromModel(Model model)
        {
            // Grab the tag that was set in the processor; this is a dictionary so that users can extend
            // the processor and pass their own data into the program without messing up the animation data
            Dictionary<string, object> modelTagData = (Dictionary<string, object>)model.Tag;
            if (modelTagData == null || !modelTagData.ContainsKey("Animations"))
            {
                return new AnimationInfoCollection();
            }
            else
            {
                // Now grab the animation info and store local references
                AnimationInfoCollection animations = (AnimationInfoCollection)modelTagData["Animations"];
                return animations;
            }
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
