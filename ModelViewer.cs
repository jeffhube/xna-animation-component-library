/*
 * ModelViewer.cs
 * A simple viewer for animated models.
 * Part of XNA Animation Component library, which is a library for animation
 * in XNA
 * 
 * Copyright (C) 2006 Michael Nikonov
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
        private BoundingSphere sphere;
        AnimationController controller;
        Matrix world, view, projection;
        Vector3 eyePos, up;
        float fov, near, far, width, height, cx, cy, aspect;
        float arcRadius;
        Viewport viewPort;
        public ModelViewer(Game game, Model model)
        {
            this.model = model;

            controller = new Animation.AnimationController(game, model, 0);
            controller.SpeedFactor = 1;
            controller.InterpolationMethod = Animation.InterpolationMethod.Linear;
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
            eyePos = new Vector3(0, 0,-sphere.Radius*5);
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
                    if (ef.GetType()==typeof(BasicPaletteEffect))
                    {
                        BasicPaletteEffect effect = (BasicPaletteEffect)ef;
                        effect.Parameters["View"].SetValue(view);
                        effect.Parameters["EyePosition"].SetValue(eyePos);
                        effect.Parameters["Projection"].SetValue(projection);
                        ef.Parameters["World"].SetValue(Matrix.Identity);
                        effect.EnableDefaultLighting();
                        effect.DirectionalLight0.Direction = new Vector3(0, 0, 1);
                    }
                }
            }
        }


        #region IUpdateable Members

        bool IUpdateable.Enabled
        {
            get { return true; }
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


