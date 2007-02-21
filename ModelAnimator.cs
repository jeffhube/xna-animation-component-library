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

namespace XCLNA.XNA.Animation
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

        private BonePoseCollection bonePoses;

        private AnimationInfoCollection animations;

        // Store the number of meshes in the model
        private readonly int numMeshes;

        private Matrix[] pose;
        private Matrix[] palette;
        private SkinInfoCollection skinInfo;

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

        public AnimationInfoCollection Animations
        { get { return animations; } }

        #endregion

        #region Constructors

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

            skinInfo = new SkinInfoCollection(model);
            palette = new Matrix[skinInfo.Count];
            base.UpdateOrder = 1;
            game.Components.Add(this);


        }




        public void InitializeEffectParams()
        {

            // Now store the parameters in the arrays so the values they refer to can quickly be set
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
                    index++;
                }
            }
        }


        #endregion

        #region Animation and Update Routines



        private static Matrix skinTransform;
        public override void Update(GameTime gameTime)
        {
            bonePoses.CopyAbsoluteTransformsTo(pose);
            foreach (SkinInfo info in skinInfo)
            {
                skinTransform = info.Transform;
                Matrix.Multiply(ref skinTransform, ref pose[info.BoneIndex],
                    out palette[info.PaletteIndex]);
            }
        }



        public void CopyAbsoluteTransformsTo(Matrix[] transforms)
        {
            pose.CopyTo(transforms, 0);
        }

        public Matrix GetAbsoluteTransform(int boneIndex)
        {
            return pose[boneIndex];
        }





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
        #endregion
    }
}
