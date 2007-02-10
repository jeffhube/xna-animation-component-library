/*
 * Util.cs
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




using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
namespace Animation
{
    public enum SkinningType
    {
        None,
        FourBonesPerVertex,
        EightBonesPerVertex,
        TwelveBonesPerVertex
    }
    /// <summary>
    /// Provides various animation utilities.
    /// </summary>
    public sealed class Util
    {
        public const long TICKS_PER_60FPS = TimeSpan.TicksPerSecond / 60;

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
         
    }



    public abstract class FiniteStateMachine : GameComponent
    {
        private List<string> states = new List<string>();
        private string currentState;
        private TimeSpan timeInState;
        private bool transitioned = false;

        public FiniteStateMachine(Game game,
            string initialState)
            : base(game)
        {
            states.Add(initialState);
            currentState = initialState;
            game.Components.Add(this);
        }


        public List<string> States
        { get { return states; } }

        public string CurrentState
        {
            get { return currentState; }
        }

        protected void Transition(string targetStateName)
        {
            string oldName = currentState;
            currentState = targetStateName;
            timeInState = new TimeSpan();
            transitioned = true;
            OnTransition(oldName);
        }

        /// <summary>
        /// This is fired when a state transition occurs.
        /// </summary>
        /// <param name="oldStateName">The name of the state from which
        /// this state was transitioned.</param>
        protected abstract void OnTransition(string oldStateName);

        public TimeSpan TimeInState
        {
            get { return timeInState; }
        }

        protected abstract void RunCurrentState(GameTime gameTime);

        public sealed override void Update(GameTime gameTime)
        {
            if (currentState != null)
            {
                transitioned = false;
                RunCurrentState(gameTime);
                while (transitioned)
                {
                    transitioned = false;
                    if (currentState != null)
                        RunCurrentState(gameTime);
                }
                timeInState += gameTime.ElapsedGameTime;
            }
            base.Update(gameTime);
        }

    }


}
