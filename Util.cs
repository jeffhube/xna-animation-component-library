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
            effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                  (float)Math.PI / 4.0f,
                  (float)effect.GraphicsDevice.Viewport.Width /
                  effect.GraphicsDevice.Viewport.Height,
                  .1f,
                  10000.0f);
            effect.View = Matrix.CreateLookAt(new Vector3(0, 0,20),
                new Vector3(0, 0, 0),
                Vector3.Up);
            effect.World = Matrix.Identity;
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
