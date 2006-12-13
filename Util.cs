/*
 * Util.cs
 * Contains miscellaneous methods used by the animation library
 * Part of XNA Animation Component library, which is a library for animation
 * in XNA
 * 
 * Copyright (C) 2006 David Astle
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */




using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Collections.Generic;
namespace Animation
{
    internal enum SkinningType
    {
        None,
        FourBonesPerVertex,
        EightBonesPerVertex
    }
    /// <summary>
    /// Provides various animation utilities.
    /// </summary>
    public sealed class Util
    {
        /// <summary>
        /// Initializes a basic effect to some reasonable values because I hate doing this
        /// every time I start a new game projection
        /// </summary>
        /// <param name="effect">The effect to be initialized</param>
        public static void InitializeBasicEffect(BasicEffect effect)
        {
            // Create some lighting
            effect.LightingEnabled = true;
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.Direction = new Vector3(0, 0, -1);
            effect.DirectionalLight0.DiffuseColor = Color.White.ToVector3();
            // Set the camera
            effect.Projection = GetDefaultProjection(effect);
            effect.View = DefaultView;
            effect.World = Matrix.Identity;
        }

        /// <summary>
        /// Gets a reasonable projection matrix for easy effect initialization.
        /// 45 degrees FOV and viewport width/viewport height aspect ratio.
        /// </summary>
        /// <param name="effect">The effect attached to a device</param>
        /// <returns>A reasonable default projection matrix</returns>
        public static Matrix GetDefaultProjection(Effect effect)
        {

            return Matrix.CreatePerspectiveFieldOfView(
              (float)Math.PI / 4.0f,
              (float)effect.GraphicsDevice.Viewport.Width /
              effect.GraphicsDevice.Viewport.Height,
              .1f,
              10000.0f);
            
        }

        /// <summary>
        /// Gets a reasonable default view matrix.
        /// </summary>
        public static Matrix DefaultView
        {
            get
            {
                return Matrix.CreateLookAt(new Vector3(0, 20, 20),
                new Vector3(0, 0, 0),
                Vector3.Up);
            }
        }

        /// <summary>
        /// Calls InitializeBasicEffect on every effect in a model
        /// </summary>
        /// <param name="model">The model</param>
        public static void InitializeAllEffects(Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
                foreach (BasicEffect effect in mesh.Effects)
                    InitializeBasicEffect(effect);
        }

        internal static SkinningType CheckSkinned(VertexElement[] elements)
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
            if (numIndexChannels == 2 || numWeightChannels == 2)
                return SkinningType.EightBonesPerVertex;
            else if (numIndexChannels == 1 || numWeightChannels == 1)
                return SkinningType.FourBonesPerVertex;
            return SkinningType.None;

        }

        static bool doit = false;
        internal static List<AnimationKeyframe> MergeKeyFrames(AnimationKeyframe[] scale,
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
            doit = !(scale[0].Transform == Matrix.Identity);
            InitializeFrames(ref scale);
            InitializeFrames(ref translation);
            InitializeFrames(ref rotation);
            SortedList<TimeSpan, object> keyframeTimes
                = new SortedList<TimeSpan, object>();
            foreach (AnimationKeyframe frame in scale)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);
            foreach(AnimationKeyframe frame in translation)
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
                m = m*newTrans[i];
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
                if (source.Length==1)
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
                    Matrix m2 = source[sourceIndex+1].Transform;

                    dest[i] = Matrix.Lerp(m1, m2, (float)interpAmount);


                    

                }
                   
            }

        }


        /// <summary>
        /// Reflects a matrix across the Z axis by multiplying both the Z
        /// column and the Z row by -1 such that the Z,Z element stays intact.
        /// </summary>
        /// <param name="m">The matrix to be reflected across the Z axis</param>
        public static  void ReflectMatrix(ref Matrix m)
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

        private static T Max<T>(params T[] items) where T : IComparable
        {
            IComparable max = null;
            foreach (IComparable c in items)
            {
                if (max == null)
                    max = c;
                else
                {
                    if (c.CompareTo(max) > 0)
                        max = c;
                }
            }
            return (T)max;
        }



        private static void SortFrames(ref AnimationKeyframe[] frames)
        {

            Array.Sort<AnimationKeyframe>(frames, new Comparison<AnimationKeyframe>(
                delegate(AnimationKeyframe one, AnimationKeyframe two)
                {
                    return one.Time.CompareTo(two.Time);
                }));

        }
    


        /// <summary>
        /// Converts from an array of bytes to any vertex type.
        /// </summary>
        /// <typeparam name="T">The type of vertex to which we are converting the bytes</typeparam>
        /// <param name="data">The bytes that will be converted to the vertices</param>
        /// <param name="vertexSize">The size of one vertex</param>
        /// <param name="device">Any working device; required to use our conversion hack</param>
        /// <returns>An array of the converted vertices</returns>
        public static T[] Convert<T>(byte[] data, int vertexSize,
            GraphicsDevice device) where T : struct
        {
            T[] verts = new T[data.Length / vertexSize];
            using (VertexBuffer vb = new VertexBuffer(device, data.Length, ResourceUsage.None))
            {
                vb.SetData<byte>(data);
                vb.GetData<T>(verts);
            }
            return verts;
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
            Quaternion qResult = Quaternion.Slerp(qStart, qEnd, (float)slerpAmount);

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
         
    }


}
