/*
 * AnimationController.cs
 * Copyright (c) 2007 David Astle, Michael Nikonov
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
#endregion


namespace Animation
{
    /// <summary>
    /// Animates and draws a model that was processed with AnimatedModelProcessor
    /// </summary>
    public partial class AnimationController : DrawableGameComponent
    {
        #region Member Variables
        // Stores the world transform for the animation controller.
        private Matrix world = Matrix.Identity;
        // Model to be animated
        private readonly Model model;
        // Total time elapsed since start of animation, resets when it
        // passes the total animation time
        private long elapsedTime = 0;
        // Contains all animation data for the currently running animation
        private ModelAnimation animation;
        // store this for quick access
        private long animationDuration;
        // Name of the currently running animation
        private string animationName;
        private readonly ModelAnimationCollection animations;
        // This stores all of the "World" matrix parameters for an unskinned model
        private readonly EffectParameter[] worldParams, matrixPaletteParams;
        
        // Used for storing the elapsed ticks since the last draw call
        private long elapsed;

        // Store the number of meshes in the model; gives a slight performance boost when
        // updating, and performance is everything
        private readonly int numMeshes;

        private Matrix absoluteMeshTransform;
        private Matrix[] pose;
        private Matrix[] skinTransforms;
        private BoneKeyframeCollection[] animationChannels;
        private Matrix[] palette;
        private List<string> flatSkeleton = new List<string>();
        private List<int> paletteToBoneMapping = new List<int>();

        // Multiplied by the elapsed time to give the user control over animation speed
        private double speedFactor = 1.0;
        #endregion

        #region General Properties

        /// <summary>
        /// Gets or sets the value that is multiplied by game time to update the elapsed time.
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
        /// Gets or sets the world matrix for the animation scene.
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

        public ModelAnimationCollection Animations
        {
            get
            {
                return animations;
            }
        }

        /// <summary>
        /// Gets the total length of the animation in ticks.
        /// </summary>
        public long AnimationDuration
        {
            get
            {
                return animationDuration;
            }
        }

        /// <summary>
        /// Gets the model associated with this controller.
        /// </summary>
        public Model Model
        { get { return model; } }

        #endregion

        #region Constructors

        private AnimationController(Game game, Model model) : base(game)
        {
            this.model = model;
            numMeshes = model.Meshes.Count;
            // Grab the tag that was set in the processor; this is a dictionary so that users can extend
            // the processor and pass their own data into the program without messing up the animation data
            Dictionary<string, object> modelTagData = (Dictionary<string, object>)model.Tag;
            // An AnimationLibrary processor was not used if this is null
            if (modelTagData == null)
                throw new Exception("Model contains no animation info; the tag is not an instance of " +
                    "Dictionary<string, object>.  Please use the \"Model - Animation Library\" processor or a subclass.");
            if (!modelTagData.ContainsKey("Animation"))
                throw new Exception("Model contains no animation info; please use the \"Model - Animation Library\"" +
                    " processor or a subclass.");
            // Now grab the animation info and store local references
            animations = (ModelAnimationCollection)modelTagData["Animation"];

            if (isSkinned(model))
            {
                if (model.Bones.Count > BasicPaletteEffect.PALETTE_SIZE)
                    throw new Exception("Model uses to many bones for animation.\nMax number of bones: " +
                        BasicPaletteEffect.PALETTE_SIZE.ToString() + "\nNumber of bones used: " +
                            model.Bones.Count.ToString());
            }
            // Find total number of effects used by the model
            int numEffects = 0;
            foreach (ModelMesh mesh in model.Meshes)
                foreach (Effect effect in mesh.Effects)
                    numEffects++;
            // Initialize the arrays that store effect parameters
            worldParams = new EffectParameter[numEffects];
            matrixPaletteParams = new EffectParameter[numEffects];
            // Now store the parameters in the arrays so the values they refer to can quickly be set
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                bool skinned = isSkinned(mesh);
                foreach (Effect effect in mesh.Effects)
                {
                    worldParams[index] = effect.Parameters["World"];
                    matrixPaletteParams[index] = effect.Parameters["MatrixPalette"];
                    index++;
                }
            }

            pose = new Matrix[model.Bones.Count];
            skinTransforms = new Matrix[model.Bones.Count];
            animationChannels = new BoneKeyframeCollection[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(pose);
            absoluteMeshTransform = Matrix.Identity;
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                if (isSkinned(model.Meshes[i]))
                {
                    absoluteMeshTransform = pose[model.Meshes[i].ParentBone.Index];
                    for (int j = 0; j < skinTransforms.Length; j++)
                    {
                        skinTransforms[j] = absoluteMeshTransform * Matrix.Invert(pose[j]);
                    }
                }
            }
            FlattenSkeleton(model.Root);
            palette = new Matrix[paletteToBoneMapping.Count];

            game.Components.Add(this);
        }

        private void FlattenSkeleton(ModelBone bone)
        {
            if (bone.Parent != null)
            {
                foreach (ModelMesh mesh in model.Meshes)
                {
                    if (mesh.ParentBone.Index == bone.Index)
                    {
                        if (isSkinned(mesh))
                        {
                            flatSkeleton.Clear();
                            paletteToBoneMapping.Clear();
                        }
                        return;
                    }
                }
                flatSkeleton.Add(bone.Name);
                paletteToBoneMapping.Add(bone.Index);
            }
            foreach (ModelBone child in bone.Children)
                FlattenSkeleton(child);
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
        }

        private bool isSkinned(ModelMeshPart meshPart)
        {
            VertexElement[] ves = meshPart.VertexDeclaration.GetVertexElements();
            foreach (VertexElement ve in ves)
            {
                //(BlendIndices with UsageIndex = 0) specifies matrix indices for fixed-function vertex processing using indexed paletted skinning.
                if (ve.VertexElementUsage == VertexElementUsage.BlendIndices
                    && ve.VertexElementFormat == VertexElementFormat.Byte4
                    && ve.UsageIndex == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool isSkinned(ModelMesh mesh)
        {
            foreach (ModelMeshPart mmp in mesh.MeshParts)
            {
                if (isSkinned(mmp))
                    return true;
            }
            return false;
        }

        private bool isSkinned(Model model)
        {
            foreach (ModelMesh mm in model.Meshes)
            {
                if (isSkinned(mm))
                    return true;
            }
            return false;
        }

        #endregion

        #region Animation and Update Routines

        /// <summary>
        /// Changes the current animation.
        /// </summary>
        /// <param name="animationIndex">An animation index in the .X file.</param>
        public virtual void ChangeAnimation(int animationIndex)
        {
            // check to see if the index is out of range
            if (animationIndex >= animations.Count)
                throw new Exception("Invalid animation index.");

            ChangeAnimation(animations[animationIndex].Name );
        }

        /// <summary>
        /// Changes the current animation.
        /// </summary>
        /// <param name="animationName">The name of the animation.</param>
        public virtual void ChangeAnimation(string animationName)
        {
            // Dont do anything if the name is equal to the current animation
            if (animationName == this.animationName)
                return;
            string oldAnimName = this.animationName;
            this.animationName = animationName;
            if (!animations.ContainsKey(animationName))
                throw new Exception("The specified animation, " + animationName + ", does not exist.");
            animation = animations[animationName];
            animationDuration = animation.Duration;
            elapsedTime = 0;
            for (int i = 0; i < animationChannels.Length; i++)
            {
                string boneName = model.Bones[i].Name;
                if (animation.BoneAnimations.ContainsKey(boneName))
                {
                    animationChannels[i] = animation.BoneAnimations[boneName];
                }
                else
                {
                    animationChannels[i] = null;
                }
            }
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
        }

        public override void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Draws the current frame
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Draw(GameTime gameTime)
        {
            if (Enabled)
            {
                elapsed = (long)(speedFactor * gameTime.ElapsedRealTime.Ticks);
                if (elapsed != 0)
                {
                    elapsedTime = (elapsedTime + elapsed) % animationDuration;
                    if (elapsedTime < 0)
                        elapsedTime = AnimationDuration;
                }
                for (int i = 0; i < pose.Length; i++)
                {
                    BoneKeyframeCollection channel = animationChannels[i];
                    if (channel != null)
                    {
                        long ticksPerFrame = channel.Duration / channel.Count;
                        int frameNum = (int)(elapsedTime / ticksPerFrame);
                        if (frameNum >= channel.Count)
                            frameNum = channel.Count-1;
                        while (frameNum < (channel.Count - 2)
                            && channel[frameNum+1].Time < elapsedTime)
                        {
                            ++frameNum;
                        }
                        while (frameNum > 0 && channel[frameNum].Time > elapsedTime)
                        {
                            --frameNum;
                        }
                        pose[i] = channel[frameNum].Transform;
                    }
                    else
                    {
                        pose[i] = model.Bones[i].Transform;
                    }
                    if (i > 0) // not root
                    {
                        Matrix.Multiply(ref pose[i], ref pose[model.Bones[i].Parent.Index], out pose[i]);
                    }
                }
                for (int i = 0; i < palette.Length; i++)
                {
                    int p = paletteToBoneMapping[i];
                    Matrix.Multiply(ref skinTransforms[p], ref pose[p], out palette[i]);
                }
                int index = 0;
                for (int i = 0; i < numMeshes; i++)
                {
                    ModelMesh mesh = model.Meshes[i];
                    if (matrixPaletteParams[index] != null)
                    {
                        foreach (Effect effect in mesh.Effects)
                        {
                            worldParams[index].SetValue(world);
                            matrixPaletteParams[index].SetValue(palette);
                            index++;
                        }
                    }
                    else
                    {
                        foreach (Effect effect in mesh.Effects)
                        {

                            worldParams[index].SetValue(pose[mesh.ParentBone.Index] * world);
                            index++;
                        }
                    }
                    mesh.Draw();
                }
            }
        }
        #endregion
    }
}
