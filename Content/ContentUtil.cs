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
using Microsoft.Xna.Framework.Graphics;

namespace Animation.Content
{

    public enum SkinningType
    {
        None,
        FourBonesPerVertex,
        EightBonesPerVertex,
        TwelveBonesPerVertex
    }

    internal class ContentUtil
    {
        public const long TICKS_PER_60FPS = TimeSpan.TicksPerSecond / 60;

        public static SkinningType CheckSkinningType(VertexElement[] elements)
        {
            int numIndexChannels = 0;
            int numWeightChannels = 0;
            foreach (VertexElement e in elements)
            {
                if (e.VertexElementUsage == VertexElementUsage.BlendIndices)
                    numIndexChannels++;
                else if (e.VertexElementUsage == VertexElementUsage.BlendWeight)
                    numWeightChannels++;
            }
            if (numIndexChannels == 3 || numWeightChannels == 3)
                return SkinningType.TwelveBonesPerVertex;
            else if (numIndexChannels == 2 || numWeightChannels == 2)
                return SkinningType.EightBonesPerVertex;
            else if (numIndexChannels == 1 || numWeightChannels == 1)
                return SkinningType.FourBonesPerVertex;
            return SkinningType.None;

        }

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

        /// <summary>
        /// Reflects a matrix across the Z axis by multiplying both the Z
        /// column and the Z row by -1 such that the Z,Z element stays intact.
        /// </summary>
        /// <param name="m">The matrix to be reflected across the Z axis</param>
        public static void ReflectMatrix(ref Matrix m)
        {
            m.M13 *= -1;
            m.M23 *= -1;
            m.M33 *= -1;
            m.M43 *= -1;
            m.M31 *= -1;
            m.M32 *= -1;
            m.M33 *= -1;
            m.M34 *= -1;
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
            {
                scale = new AnimationKeyframe[] {new AnimationKeyframe(new TimeSpan(0),
                    Matrix.Identity)};
            }
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

        /// <summary>
        /// Roughly decomposes two matrices and performs spherical linear interpolation
        /// </summary>
        /// <param name="start">Source matrix for interpolation</param>
        /// <param name="end">Destination matrix for interpolation</param>
        /// <param name="slerpAmount">Ratio of interpolation</param>
        /// <returns></returns>
        public static Matrix SlerpMatrix(Matrix start, Matrix end,
            double slerpAmount)
        {
            // Get rotation components and interpolate (not completely accurate but I don't want 
            // to get into polar decomposition and this seems smooth enough)
            Quaternion qStart = Quaternion.CreateFromRotationMatrix(start);
            Quaternion qEnd = Quaternion.CreateFromRotationMatrix(end);
            Quaternion qResult = Quaternion.Lerp(qStart, qEnd, (float)slerpAmount);

            // Get translation components 
            Vector3 curTrans = start.Translation;
            Vector3 nextTrans = end.Translation;

            // Get scale/translation matrices
            Matrix curScale = start - Matrix.CreateFromQuaternion(qStart);
            Matrix nextScale = end - Matrix.CreateFromQuaternion(qEnd);

            // Interpolate scale components linearly
            Vector3 v = Vector3.Lerp(new Vector3(curScale.M11, curScale.M22, curScale.M33),
                new Vector3(nextScale.M11, nextScale.M22, nextScale.M33),
                (float)slerpAmount);

            // Create the rotation matrix from the slerped quaternions
            Matrix result = Matrix.CreateFromQuaternion(qResult);

            // Add the lerped translation component
            result.Translation = Vector3.Lerp(curTrans, nextTrans,
                (float)slerpAmount);

            // And the lerped scale component
            result.M11 += v.X;
            result.M22 += v.Y;
            result.M33 += v.Z;
            return result;
        }


        private static void InterpFrames(
            ref AnimationKeyframe[] source,
            ref Matrix[] dest,
            IList<TimeSpan> times)
        {
            int sourceIndex = 0;
            for (int i = 0; i < times.Count; i++)
            {

                while (sourceIndex != source.Length-1 && source[sourceIndex + 1].Time < times[i])
                {
                    sourceIndex++;
                }
                if (sourceIndex==source.Length-1)
                {
                    dest[i] = source[sourceIndex].Transform;
                    continue;
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
