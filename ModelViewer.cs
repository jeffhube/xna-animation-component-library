/*
 * ModelViewer.cs
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
#define XBOX360
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
    ///     new ModelViewer(game, model);
    /// </summary>
    public partial class ModelViewer : DrawableGameComponent
    {
        List<Model> models=new List<Model>();
        private List<ModelAnimator> animators=new List<ModelAnimator>();
        List<Effect> effects=new List<Effect>();

        private BoundingSphere sphere;
   
        Matrix world, view, projection;
        Vector3 cameraPosition, up;
        float fov, near, far, width, height, cx, cy, aspect;
        float arcRadius;
        Viewport viewPort;
        Vector3 modelPos = Vector3.One;
#if WINDOWS
        MouseState lastState;
        KeyboardState lastKeyboardState;
#endif

        public System.Collections.ObjectModel.ReadOnlyCollection<ModelAnimator> Animators
        {
            get { return animators.AsReadOnly(); }
        }

        public ModelViewer(Game game): base(game)
        {
            sphere=new BoundingSphere(Vector3.Zero, 1.0f);
            game.Components.Add(this);
            game.IsMouseVisible = true;
            UpdateOrder = 0;
            IGraphicsDeviceService graphics = 
                (IGraphicsDeviceService)game.Services.GetService(
                typeof(IGraphicsDeviceService));
            viewPort = graphics.GraphicsDevice.Viewport;
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
            world = Matrix.Identity;
        }

        public ModelViewer(Game game, Model model)
            : this(game)
        {
            Add(model);
        }

        public void Add(Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
                sphere = BoundingSphere.CreateMerged(sphere, mesh.BoundingSphere);
            models.Add(model);
            ModelAnimator controller=new ModelAnimator(Game, model);
            controller.World = Matrix.CreateRotationY(MathHelper.Pi / 4.0f);
            controller.Enabled = true;
            controller.Visible = true;
            animators.Add(controller);
            InitializeEffects(model);
            Arrange();
        }

        private void Arrange()
        {
            int columns = animators.Count;
            if (columns > 5)
                columns = 5;
            for (int i = 0; i < animators.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                Matrix t=Matrix.CreateTranslation(sphere.Radius * (column-columns/2), 0, - sphere.Radius * row);
                animators[i].World = t * world;
            }
        }

        private void InitializeEffects(Model model)
        {
            cameraPosition = new Vector3(0, 0, sphere.Radius * 5);
            arcRadius = cameraPosition.Length() / 2.0f;
            view = Matrix.CreateLookAt(
                cameraPosition, Vector3.Zero, up);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect ef in mesh.Effects)
                {
                    effects.Add(ef);
                    ef.Parameters["View"].SetValue(view);
                    ef.Parameters["EyePosition"].SetValue(cameraPosition);
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

        
        // Returns true if the user clicked on the boundings sphere, false otherwise
        bool IntersectPoint(int x, int y,
            out Vector3 intersectionPoint)
        {
            BoundingSphere sphere = new BoundingSphere(new Vector3(),
                arcRadius);
            // Convert the mouse position to a 3d vector
            Vector3 location = new Vector3(x, y, 0);
            // Location of mouse point converted to true 3d space
            location = viewPort.Unproject(location, projection, view, Matrix.Identity);
            // direction vector of camera
            Vector3 direction = location - cameraPosition;
            direction.Normalize();
            // Ray in the direction of the direction vector
            Ray r = new Ray(cameraPosition, direction);
            // Used to calculate where the ray intersects the sphere
            float? intersectFactor = r.Intersects(sphere);
            if (intersectFactor == null)
            {
                intersectionPoint = Vector3.Zero;
                return false;
            }
            else
            {
                intersectionPoint = cameraPosition + 
                    (direction * ((float)intersectFactor));
                return true;
            }
        }

        public override void Update(GameTime gameTime)
        {
#if WINDOWS
            MouseState state = Mouse.GetState();
            KeyboardState ks = Keyboard.GetState();
            if (state.ScrollWheelValue != lastState.ScrollWheelValue)
            {
                float zoom = sphere.Radius * 5 -
                    (sphere.Radius / 10.0f) * 
                    (((float)state.ScrollWheelValue) / 20.0f);
                if (zoom < sphere.Radius)
                    zoom = sphere.Radius;
                arcRadius = zoom / 2.0f;
                Vector3 n = Vector3.Normalize(cameraPosition);
                cameraPosition = n * zoom;
                view = Matrix.CreateLookAt(
                    cameraPosition, Vector3.Zero,
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
                    {
                        world *= axisRot;
                    }

                    if (state.RightButton == ButtonState.Pressed &&
                        ks.IsKeyUp(Keys.LeftShift))
                    {
                        Matrix invertRot = Matrix.Invert(axisRot);

                        cameraPosition = Vector3.Transform(cameraPosition,
                            invertRot);
                        up = Vector3.Normalize(Vector3.Transform(up,
                            invertRot));
                        view = Matrix.CreateLookAt(
                            cameraPosition, Vector3.Zero,
                            up);
                    }

                    if (state.RightButton == ButtonState.Pressed
                        && ks.IsKeyDown(Keys.LeftShift))
                    {
                        modelPos.X = state.X - lastState.X;
                        modelPos.Y = -state.Y + lastState.Y;
                        world *= Matrix.CreateTranslation(modelPos);
                    }
                }

            }

            Arrange();

            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Space) && !lastKeyboardState.IsKeyDown(Keys.Space))
            {
                ++animationIndex;
                if (animationIndex >= animators[0].Animations.Count)
                    animationIndex = 0;

            }

            lastState = state;
            lastKeyboardState = keyboardState;

            foreach (Effect effect in effects)
            {
                effect.Parameters["View"].SetValue(view);
                effect.Parameters["EyePosition"].SetValue(cameraPosition);
            }
#endif
        }
    }
}


