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
#endregion

namespace Animation
{
    /// <summary>
    /// A viewer for animated models. To view your model, just do 
    ///     ModelViewer viewer = new ModelViewer(this, model);
    /// </summary>
    public partial class ModelViewer : Microsoft.Xna.Framework.DrawableGameComponent
    {
        Model model;
        float modelRadius = 0.0f;
        AnimationController controller;

        public ModelViewer(Game game, Model model)
            : base(game)
        {
            this.model = model;
            InitializeEffects();

            controller = new Animation.AnimationController(Game, model, 0);
            controller.SpeedFactor = 1;
            controller.InterpolationMethod = Animation.InterpolationMethod.Linear;
            controller.World = Matrix.CreateRotationY(MathHelper.Pi / 4.0f);
            controller.Enabled = true;
            controller.Visible = true;

        }

        private void InitializeEffects()
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                modelRadius = Math.Max(modelRadius, mesh.BoundingSphere.Radius);
            }
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect ef in mesh.Effects)
                {
                    if (ef.GetType()==typeof(BasicPaletteEffect))
                    {
                        BasicPaletteEffect effect = (BasicPaletteEffect)ef;
                        effect.World = Matrix.Identity;
                        effect.View = Matrix.CreateLookAt(
                                new Vector3(0, 0, modelRadius * 5),
                                Vector3.Zero,
                                Vector3.Up);
                        effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                            (float)Math.PI / 4.0f,
                            (float)effect.GraphicsDevice.Viewport.Width /
                            effect.GraphicsDevice.Viewport.Height,
                            modelRadius / 64.0f,
                            modelRadius * 200.0f);

                        effect.LightingEnabled = true;
                        effect.TextureEnabled = true;
                        effect.DiffuseColor = Color.White.ToVector3();
                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight0.Direction = Vector3.Forward;
                        effect.DirectionalLight0.DiffuseColor = Color.Turquoise.ToVector3();
                    }
                }
            }
        }

    }
}


