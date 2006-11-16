/*
 * AnimationController.cs
 * A component for animating models loaded with AnimatedModelProcessor
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

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Collections.Generic;
using System;
using Animation.Content;
#endregion


namespace Animation
{
    /// <summary>
    /// Animates and draws a model that was processed with AnimatedModelProcessor
    /// </summary>
    public class AnimationController : IDrawable, IUpdateable,
        IGameComponent
    {
        #region Member Variables
        // Model to be animated
        private Model model;
        // If we are precomputing interpolations, this will store all of the
        // bone poses for every frame of the animation
        private BonePoseTable table = null;
        private int updateOrder = 0;
        private int drawOrder = 0;
        // Total time elapsed since start of animation, resets when it
        // passes the total animation time
        private long elapsedTime = 0;
        // The number of calls to Update(); used for Poor quality animations
        // in which we update every other call
        private int numUpdates = 0;
        private bool enabled = true;
        private bool visible = true;
        // Contains all animation data for the currently running animation
        private AnimationContent anim;
        // Contains all the options that determine how the animation runs
        private AnimationOptions options;
        // This stores all of the "World" matrix parameters for an unskinned model
        private List<EffectParameter> worldParams = new List<EffectParameter>();
        // Used as a buffer for storing the poses of the current frame
        private Matrix[] bones = null;
        // Creates bone poses for the current animation
        private BonePoseCreator creator = null;

        // Required interface events
        public event EventHandler EnabledChanged;
        public event EventHandler UpdateOrderChanged;
        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of AnimationController
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="model">The model to be animated</param>
        /// <param name="animationName">Name of the animation designated in .X file.  If 
        /// name was blank in file, it will be named "Animationi," where i is the ith unnamed
        /// animation to appear in the file</param>
        /// <param name="options">A set of options that describes how the animation runs</param>
        public AnimationController(Game game, Model model,
            string animationName,
            AnimationOptions options)
        {
            this.options = options;
            this.model = model;
            ModelInfo info = (ModelInfo)model.Tag;
            anim = info.Animations[animationName];
            // Create the object that manufactures bone poses
            creator = new BonePoseCreator(model, anim,
                info.BlendTransforms,options.InterpolationMethod,
                options.PreserveBones);
            // If we are precomputing interpolations, create a bone pose table
            // with a time step proportional to the quality
            if (options.PrecomputeInterpolations)
            {
                long timeStep = game.TargetElapsedTime.Ticks / 2;
                if (options.Quality == AnimationQuality.Poor)
                    timeStep *= 2;
                else if (options.Quality == AnimationQuality.Best)
                    timeStep /= 2;
                table = new BonePoseTable(creator, timeStep);
            }
            // else createa  buffer to store the on-the-fly interpolations
            else
                bones = new Matrix[model.Bones.Count];

            game.Components.Add(this);
            
            // Find all the "World" parameters in each effect.  We only need to
            // change the world matrix in an unskinned mesh in order to animate it
            foreach (ModelMesh mesh in model.Meshes)
                foreach (Effect effect in mesh.Effects)
                    worldParams.Add(effect.Parameters["World"]);
        }
        #endregion

        #region Animation and Update Routines
        /// <summary>
        /// Resets the animation
        /// </summary>
        public void Reset()
        {
            elapsedTime = 0;
            creator.Reset();
        }

        /// <summary>
        /// Does nothing; required for GameComponent interface
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// True if the animation frames will be advanced on update/draw calls
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled && EnabledChanged != null)
                    EnabledChanged(this, new EventArgs());
                enabled = value;
            }
        }

        /// <summary>
        /// Returns the update order for the animation
        /// </summary>
        public int UpdateOrder
        {
            get { return updateOrder; }
            set
            {
                if (value != updateOrder && UpdateOrderChanged != null)
                    UpdateOrderChanged(this, new EventArgs());
                updateOrder = value;
            }
        }

        /// <summary>
        /// Returns the draw order for the animation
        /// </summary>
        public int DrawOrder
        {
            get { return drawOrder; }
            set
            {
                if (drawOrder != value && DrawOrderChanged != null)
                    DrawOrderChanged(this, new EventArgs());
                drawOrder = value;
            }
        }

        /// <summary>
        /// True if animation will be rendered
        /// </summary>
        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible != value && VisibleChanged != null)
                    VisibleChanged(this, new EventArgs());
                visible = value;
            }
        }

        /// <summary>
        /// Updates the animation frame if the interpolations are not precomputed
        /// and if we aren't using the best animation quality
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            numUpdates++;
            if (!options.PrecomputeInterpolations && options.Quality != AnimationQuality.Best)
            {
                elapsedTime = (elapsedTime + gameTime.ElapsedGameTime.Ticks)
                    % creator.TotalAnimationTicks;
                creator.AdvanceTime(gameTime.ElapsedGameTime.Ticks);

                if (options.Quality == AnimationQuality.Good || numUpdates % 2 == 0)
                    creator.CreatePoseSet(bones);
            }

        }


        /// <summary>
        /// Draws the current frame
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Do not advance frame if we are not receiving updates
            if (enabled)
            {
                // Time is advanced in draw for best precision if we have a high quality option set
                if (options.PrecomputeInterpolations || options.Quality == AnimationQuality.Best)
                    elapsedTime = (elapsedTime + gameTime.ElapsedRealTime.Ticks)
                        % creator.TotalAnimationTicks;
                // Create absolute bone pose for current frame
                if (options.PrecomputeInterpolations)
                    bones = table.GetBonePoses(elapsedTime);
                else if (options.Quality == AnimationQuality.Best)
                {
                    creator.AdvanceTime(gameTime.ElapsedRealTime.Ticks);
                    creator.CreatePoseSet(bones);
                }
            }

            // Animate the mesh by applying the absolute bone transform the current world transform
            // for each effect, drawing the mesh, and then returning the World matrix to its old state.
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix[] worlds = new Matrix[mesh.Effects.Count];
                for (int i = 0; i < mesh.Effects.Count; i++)
                {
                    worlds[i] = worldParams[index + i].GetValueMatrix();
                    worldParams[index].SetValue(worlds[i] * bones[mesh.ParentBone.Index]);
                }
                mesh.Draw();
                for (int i = 0; i < worlds.Length; i++, index++)
                    worldParams[index].SetValue(worlds[i]);
            }
        }
        #endregion
    }
}
