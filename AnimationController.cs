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
using Microsoft.Xna.Framework.Content;
#endregion


namespace Animation
{
    /// <summary>
    /// A delegate used by the AnimationChanged event.
    /// </summary>
    /// <param name="sender">The animation controller whose animation changed</param>
    /// <param name="args">The AnimationChangedEventArgs</param>
    public delegate void AnimationChangedEventHandler(object sender, AnimationChangedEventArgs args);
    /// <summary>
    /// Contains arguments for when an animation changes.
    /// </summary>
    public class AnimationChangedEventArgs : EventArgs
    {
        private string oldAnimation;
        private string newAnimation;
        internal AnimationChangedEventArgs(string oldAnimation, string newAnimation)
        {
            this.oldAnimation = oldAnimation;
            this.newAnimation = newAnimation;
        }

        /// <summary>
        /// The name of the old animation.
        /// </summary>
        public string OldAnimationName
        {
            get
            {
                return oldAnimation;
            }
        }
        
        /// <summary>
        /// The name of the new animation.
        /// </summary>
        public string NewAnimationName
        {
            get
            {
                return newAnimation;
            }
        }
    }

    /// <summary>
    /// Animates and draws a model that was processed with AnimatedModelProcessor
    /// </summary>
    public partial class AnimationController : IDrawable, IUpdateable,
        IGameComponent, ICloneable
    {
        #region Member Variables

        private Matrix world = Matrix.Identity;
        // Model to be animated
        private Model model;
        // Maps bone names to their blend transform, which, when applied to a bone,
        // creates a matrix that transforms vertices in the bones local space
        // empty if there is no skinning information
        private Dictionary<string, Matrix> blendTransforms;
        // If we are precomputing interpolations, this will store all of the
        // bone poses for every frame of the animation
        private BonePoseTable table = null;
        private int updateOrder = 0;
        private int drawOrder = 0;
        private Game game;
        // Total time elapsed since start of animation, resets when it
        // passes the total animation time
        private long elapsedTime = 0;
        // The number of calls to Update(); used for Poor quality animations
        // in which we update every other call
        private int numUpdates = 0;
        private bool enabled = true;
        private bool visible = true;
        private bool usingTable = false;
        // Contains all animation data for the currently running animation
        private AnimationContent anim;
        private string animationName;
        private AnimationContentDictionary animations;
        // This stores all of the "World" matrix parameters for an unskinned model
        private List<EffectParameter> worldParams = new List<EffectParameter>();
        private Matrix[] worldBuffer;
        // Used as a buffer for storing the poses of the current frame
        private Matrix[] bones = null;
        // Creates bone poses for the current animation
        private BonePoseCreator creator = null;
        private double speedFactor = 1.0;
        private Matrix[] mats =null;
        AnimationQuality quality = AnimationQuality.Good;
        InterpolationMethod interpMethod = InterpolationMethod.Linear;
        // Required interface events
        /// <summary>
        /// Fired when the enabled property of the AnimationController changes.
        /// </summary>
        public event EventHandler EnabledChanged;
        /// <summary>
        /// Fired when the update order of the AnimationController changes.
        /// </summary>
        public event EventHandler UpdateOrderChanged;
        /// <summary>
        /// Fired when the draw order of the AnimationController changes.
        /// </summary>
        public event EventHandler DrawOrderChanged;
        /// <summary>
        /// Fired when the visible property of the AnimationController changes.
        /// </summary>
        public event EventHandler VisibleChanged;
        /// <summary>
        /// Fired when the animation resets
        /// </summary>
        public event EventHandler AnimationReset;
        /// <summary>
        /// Fired when the animation changes
        /// </summary>
        public event AnimationChangedEventHandler AnimationChanged;
 
        #endregion

        #region Constructors


        private AnimationController(Game game, Model model)
        {
            this.model = model;
            ContentManager manager = new ContentManager(game.Services);
            BasicPaletteEffect.ReplaceBasicEffects(manager, model);
            ModelAnimationInfo info = (ModelAnimationInfo)((Dictionary<string, object>)model.Tag)["ModelAnimationInfo"];

            animations = info.Animations;
            blendTransforms = info.BlendTransforms;
            this.game = game;
            bones = new Matrix[model.Bones.Count];

            // Find all the "World" parameters in each effect.  We only need to
            // change the world matrix in an unskinned mesh in order to animate it
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    worldParams.Add(effect.Parameters["World"]);
                    if (effect is BasicPaletteEffect)
                    {
                        if (mats == null)
                        {
                            mats = new Matrix[BasicPaletteEffect.PALETTE_SIZE];
                            model.CopyAbsoluteBoneTransformsTo(mats);
                        }
                        effect.Parameters["BonePalette"].SetValue(mats);
                    }
                }
            }
            worldBuffer = new Matrix[worldParams.Count];


            game.Components.Add(this);
        }
        /// <summary>
        /// Creates a new instance of AnimationController and calls BasicPaletteEffect.ReplaceBasicEffects
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="model">The model to be animated</param>
        /// <param name="animationName">Name of the animation designated in .X file.  If 
        /// name was blank in file, it will be named "Animationi," where i is the ith unnamed
        /// animation to appear in the file</param>
        public AnimationController(Game game, Model model,
            string animationName) : this(game,model)
        {
            creator = new BonePoseCreator(this);
            ChangeAnimation(animationName);
            UsePrecomputedInterpolations = true;
        }

        /// <summary>
        /// Creates a new instance of AnimationController and calls BasicPaletteEffect.ReplaceBasicEffects
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="model">The model to be animated</param>
        /// <param name="animationIndex">The index of the animation in the X file.</param>
        public AnimationController(Game game, Model model,
            int animationIndex)
            : this(game, model)
        {
            creator = new BonePoseCreator(this);
            ChangeAnimation(animationIndex);
            UsePrecomputedInterpolations = true;
        }

        /// <summary>
        /// Creates a new instance of AnimationController and calls BasicPaletteEffect.ReplaceBasicEffects
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="model">The model to be animated</param>
        /// <param name="animationIndex">The index of the animation in the X file.</param>
        /// <param name="usePrecomputedInterpolations">Whether or not to compute all interpolates for the
        /// given animation upon construction</param>
        public AnimationController(Game game, Model model,
            int animationIndex, bool usePrecomputedInterpolations)
            : this(game, model)
        {
            creator = new BonePoseCreator(this);
            ChangeAnimation(animationIndex);
            UsePrecomputedInterpolations = usePrecomputedInterpolations;
        }

        /// <summary>
        /// Creates a new instance of AnimationController and calls BasicPaletteEffect.ReplaceBasicEffects
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="model">The model to be animated</param>
        /// <param name="animationIndex">The index of the animation in the X file.</param>
        /// <param name="usePrecomputedInterpolations">Whether or not to compute all interpolates for the
        /// given animation upon construction</param>
        public AnimationController(Game game, Model model,
            string animationName, bool usePrecomputedInterpolations)
            : this(game, model)
        {
            creator = new BonePoseCreator(this);
            ChangeAnimation(animationName);
            UsePrecomputedInterpolations = usePrecomputedInterpolations;
        }
        #endregion

        #region General Properties
        /// <summary>
        /// This returns the time step (precision) used for the bone pose table
        /// if precomputer interpolations are active, divided by the SpeedFactor
        /// </summary>
        public long RawTimeStep
        {
            get
            {
                long timeStep = game.TargetElapsedTime.Ticks;
                if (quality == AnimationQuality.Best)
                    timeStep /= 2;
                else if (quality == AnimationQuality.Poor)
                    timeStep *= 2;
                return timeStep;
            }
        }

        /// <summary>
        /// This is multiplied by game time to update the elapsed time.
        /// </summary>
        public double SpeedFactor
        {
            get
            {
                return speedFactor;
            }
            set
            {
                speedFactor = value;
            }
        }

        /// <summary>
        /// Sets or gets the world matrix for the animation scene.
        /// </summary>
        public Matrix World
        {
            get
            {
                return world;
            }
            set
            {
                world = value;
            }
        }


        /// <summary>
        /// Copies the absolute transforms of the current frame to the specified array.
        /// </summary>
        public void CopyAbsoluteFrameTransformsTo(Matrix[] boneSet)
        {
            creator.CreatePoseSet(boneSet);
        }


        /// <summary>
        /// The total length of the animation in ticks.
        /// </summary>
        public long AnimationDuration
        {
            get
            {
                return anim.Duration.Ticks;
            }
        }

        /// <summary>
        /// The quality of the animation.
        /// </summary>
        public AnimationQuality Quality
        {
            get
            {
                return quality;
            }
            set
            {
                quality = value;
            }
        }

        /// <summary>
        /// The interpolation method used by the animation
        /// </summary>
        public InterpolationMethod InterpolationMethod
        {
            get
            {
                return interpMethod;
            }
            set
            {
                interpMethod = value;
            }
        }

        /// <summary>
        /// If true, then the controller will use precomputed interpolations.
        /// You must recompute interpolations after the first time this is set to true
        /// if you want to change the interpolations.
        /// </summary>
        public bool UsePrecomputedInterpolations
        {
            get
            {
                return usingTable;
            }
            set
            {
                if (value && table == null)
                    RecomputeInterpolations();
                usingTable = value;
            }
        }

        /// <summary>
        /// Returns the model associated with this controller.
        /// </summary>
        public Model Model
        { get { return model; } }

        #endregion
     
        #region Animation and Update Routines

        /// <summary>
        /// Changes the current animation.
        /// </summary>
        /// <param name="animationIndex">An animation index in the .X file.</param>
        public virtual void ChangeAnimation(int animationIndex)
        {
            int i = 0;
            string animName = null;
            if (animationIndex >= animations.Count)
                throw new Exception("Invalid animation index.");
            foreach (KeyValuePair<string, AnimationContent> k in animations)
            {
                if (i == animationIndex)
                {
                    animName = k.Key;
                    break;
                }
                i++;
            }
            ChangeAnimation(animName);

        }

        /// <summary>
        /// Changes the current animation.
        /// </summary>
        /// <param name="animationName">The name of the animation.</param>
        public virtual void ChangeAnimation(string animationName)
        {
            if (animationName == this.animationName)
                return;
            string oldAnimName = this.animationName;
            this.animationName = animationName;
            UsePrecomputedInterpolations = false;
            if (!animations.ContainsKey(animationName))
                throw new Exception("The specified animation, " + animationName + ", does not exist.");
            anim = animations[animationName];
            // Create the object that manufactures bone poses
            elapsedTime = 0;
            creator.Reset();
            creator.CreatePoseSet(bones);
            if (oldAnimName != null && AnimationChanged != null)
                AnimationChanged(this, new AnimationChangedEventArgs(oldAnimName, animationName));
        }

        /// <summary>
        /// Gets the name of the currently running animation.
        /// </summary>
        public string AnimationName
        {
            get
            {
                return animationName;
            }
        }

        /// <summary>
        /// Resets the animation
        /// </summary>
        public virtual void Reset()
        {
            elapsedTime = 0;
            creator.Reset();
            if (AnimationReset != null)
                AnimationReset(this, new EventArgs());
        }

        /// <summary>
        /// Recomputes the interpolations, but does not make the controller use the 
        /// interpolations.
        /// </summary>
        public void RecomputeInterpolations()
        {
            table = new BonePoseTable(this,
                (long)(RawTimeStep * speedFactor));
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
                bool oldVal = enabled;
                enabled = value;
                if (oldVal != enabled && EnabledChanged != null)
                    EnabledChanged(this, new EventArgs());

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
                int order = updateOrder;
                updateOrder = value;
                if (order != updateOrder && UpdateOrderChanged != null)
                    UpdateOrderChanged(this, new EventArgs());

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
                int order = drawOrder;
                drawOrder = value;
                if (order != drawOrder && DrawOrderChanged != null)
                    DrawOrderChanged(this, new EventArgs());

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
                bool oldVal = visible;
                visible = value;
                if (visible != oldVal && VisibleChanged != null)
                    VisibleChanged(this, new EventArgs());

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
            if (!usingTable && quality != AnimationQuality.Best)
            {
                elapsedTime = (elapsedTime + (long)(speedFactor * gameTime.ElapsedGameTime.Ticks))
                    % AnimationDuration;
                if (elapsedTime < 0)
                    elapsedTime = AnimationDuration;
                creator.AdvanceTime((long)(speedFactor * gameTime.ElapsedGameTime.Ticks));

                if (quality == AnimationQuality.Good || numUpdates % 2 == 0)
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
                if (usingTable || quality == AnimationQuality.Best)
                {
                    elapsedTime = (elapsedTime + (long)(speedFactor * gameTime.ElapsedRealTime.Ticks))
                        % AnimationDuration;
                    if (elapsedTime < 0)
                        elapsedTime = AnimationDuration;
                }
                // Create absolute bone pose for current frame
                if (usingTable)
                    bones = table.GetBonePoses(elapsedTime);
                else if (quality  == AnimationQuality.Best)
                {
                    creator.AdvanceTime((long)(speedFactor * gameTime.ElapsedRealTime.Ticks));
                    creator.CreatePoseSet(bones);
                }
            }

            // Animate the mesh by applying the absolute bone transform the current world transform
            // for each effect, drawing the mesh, and then returning the World matrix to its old state.
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                for (int i = 0; i < mesh.Effects.Count; i++,index++)
                {
                    worldBuffer[index] = worldParams[index].GetValueMatrix();
                    if (mesh.Effects[i] is BasicPaletteEffect)
                    {
                        mesh.Effects[i].Parameters["BonePalette"].SetValue(bones);
                        worldParams[index].SetValue(world);
                    }
                    else
                    {
                        worldParams[index].SetValue(bones[mesh.ParentBone.Index] * world);
                    }
                }
                mesh.Draw();
            }
            for (int i = 0; i < worldParams.Count; i++)
            {
                worldParams[i].SetValue(worldBuffer[i]);
            }
        }
        #endregion

        #region ICloneable Members

        public object Clone()
        {
            Animation.AnimationController controller = new AnimationController(this.game,
                model, this.animationName,false);
            controller.speedFactor = this.speedFactor;
            controller.quality = this.quality;
            controller.InterpolationMethod = this.InterpolationMethod;
            if (UsePrecomputedInterpolations)
            {
                controller.usingTable = true;
                controller.table = this.table;
            }
            return controller;
            
        }

        #endregion
    }
}
