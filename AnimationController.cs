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
        private readonly Model model;
        // Maps bone names to their blend transform, which, when applied to a bone,
        // creates a matrix that transforms vertices in the bones local space
        // empty if there is no skinning information
        private readonly List<SkinTransform[]> skinTransforms;
        // If we are precomputing interpolations, this will store all of the
        // bone poses for every frame of the animation
        private InterpolationTableCollection tables = null;
        private int updateOrder = 0;
        private int drawOrder = 0;
        private readonly Game game;
        // Total time elapsed since start of animation, resets when it
        // passes the total animation time
        private long elapsedTime = 0;
        // The number of calls to Update(); used for Poor quality animations
        // in which we update every other call
        private bool enabled = true;
        private bool visible = true;
        private bool usingTable = false;
        // Contains all animation data for the currently running animation
        private AnimationContent anim;
        private bool isClone = false;
        // store this for quick access
        private long animationDuration;
        private string animationName;
        private readonly AnimationContentDictionary animations;
        // This stores all of the "World" matrix parameters for an unskinned model
        private readonly EffectParameter[] worldParams, matrixPaletteParams;
        
        // Used for storing the elapsed ticks since the last draw call
        private static long elapsed;
        private static int frameNum;

        // Used as a buffer for storing the poses of the current frame
        private readonly Matrix[] bones = null;
        private readonly Matrix[][] interpolationBuffers;
        // Creates bone poses for the current animation
        private readonly BonePoseCreator creator = null;
        // Multiplied by the elapsed time to give the user control over animation speed
        private double speedFactor = 1.0;
        InterpolationMethod interpMethod = InterpolationMethod.SphericalLinear;
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

        private AnimationController(AnimationController source)
        {
            model = source.model;
            speedFactor = source.speedFactor;
            creator = new BonePoseCreator(model, source.interpMethod);
            interpMethod = source.InterpolationMethod;
            if (source.UsePrecomputedInterpolations)
            {
                usingTable = true;
                tables = source.tables;
            }
            else
                usingTable = false;
            bones = source.bones;
            worldParams = source.worldParams;
            matrixPaletteParams = source.matrixPaletteParams;
            drawOrder = source.drawOrder;
            updateOrder = source.updateOrder;
            speedFactor = source.speedFactor;

            creator = new BonePoseCreator(model, interpMethod);
            enabled = source.enabled;
            visible = source.visible;
            animations = source.animations;
            bones = source.bones;
            animationName = source.animationName;
            anim = source.anim;
            game = source.game;
            animationDuration = source.animationDuration;
            elapsedTime = source.elapsedTime;
            skinTransforms = source.skinTransforms;
            this.interpolationBuffers = source.interpolationBuffers;
            isClone = true;
            game.Components.Add(this);

        }

        private AnimationController(Game game, Model model)
        {
            this.model = model;
            ContentManager manager = new ContentManager(game.Services);
            Dictionary<string, object> modelTagData = (Dictionary<string, object>)model.Tag;
            if (modelTagData == null)
                throw new Exception("Model contains no animation info; the tag is not an instance of " +
                    "Dictionary<string, object>.  Please use the \"Model - Animation Library\" processor or a subclass.");
            if (!modelTagData.ContainsKey("ModelAnimationInfo"))
                throw new Exception("Model contains no animation info; please use the \"Model - Animation Library\"" +
                    " processor or a subclass.");
            ModelAnimationInfo info = (ModelAnimationInfo)modelTagData["ModelAnimationInfo"];

            animations = info.Animations;
            skinTransforms = info.SkinTransforms;
            this.game = game;
            bones = new Matrix[model.Bones.Count];
            interpolationBuffers = new Matrix[model.Meshes.Count][];
            for (int i = 0; i < interpolationBuffers.Length; i++)
            {
                if (skinTransforms[i] == null)
                    interpolationBuffers[i] = new Matrix[1];
                else
                    interpolationBuffers[i] = new Matrix[skinTransforms[i].Length];
            }
            int numEffects = 0;
            foreach (ModelMesh mesh in model.Meshes)
                foreach (Effect effect in mesh.Effects)
                    numEffects++;
            worldParams = new EffectParameter[numEffects];
            matrixPaletteParams = new EffectParameter[numEffects];
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    worldParams[index] = effect.Parameters["World"];
                    matrixPaletteParams[index] = effect.Parameters["MatrixPalette"];
                    index++;
                }
            }
            creator = new BonePoseCreator(model, interpMethod);
            tables = new InterpolationTableCollection(creator,skinTransforms);

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
            ChangeAnimation(animationIndex);
            UsePrecomputedInterpolations = usePrecomputedInterpolations;
        }

        /// <summary>
        /// Creates a new instance of AnimationController and calls BasicPaletteEffect.ReplaceBasicEffects
        /// </summary>
        /// <param name="game">The current game</param>
        /// <param name="model">The model to be animated</param>
        /// <param name="animationName">The name of the animation to start</param>
        /// <param name="usePrecomputedInterpolations">Whether or not to compute all interpolates for the
        /// given animation upon construction</param>
        public AnimationController(Game game, Model model,
            string animationName, bool usePrecomputedInterpolations)
            : this(game, model)
        {
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
                return game.TargetElapsedTime.Ticks;

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
        /// The total length of the animation in ticks.
        /// </summary>
        public long AnimationDuration
        {
            get
            {
                return animationDuration;
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
                creator.InterpolationMethod = value;
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
                if (value && !tables.TablesCreated)
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
            if (!animations.ContainsKey(animationName))
                throw new Exception("The specified animation, " + animationName + ", does not exist.");
            anim = animations[animationName];
            animationDuration = anim.Duration.Ticks;
            // Create the object that manufactures bone poses
            elapsedTime = 0;
            creator.Animation = anim;
            creator.CreateModelPoseSet(bones);
            if (oldAnimName != null && AnimationChanged != null)
                AnimationChanged(this, new AnimationChangedEventArgs(oldAnimName, animationName));
            if (UsePrecomputedInterpolations)
                RecomputeInterpolations();
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
            if (isClone)
            {
                tables = new InterpolationTableCollection(creator,
                    skinTransforms);
                isClone = false;
            }
            tables.CreateTables((long)(RawTimeStep * speedFactor));
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

        }



        /// <summary>
        /// Draws the current frame
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {

            if (enabled)
            {
                elapsed = (long)(speedFactor * gameTime.ElapsedRealTime.Ticks);
                if (elapsed != 0)
                {
                    elapsedTime = (elapsedTime + elapsed) % animationDuration;
                    if (elapsedTime < 0)
                        elapsedTime = AnimationDuration;
                }

                if (usingTable)
                {
                    frameNum = tables.TimeStep==0 ? 0 : (int)(elapsedTime / tables.TimeStep);
                    if (frameNum >= tables.NumFrames)
                        frameNum--;
                    int index = 0;
                    for (int i = 0; i < model.Meshes.Count; i++)
                    {
                        ModelMesh mesh = model.Meshes[i];
                        bool skinned = skinTransforms[i] != null;
                        Matrix[] pose = tables.GetMeshPose(ref i, ref frameNum);
                        foreach (Effect effect in mesh.Effects)
                        {
                            if (skinned)
                            {
                                worldParams[index].SetValue(world);
                                matrixPaletteParams[index].SetValue(pose);
                            }
                            else
                                worldParams[index].SetValue( pose[0] * world);
                            index++;
                        }
                        mesh.Draw();
                    }

                }
                else
                {
                    creator.CreateModelPoseSet(bones);
                    int index = 0;
                    for (int i = 0; i < model.Meshes.Count; i++)
                    {
                        bool skinned = skinTransforms[i] != null;
                        creator.CreateMeshPoseSet(interpolationBuffers[i], bones,
                            skinTransforms[i], model.Meshes[i].ParentBone.Index);
                        foreach (Effect effect in model.Meshes[i].Effects)
                        {
                            if (skinned)
                            {
                                worldParams[index].SetValue(world);
                                matrixPaletteParams[index].SetValue(interpolationBuffers[i]);
                            }
                            else
                                worldParams[index].SetValue(interpolationBuffers[i][0] * world);
                            index++;
                        }
                        model.Meshes[i].Draw();
                    }
                    creator.AdvanceTime(elapsed);
                }
            }

     

        }
        #endregion

        #region ICloneable Members

        /// <summary>
        /// Makes a shallow copy of the AnimationController.  Recommended for multiple animations
        /// that share the same effect.
        /// </summary>
        /// <returns>A shallow copy of the current instance</returns>
        public object Clone()
        {
            Animation.AnimationController controller = new AnimationController(this);
            return controller;
            
        }

        #endregion
    }
}
