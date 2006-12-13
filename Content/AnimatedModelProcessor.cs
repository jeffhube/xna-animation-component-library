/*
 * AnimatedModelProcessor.cs
 * Processes NodeContent root into a model and attaches animation data to its tag.
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
            ModelAnimationInfo info = new ModelAnimationInfo();
            FindAnimations(input);
            info.Animations = animations;
            // If we used default importer we can't do any skinning
            if (input.OpaqueData.ContainsKey("SkinTransforms"))
                info.SkinTransforms = (List<SkinTransform[]>)input.OpaqueData["SkinTransforms"];
            else
                info.SkinTransforms = new List<SkinTransform[]>(new SkinTransform[c.Meshes.Count][]);
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
                SkinningType skinType = Util.CheckSkinned(part.GetVertexDeclaration());
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


