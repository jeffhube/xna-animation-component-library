/*
 * ModelAnimator.cs
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

#define GI
#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
#endregion

namespace Xclna.Xna.Animation
{

    /// <summary>
    /// Animates and draws a model that was processed with AnimatedModelProcessor
    /// </summary>
    public partial class ModelAnimator : DrawableGameComponent
    {


        #region Member Variables
        // Stores the world transform for the animation controller.
        private Matrix world = Matrix.Identity;

        // Model to be animated
        private readonly Model model;

        // This stores all of the "World" matrix parameters for an unskinned model
        private readonly EffectParameter[] worldParams, matrixPaletteParams;
        // Skeletal structure containg transforms
        private BonePoseCollection bonePoses;

        private AnimationInfoCollection animations;

        // List of attached objects
        private IList<IAttachable> attachedObjects = new List<IAttachable>();

        // Store the number of meshes in the model
        private readonly int numMeshes;

        // Used to avoid reallocation
        private static Matrix skinTransform;
        // Buffer for storing absolute bone transforms
        private Matrix[] pose;
        // Array used for the matrix palette
        private Matrix[][] palette;
        // Inverse reference pose transforms
        private SkinInfoCollection[] skinInfo;

        #endregion

        #region General Properties


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


        /// <summary>
        /// Gets the model associated with this controller.
        /// </summary>
        public Model Model
        { get { return model; } }

        /// <summary>
        /// Gets the animations that were loaded in from the content pipeline
        /// for this model.
        /// </summary>
        public AnimationInfoCollection Animations
        { get { return animations; } }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of ModelAnimator.
        /// </summary>
        /// <param name="game">The game to which this component will belong.</param>
        /// <param name="model">The model to be animated.</param>
        public ModelAnimator(Game game, Model model) : base(game)
        {
            this.model = model;
            animations = AnimationInfoCollection.FromModel(model);
            bonePoses = BonePoseCollection.FromModelBoneCollection(
                model.Bones);
            numMeshes = model.Meshes.Count;
            // Find total number of effects used by the model
            int numEffects = 0;
            foreach (ModelMesh mesh in model.Meshes)
                foreach (Effect effect in mesh.Effects)
                    numEffects++;
            // Initialize the arrays that store effect parameters
            worldParams = new EffectParameter[numEffects];
            matrixPaletteParams = new EffectParameter[numEffects];
            InitializeEffectParams();

            pose = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(pose);
            // Get all the skinning info for the model
            Dictionary<string, object> modelTagInfo = (Dictionary<string, object>)model.Tag;
            if (modelTagInfo == null)
                throw new Exception("Model Processor must subclass AnimatedModelProcessor.");
            skinInfo = (SkinInfoCollection[])modelTagInfo["SkinInfo"];
            if (skinInfo == null)
                throw new Exception("Model processor must pass skinning info through the tag.");

            palette = new Matrix[model.Meshes.Count][];
            for (int i = 0; i < skinInfo.Length; i++)
            {
                if (Util.IsSkinned(model.Meshes[i]))
                    palette[i] = new Matrix[skinInfo[i].Count];
                else
                    palette[i] = null;
            }
            // Update after AnimationController by default
            base.UpdateOrder = 1;
            game.Components.Add(this);
   
            // Test to see if model has too many bones
            for (int i = 0; i < model.Meshes.Count; i++ )
            {
                if (palette[i] != null && matrixPaletteParams[i] != null)
                {
                    Matrix[] meshPalette = palette[i];
                    try
                    {
                        matrixPaletteParams[i].SetValue(meshPalette);
                    }
                    catch
                    {
                        throw new Exception("Model has too many skinned bones for the matrix palette.");
                    }
                }
            }
        }

        /// <summary>
        /// Returns skinning information for a mesh.
        /// </summary>
        /// <param name="index">The index of the mesh.</param>
        /// <returns>Skinning information for the mesh.</returns>
        public SkinInfoCollection GetMeshSkinInfo(int index)
        {
            return skinInfo[index];
        }

        /// <summary>
        /// Initializes the effect parameters.  Should be called after the effects
        /// on the model are changed.
        /// </summary>
        public void InitializeEffectParams()
        {

            // store the parameters in the arrays so the values they refer to can quickly be set
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                bool skinned = Util.IsSkinned(mesh);
                foreach (Effect effect in mesh.Effects)
                {
                    worldParams[index] = effect.Parameters["World"];
                    if (skinned)
                    {
                        matrixPaletteParams[index] = effect.Parameters["MatrixPalette"];
                    }
                    else
                        matrixPaletteParams[index]=null;
                    index++;
                }
            }
        }


        #endregion

        #region Animation and Update Routines


        /// <summary>
        /// Updates the animator by finding the current absolute transforms.
        /// </summary>
        /// <param name="gameTime">The GameTime.</param>
        public override void Update(GameTime gameTime)
        {
            bonePoses.CopyAbsoluteTransformsTo(pose);
            for (int i = 0; i < skinInfo.Length; i ++) 
            {
                if (palette[i] == null)
                    continue;
                SkinInfoCollection infoCollection = skinInfo[i];
                foreach (SkinInfo info in infoCollection)
                {
                    skinTransform = info.InverseBindPoseTransform;
                    Matrix.Multiply(ref skinTransform, ref pose[info.BoneIndex],
                       out palette[i][info.PaletteIndex]);
                }
            }

            foreach (IAttachable attached in attachedObjects)
            {
                attached.CombinedTransform = attached.LocalTransform *
                    pose[attached.AttachedBone.Index] * world;
            }

        }


        /// <summary>
        /// Copies the current absolute transforms to the specified array.
        /// </summary>
        /// <param name="transforms">The array to which the transforms will be copied.</param>
        public void CopyAbsoluteTransformsTo(Matrix[] transforms)
        {
            pose.CopyTo(transforms, 0);
        }

        /// <summary>
        /// Gets the current absolute transform for the given bone index.
        /// </summary>
        /// <param name="boneIndex"></param>
        /// <returns>The current absolute transform for the bone index.</returns>
        public Matrix GetAbsoluteTransform(int boneIndex)
        {
            return pose[boneIndex];
        }


        /// <summary>
        /// Gets a list of objects that are attached to a bone in the model.
        /// </summary>
        public IList<IAttachable> AttachedObjects
        {
            get { return attachedObjects; }
        }

        /// <summary>
        /// Gets the BonePoses associated with this ModelAnimator.
        /// </summary>
        public BonePoseCollection BonePoses
        {
            get { return bonePoses; }
        }

        /// <summary>
        /// Draws the current frame
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Draw(GameTime gameTime)
        {
            try
            {
                int index = 0;
                // Update all the effects with the palette and world and draw the meshes
                for (int i = 0; i < numMeshes; i++)
                {
                    ModelMesh mesh = model.Meshes[i];
                    if (matrixPaletteParams[index] != null)
                    {
                        foreach (Effect effect in mesh.Effects)
                        {
                   
                                worldParams[index].SetValue(
               
                                    world);
                            

                            matrixPaletteParams[index].SetValue(palette[i]);
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
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("The effects on the model for a " +
                    "ModelAnimator were changed without calling ModelAnimator.InitializeEffectParams().");
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException("ModelAnimator has thrown an InvalidCastException.  This is " +
                    "likely because the model uses too many bones for the matrix palette.  The default palette size "
                    + "is 56 for windows and 40 for Xbox.");
            }
            
        }
        #endregion
    }
}