/*
 * AnimatedModelProcessor.cs
 * Copyright (c) 2006 David Astle
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Collections;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content;
using System.Globalization;

namespace Animation.Content
{
    /// <summary>
    /// Processes a NodeContent object that was imported by SkinnedModelImporter
    /// and attaches animation data to its tag
    /// </summary>
    [ContentProcessor(DisplayName="Model - Animation Library")]
    public class AnimatedModelProcessor : ModelProcessor
    {

        // Stores the byte code for the BasicPaletteEffect
        internal static byte[] paletteByteCode4Bones = null;
        internal static byte[] paletteByteCode8Bones = null;
        private static bool paletteLoadAttempted = false;
        private ContentProcessorContext context;
        // stores all animations for the model
        private AnimationContentDictionary animations = new AnimationContentDictionary();
        /// <summary>Processes a SkinnedModelImporter NodeContent root</summary>
        /// <param name="input">The root of the X file tree</param>
        /// <param name="context">The context for this processor</param>
        /// <returns>A model with animation data on its tag</returns>
        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            if (!paletteLoadAttempted)
            {
                EffectContent effect = new EffectContent();
                effect.EffectCode = BasicPaletteEffect.SourceCode4BonesPerVertex;
                EffectProcessor processor = new EffectProcessor();
                CompiledEffect compiledEffect4 = processor.Process(effect, context);
                effect = new EffectContent();
                effect.EffectCode = BasicPaletteEffect.SourceCode8BonesPerVertex;
                processor = new EffectProcessor();
                CompiledEffect compiledEffect8 = processor.Process(effect, context);

                if (compiledEffect4.Success != false && compiledEffect8.Success != false)
                {
                    paletteByteCode4Bones = compiledEffect4.GetEffectCode();
                    paletteByteCode8Bones = compiledEffect8.GetEffectCode();
                }
                else
                {
                    context.Logger.LogWarning("",
                        new ContentIdentity(),
                        "Compilation of BasicPaletteEffect failed.");
                }
                paletteLoadAttempted = true;
            }


            this.context = context;
            // Get the process model minus the animation data
            ModelContent c = base.Process(input, context);
            // Attach the animation and skinning data to the models tag

            FindAnimations(input);

            List<SkinTransform[]> skinTransforms;

            // If we used default importer we-can't do any skinning
            if (input.OpaqueData.ContainsKey("SkinTransforms"))
                skinTransforms = (List<SkinTransform[]>)input.OpaqueData["SkinTransforms"];
            else
                skinTransforms = new List<SkinTransform[]>(new SkinTransform[c.Meshes.Count][]);

            ModelAnimationInfoContent info = new ModelAnimationInfoContent(
                c,
                animations,
                skinTransforms);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("ModelAnimationInfo", info);
            

            foreach (ModelMeshContent meshContent in c.Meshes)
                ReplaceBasicEffects(meshContent);
            c.Tag = dict;



            
            return c;
        }


        private void ReplaceBasicEffects(ModelMeshContent input)
        {
            foreach (ModelMeshPartContent part in input.MeshParts)
            {
                SkinningType skinType = Util.CheckSkinningType(part.GetVertexDeclaration());
                if (skinType != SkinningType.None)
                {

                    BasicMaterialContent basic = part.Material as BasicMaterialContent;
                    if (basic != null)
                    {
                        PaletteMaterialContent paletteContent;
                        if (skinType == SkinningType.FourBonesPerVertex)
                        {
                            paletteContent = new PaletteMaterialContent(basic, paletteByteCode4Bones,
                                context);
                        }
                        else
                        {
                            paletteContent = new PaletteMaterialContent(basic, paletteByteCode8Bones,
                                context);
                        }
                        part.Material = paletteContent;
                    }
                }
            }

        }


        /// <summary>
        /// Searches through the NodeContent tree for all animations and puts them in
        /// one AnimationContentDictionary
        /// </summary>
        /// <param name="root">The root of the tree</param>
        private void FindAnimations(NodeContent root)
        {
            foreach (KeyValuePair<string, AnimationContent> k in root.Animations)
                if (!animations.ContainsKey(k.Key))
                    animations.Add(k.Key, k.Value);
                else if (!animations.Values.Contains(k.Value))
                    animations.Add("Animation" + animations.Count.ToString(), k.Value);
            foreach (NodeContent child in root.Children)
                FindAnimations(child);

        }
    }



}


