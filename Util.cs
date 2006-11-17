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
    public class Util
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

        public static Matrix GetDefaultProjection(Effect effect)
        {

            return Matrix.CreatePerspectiveFieldOfView(
              (float)Math.PI / 4.0f,
              (float)effect.GraphicsDevice.Viewport.Width /
              effect.GraphicsDevice.Viewport.Height,
              .1f,
              10000.0f);
            
        }

        public static Matrix DefaultView
        {
            get
            {
                return Matrix.CreateLookAt(new Vector3(0, 0, 20),
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




        public static List<AnimationKeyframe> MergeKeyFrames(AnimationKeyframe[] scale,
            AnimationKeyframe[] translation, AnimationKeyframe[] rotation)
        {
            List<AnimationKeyframe> frames = new List<AnimationKeyframe>();
            frames.Add(new AnimationKeyframe(new TimeSpan(),Matrix.Identity));
            InitFrame(ref scale);
            InitFrame(ref translation);
            InitFrame(ref rotation);
            TimeSpan[] allTimes = new TimeSpan[scale.Length + translation.Length +
                rotation.Length];
            int index = 0;
            for (int i = 0; i < scale.Length; i++)
                allTimes[index++] = scale[i].Time;
            for (int i = 0; i < translation.Length; i++)
                allTimes[index++] = translation[i].Time;
            for (int i = 0; i < rotation.Length; i++)
                allTimes[index++] = rotation[i].Time;
            Array.Sort<TimeSpan>(allTimes);
            foreach (TimeSpan time in allTimes)
                if (time > frames[frames.Count-1].Time)
                    frames.Add(new AnimationKeyframe(time, Matrix.Identity));
            int rotIndex = 0;
            int scaleIndex = 0;
            int transIndex = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                TimeSpan curTime = frames[i].Time;
                if (rotIndex != rotation.Length-1 && curTime > rotation[rotIndex + 1].Time)
                    rotIndex++;
                if (rotIndex == rotation.Length - 1)
                    frames[i].Transform = rotation[rotIndex].Transform;
                else
                {
                    double slerpAmount = (curTime.Ticks - rotation[rotIndex].Time.Ticks) /
                        (rotation[rotIndex + 1].Time.Ticks - rotation[rotIndex].Time.Ticks);
                    Quaternion q1 = Quaternion.CreateFromRotationMatrix(rotation[rotIndex].Transform);
                    Quaternion q2 = Quaternion.CreateFromRotationMatrix(rotation[rotIndex + 1].Transform);
                    Quaternion q3;
                    Quaternion.Slerp(ref q1, ref q2, (float)slerpAmount, out q3);
                    frames[i].Transform = Matrix.CreateFromQuaternion(q3);
                }

                if (scaleIndex != scale.Length-1 && curTime > scale[scaleIndex + 1].Time )
                    scaleIndex++;
                if (scaleIndex == scale.Length - 1)
                    frames[i].Transform *= scale[scaleIndex].Transform;
                else
                {
                    double scaleLerpAmount = (curTime.Ticks - scale[scaleIndex].Time.Ticks) /
                        (scale[scaleIndex + 1].Time.Ticks - scale[scaleIndex].Time.Ticks);
                    frames[i].Transform *= Matrix.Lerp(scale[scaleIndex].Transform,
                        scale[scaleIndex + 1].Transform,
                        (float)scaleLerpAmount);
                }
                if (transIndex != translation.Length - 1 && curTime > translation[transIndex + 1].Time )
                    transIndex++;
                if (transIndex == translation.Length - 1)
                    frames[i].Transform *= translation[transIndex].Transform;
                else
                {
                    double transLerpAmount = (curTime.Ticks - translation[transIndex].Time.Ticks) /
                        (translation[transIndex + 1].Time.Ticks - translation[transIndex].Time.Ticks);
                    frames[i].Transform *= Matrix.Lerp(translation[transIndex].Transform,
                        translation[transIndex + 1].Transform,
                        (float)transLerpAmount);
                }

            }

            return frames;

        }


        private static void InitFrame(ref AnimationKeyframe[] frames)
        {
            if (frames == null)
                frames = new AnimationKeyframe[] {new AnimationKeyframe(new TimeSpan(),
                    Matrix.Identity), new AnimationKeyframe(new TimeSpan(),
                    Matrix.Identity)};
            else
            {
                Array.Sort<AnimationKeyframe>(frames, new Comparison<AnimationKeyframe>(
                    delegate(AnimationKeyframe one, AnimationKeyframe two)
                    {
                        return one.Time.CompareTo(two.Time);
                    }));
            }
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
