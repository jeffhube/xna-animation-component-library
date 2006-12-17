/*
 * PaletteEffectContent.cs
 * Copyright (c) 2006 Michael Nikonov, David Astle
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

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Animation
{
    /// <summary>
    /// A viewer for animated models. To view your model, just do 
    ///     ModelViewer viewer = new ModelViewer(this, model);
    /// </summary>
    public partial class ModelViewer : Microsoft.Xna.Framework.IGameComponent,
        IUpdateable
    {
        Model model;
        AnimationController controller;
        private BoundingSphere sphere;

        Matrix world, view, projection;
        Vector3 eyePos, up;
        float fov, near, far, width, height, cx, cy, aspect;
        float arcRadius;
        Viewport viewPort;
        public ModelViewer(Game game, Model model)
        {
            this.model = model;

            controller = new Animation.AnimationController(game, model, 0);
            controller.World = Matrix.CreateRotationY(MathHelper.Pi / 4.0f);
            controller.Enabled = true;
            controller.Visible = true;
            game.Components.Add(this);
            game.IsMouseVisible = true;
            sphere = model.Meshes[0].BoundingSphere;
            foreach (ModelMesh mesh in model.Meshes)
                sphere = BoundingSphere.CreateMerged(sphere, mesh.BoundingSphere);
            IGraphicsDeviceService graphics = 
                (IGraphicsDeviceService)game.Services.GetService(
                typeof(IGraphicsDeviceService));
            viewPort = graphics.GraphicsDevice.Viewport;
            eyePos = new Vector3(0, 0,sphere.Radius*5);
            arcRadius = eyePos.Length() / 2.0f;
            width = (float)viewPort.Width;
            height = (float)viewPort.Height;
            fov = MathHelper.PiOver4;
            near = .1f;
            far = 10000.0f;
            cx = width / 2.0f;
            cy = height / 2.0f;
            aspect = width / height;
            up = Vector3.Up;
            projection = Matrix.CreatePerspectiveFieldOfView(
                fov, aspect, near, far);

            view = Matrix.CreateLookAt(
                eyePos, Vector3.Zero, up);
            world = Matrix.Identity;
            InitializeEffects();
        }


        MouseState lastState;

        private void InitializeEffects()
        {

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect ef in mesh.Effects)
                {
                    ef.Parameters["View"].SetValue(view);
                    ef.Parameters["EyePosition"].SetValue(eyePos);
                    ef.Parameters["Projection"].SetValue(projection);
                    ef.Parameters["World"].SetValue(Matrix.Identity);

                    if (ef is BasicPaletteEffect)
                    {
                        BasicPaletteEffect effect = (BasicPaletteEffect)ef;

                        effect.EnableDefaultLighting();
                        effect.DirectionalLight0.Direction = new Vector3(0, 0, -1);
                    }
                    else if (ef is BasicEffect)
                    {
                        BasicEffect effect = (BasicEffect)ef;
                        effect.EnableDefaultLighting();
                        effect.DirectionalLight0.Direction = new Vector3(0, 0, -1);
                        effect.DirectionalLight1.Enabled = false;
                        effect.DirectionalLight2.Enabled = false;
                        effect.AmbientLightColor = Color.Black.ToVector3();
                        effect.EmissiveColor = Color.Black.ToVector3();
                    }
                }
            }
        }


        #region IUpdateable Members

        bool IUpdateable.Enabled
        {
            get { return true; }
        }

        public AnimationController Controller
        {
            get { return controller; }
        }

        public Model Model
        {
            get { return model; }
        }

        bool IntersectPoint(int x, int y, out Vector3 intPt)
        {
            Vector3 loc = new Vector3(x, y, 0);
            loc = viewPort.Unproject(loc, projection, view, Matrix.Identity);
            Vector3 dir = loc - eyePos;
            dir.Normalize();
            Ray r = new Ray(eyePos, dir);
            float? f = r.Intersects(
                new BoundingSphere(
                Vector3.Zero,
                arcRadius));
            if (f == null)
            {
                intPt = Vector3.Zero;
                return false;
            }
            else
            {
                intPt = eyePos + (dir * ((float)f));
                return true;
            }
        }

        void IUpdateable.Update(GameTime gameTime)
        {
            MouseState state = Mouse.GetState();
            if (state.ScrollWheelValue != lastState.ScrollWheelValue)
            {
                float zoom = sphere.Radius * 5 -
                    (sphere.Radius / 10.0f) * 
                    (((float)state.ScrollWheelValue) / 20.0f);
                if (zoom < sphere.Radius)
                    zoom = sphere.Radius;
                arcRadius = zoom / 2.0f;
                Vector3 n = Vector3.Normalize(eyePos);
                eyePos = n * zoom;
                view = Matrix.CreateLookAt(
                    eyePos, Vector3.Zero,
                    up);
            }

            if ((state.LeftButton == ButtonState.Pressed
                || state.RightButton == ButtonState.Pressed)
                && (state.X != lastState.X || state.Y != lastState.Y) )
            {
                Vector3 curPt, lastPt;
                if (IntersectPoint(lastState.X, lastState.Y, out lastPt)
                    && IntersectPoint(state.X, state.Y, out curPt))
                {
                    Vector3 cross = Vector3.Cross(lastPt, curPt);
                    cross.Normalize();
                    lastPt.Normalize();
                    curPt.Normalize();
                    float ang = (float)Math.Acos(Vector3.Dot(lastPt,
                        curPt));
                    Matrix axisRot = Matrix.CreateFromAxisAngle(
                        cross, ang * 2.0f);
                    if (state.LeftButton == ButtonState.Pressed)
                        world *= axisRot;
                    if (state.RightButton == ButtonState.Pressed)
                    {
                        Matrix invertRot = Matrix.Invert(axisRot);

                        eyePos = Vector3.Transform(eyePos,
                            invertRot);
                        up = Vector3.Normalize(Vector3.Transform(up,
                            invertRot));
                        view = Matrix.CreateLookAt(
                            eyePos, Vector3.Zero,
                            up);
                    }
                }

            }
         
            controller.World = world;
            lastState = state;
            
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["View"].SetValue(view);
                    effect.Parameters["EyePosition"].SetValue(eyePos);
                }
            }
             
        }

        int IUpdateable.UpdateOrder
        {
            get { return 0; }
        }


        #endregion

        #region IUpdateable Members

        private event EventHandler enabledChanged;
        event EventHandler IUpdateable.EnabledChanged
        {
            add { enabledChanged += value; }
            remove { enabledChanged -= value; }
        }

        private event EventHandler updateOrderChanged;
        event EventHandler IUpdateable.UpdateOrderChanged
        {
            add { updateOrderChanged += value; }
            remove { updateOrderChanged -= value; }
        }



        #endregion

        #region IGameComponent Members

        public void Initialize()
        {

        }

        #endregion
    }
}


