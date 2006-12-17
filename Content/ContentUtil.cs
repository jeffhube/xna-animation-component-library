/*
 * ContentUtil.cs
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
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Animation.Content
{

    [ContentTypeWriter]
    public class SkinTransformWriter : ContentTypeWriter<SkinTransform>
    {
        protected override void Write(ContentWriter output, SkinTransform value)
        {
            output.Write(value.BoneName);
            output.Write(value.Transform);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(SkinTransformReader).AssemblyQualifiedName;
        }
    }

    internal class ContentUtil
    {
        private static void InitializeFrames(ref AnimationKeyframe[] frames)
        {
            SortFrames(ref frames);
            if (frames[0].Time != TimeSpan.Zero)
            {
                AnimationKeyframe[] newFrames = new AnimationKeyframe[frames.Length + 1];
                Array.ConstrainedCopy(frames, 0, newFrames, 1, frames.Length);
                newFrames[0] = frames[0];
                frames = newFrames;
            }
        }


        private static void SortFrames(ref AnimationKeyframe[] frames)
        {

            Array.Sort<AnimationKeyframe>(frames, new Comparison<AnimationKeyframe>(
                delegate(AnimationKeyframe one, AnimationKeyframe two)
                {
                    return one.Time.CompareTo(two.Time);
                }));

        }

        public static List<AnimationKeyframe> MergeKeyFrames(AnimationKeyframe[] scale,
    AnimationKeyframe[] translation, AnimationKeyframe[] rotation)
        {
            if (scale == null)
                throw new Exception("Animation data is not stored as matrices and " +
                    "has no scale component.");
            if (translation == null)
                throw new Exception("Animation data is not stored as matrices and " +
                    "has no translation component.");
            if (rotation == null)
                throw new Exception("Animation data is not stored as matrices and " +
                    "has no rotation component");

            InitializeFrames(ref scale);
            InitializeFrames(ref translation);
            InitializeFrames(ref rotation);
            SortedList<TimeSpan, object> keyframeTimes
                = new SortedList<TimeSpan, object>();
            foreach (AnimationKeyframe frame in scale)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);
            foreach (AnimationKeyframe frame in translation)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);
            foreach (AnimationKeyframe frame in rotation)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);
            IList<TimeSpan> times = keyframeTimes.Keys;
            Matrix[] newScales = new Matrix[keyframeTimes.Count];
            Matrix[] newTrans = new Matrix[keyframeTimes.Count];
            Matrix[] newRot = new Matrix[keyframeTimes.Count];
            List<AnimationKeyframe> returnFrames = new List<AnimationKeyframe>();

            InterpFrames(ref scale, ref newScales, times);
            InterpFrames(ref translation, ref newTrans, times);

            InterpFrames(ref rotation, ref newRot, times);
            for (int i = 0; i < times.Count; i++)
            {


                Matrix m = newRot[i];
                m = m * newTrans[i];
                m = newScales[i] * m;



                returnFrames.Add(new AnimationKeyframe(times[i], m));
            }

            return returnFrames;

        }


        private static void InterpFrames(
            ref AnimationKeyframe[] source,
            ref Matrix[] dest,
            IList<TimeSpan> times)
        {
            int sourceIndex = 0;
            for (int i = 0; i < times.Count; i++)
            {
                if (source.Length == 1)
                {
                    dest[i] = source[sourceIndex].Transform;
                    continue;
                }
                while (source[sourceIndex + 1].Time < times[i])
                {
                    sourceIndex++;
                }
                if (source[sourceIndex].Time == times[i])
                {
                    dest[i] = source[sourceIndex].Transform;
                }
                else
                {
                    double interpAmount = ((double)times[i].Ticks - source[sourceIndex].Time.Ticks) /
                        ((double)source[sourceIndex + 1].Time.Ticks - source[sourceIndex].Time.Ticks);


                    Matrix m1 = source[sourceIndex].Transform;
                    Matrix m2 = source[sourceIndex + 1].Transform;

                    dest[i] = Matrix.Lerp(m1, m2, (float)interpAmount);




                }

            }

        }
    }
}
