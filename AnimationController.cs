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

#define GI
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

        // This stores all of the "World" matrix parameters for an unskinned model
        private readonly EffectParameter[] worldParams, matrixPaletteParams;

        private BoneAnimationCollection animatedBones;

        private SortedList<string, RunningAnimation> runningAnimations =
            new SortedList<string, RunningAnimation>();
        private SortedList<string, RunningAnimation> runningBlendAnimations =
            new SortedList<string, RunningAnimation>();

        private ModelAnimationCollection animations;

        // Store the number of meshes in the model
        private readonly int numMeshes;

        private Matrix absoluteMeshTransform;
        private Matrix[] pose;
        private List<Matrix[]> skinTransforms=new List<Matrix[]>();
        private Matrix[] palette;
        private string[] skinnedBones;
        private int[] paletteToBoneMapping;

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

        public ModelAnimationCollection Animations
        {
            get
            {
                return animations;
            }
        }


        /// <summary>
        /// Gets the model associated with this controller.
        /// </summary>
        public Model Model
        { get { return model; } }

        #endregion

        #region Constructors

        public AnimationController(Game game, Model model) : base(game)
        {
            this.model = model;
            animatedBones = BoneAnimationCollection.FromModelBoneCollection(
                model.Bones);
            numMeshes = model.Meshes.Count;
            CheckForInvalidData();

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
            absoluteMeshTransform = Matrix.Identity;

            InitializeSkinningInfo();

            game.Components.Add(this);

        }




        private void InitializeSkinningInfo()
        {
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                skinTransforms.Add(null);
                if (isSkinned(model.Meshes[i]))
                {
                    absoluteMeshTransform = pose[model.Meshes[i].ParentBone.Index];
                    skinTransforms[i] = new Matrix[model.Bones.Count];
                    for (int j = 0; j < skinTransforms[i].Length; j++)
                    {
                        skinTransforms[i][j] = absoluteMeshTransform * Matrix.Invert(pose[j]);
                    }
                }
            }
            paletteToBoneMapping = new int[skinnedBones.Length];
            for (int i = 0; i < skinnedBones.Length; i++)
            {
                paletteToBoneMapping[i] = model.Bones[skinnedBones[i]].Index;
            }
            palette = new Matrix[paletteToBoneMapping.Length];
        }

        private void CheckForInvalidData()
        {
            // Grab the tag that was set in the processor; this is a dictionary so that users can extend
            // the processor and pass their own data into the program without messing up the animation data
            Dictionary<string, object> modelTagData = (Dictionary<string, object>)model.Tag;
            // An AnimationLibrary processor was not used if this is null
            if (modelTagData == null)
                throw new Exception("Model contains no animation info; the tag is not an instance of " +
                    "Dictionary<string, object>.  Please use the \"Model - Animation Library\" processor or a subclass.");
            if (!modelTagData.ContainsKey("Animations"))
                throw new Exception("Model contains no animation info; please use the \"Model - Animation Library\"" +
                    " processor or a subclass.");
            // Now grab the animation info and store local references
            animations = (ModelAnimationCollection)modelTagData["Animations"];
            skinnedBones = (string[])modelTagData["SkinnedBones"];

            if (isSkinned(model))
            {
                if (skinnedBones.Length > BasicPaletteEffect.PALETTE_SIZE)
                    throw new Exception("Model uses too many bones for animation.\nMax number of bones: " +
                        BasicPaletteEffect.PALETTE_SIZE.ToString() + "\nNumber of bones used: " +
                            model.Bones.Count.ToString());
            }
        }

        private void InitializeEffectParams()
        {

            // Now store the parameters in the arrays so the values they refer to can quickly be set
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes)
            {
                bool skinned = isSkinned(mesh);
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




        public override void Update(GameTime gameTime)
        {
        }

        public void GetCurrentTransforms(Matrix[] pose)
        {
            for (int i = 0; i < pose.Length; i++)
            {
                if (i > 0) // not root
                {
                    pose[i] = animatedBones[i].CurrentTransform *
                        pose[animatedBones[i].Parent.Index];
                }
                else
                {
                    pose[i] = animatedBones[i].CurrentTransform;
                }
            }
        }

        public void RunAnimation(string animName, bool blendAnimation)
        {
            RunningAnimation runningAnim;
            if (blendAnimation)
            {
                if (runningBlendAnimations.ContainsKey(animName))
                {
                    runningAnim = runningBlendAnimations[animName];
                    runningAnim.RemoveAllBones();
                    runningBlendAnimations.Remove(animName);
                }
                runningBlendAnimations.Add(animName,
                    new RunningAnimation(animations[animName], animatedBones,true));
            }
            else
            {
                if (runningAnimations.ContainsKey(animName))
                {
                    runningAnim = runningAnimations[animName];
                    runningAnim.RemoveAllBones();
                    runningAnimations.Remove(animName);
                }
                runningAnimations.Add(animName,
                    new RunningAnimation(animations[animName], animatedBones, false));
            }


        }

        public void RunAnimation(string animName, IList<string> affectedBones, bool blendAnimation)
        {
            RunningAnimation runningAnim;
            if (blendAnimation)
            {
                if (runningBlendAnimations.ContainsKey(animName))
                {
                    runningAnim = runningBlendAnimations[animName];
                    runningAnim.RemoveAllBones();
                    runningBlendAnimations.Remove(animName);
                }
                runningBlendAnimations.Add(animName,
                    new RunningAnimation(animations[animName], animatedBones, affectedBones,true));
            }
            else
            {
                if (runningAnimations.ContainsKey(animName))
                {
                    runningAnim = runningAnimations[animName];
                    runningAnim.RemoveAllBones();
                    runningAnimations.Remove(animName);
                }
                runningAnimations.Add(animName,
                    new RunningAnimation(animations[animName], animatedBones, affectedBones,false));
            }
        }

        public void RunAnimation(int animationIndex, bool blendAnimation)
        {
            RunAnimation(animations[animationIndex].Name, blendAnimation);
        }

        public void RunAnimation(int animationIndex, IList<string> affectedBones,
            bool blendAnimation)
        {
            RunAnimation(animations[animationIndex].Name, affectedBones,blendAnimation);
        }

        public void StopAnimation(string animName)
        {
            runningAnimations[animName].RemoveAllBones();
            runningAnimations.Remove(animName);

        }

        public IList<RunningAnimation> RunningAnimations
        {
            get
            {
                return runningAnimations.Values;
            }
        }

        public IList<RunningAnimation> BlendAnimations
        {
            get
            {
                return runningBlendAnimations.Values;
            }
        }

        public RunningAnimation GetRunningAnimation(string animName)
        {
            return runningAnimations[animName];
        }
        public RunningAnimation GetBlendAnimation(string animName)
        {
            return runningBlendAnimations[animName];
        }


        


        public BoneAnimationCollection BoneAnimations
        {
            get { return animatedBones; }
        }


        /// <summary>
        /// Draws the current frame
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public override void Draw(GameTime gameTime)
        {
            if (Enabled)
            {
                foreach (RunningAnimation anim in runningAnimations.Values)
                {
                    anim.AdvanceTime(gameTime);
                }
                foreach (RunningAnimation anim in runningBlendAnimations.Values)
                {
                    anim.AdvanceTime(gameTime);
                }
                GetCurrentTransforms(pose);
                int index = 0;
                for (int i = 0; i < numMeshes; i++)
                {
                    ModelMesh mesh = model.Meshes[i];
                    if (matrixPaletteParams[index] != null)
                    {
                        for (int j = 0; j < palette.Length; j++)
                        {
                            int p = paletteToBoneMapping[j];
                            Matrix.Multiply(ref skinTransforms[i][p], ref pose[p], out palette[j]);
                        }
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
