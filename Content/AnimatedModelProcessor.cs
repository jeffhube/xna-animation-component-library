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
using Microsoft.Xna.Framework.Graphics.PackedVector;
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
        internal static byte[] paletteByteCode12Bones = null;
        private static bool paletteLoadAttempted = false;
        private ContentProcessorContext context;
        // stores all animations for the model
        private AnimationContentDictionary animations = new AnimationContentDictionary();
        private NodeContent input;
        protected List<string> bones=new List<string>();
        protected Dictionary<string, int> boneIndices=new Dictionary<string,int>();
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

                effect = new EffectContent();
                effect.EffectCode = BasicPaletteEffect.SourceCode12BonesPerVertex;
                processor = new EffectProcessor();
                CompiledEffect compiledEffect12 = processor.Process(effect, context);

                if (compiledEffect4.Success != false && compiledEffect8.Success != false)
                {
                    paletteByteCode4Bones = compiledEffect4.GetEffectCode();
                    paletteByteCode8Bones = compiledEffect8.GetEffectCode();
                    paletteByteCode12Bones = compiledEffect12.GetEffectCode();
                }
                else
                {
                    context.Logger.LogWarning("",
                        new ContentIdentity(),
                        "Compilation of BasicPaletteEffect failed.");
                }
                paletteLoadAttempted = true;
            }

            this.input = input;
            this.context = context;

            //if (input.Identity.SourceFilename.Contains("tiny"))
            //    System.Diagnostics.Debugger.Launch();
            //FlattenSkeleton(input);
            // Get the process model minus the animation data
            ModelContent c = base.Process(input, context);
            // Attach the animation and skinning data to the models tag
            FindAnimations(input);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("Animations", animations);
            dict.Add("SkinnedBones", bones.ToArray());            
            foreach (ModelMeshContent meshContent in c.Meshes)
                ReplaceBasicEffects(meshContent);
            c.Tag = dict;
            return c;
        }

        private void FlattenSkeleton(NodeContent node)
        {
            /*IList<BoneContent> flatSkeleton = MeshHelper.FlattenSkeleton(MeshHelper.FindSkeleton(input));
            for (int i = 0; i < flatSkeleton.Count; i++)
            {
                bones.Add(flatSkeleton[i].Name);
                boneIndices.Add(flatSkeleton[i].Name, i);
            }*/
            string name = node.Name;
            if (name == "" || name == null)
                name = "noname" + bones.Count;
            boneIndices.Add(name, bones.Count);
            bones.Add(name);
            foreach (NodeContent child in node.Children)
            {
                FlattenSkeleton(child);
            }
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
                        PaletteMaterialContent paletteContent = null;
                        if (skinType == SkinningType.FourBonesPerVertex)
                        {
                            paletteContent = new PaletteMaterialContent(basic, paletteByteCode4Bones,
                                context);
                        }
                        else if (skinType == SkinningType.EightBonesPerVertex)
                        {
                            paletteContent = new PaletteMaterialContent(basic, paletteByteCode8Bones,
                                context);
                        }
                        else
                        {
                            paletteContent = new PaletteMaterialContent(basic, paletteByteCode12Bones,
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
        private void FindAnimations(NodeContent node)
        {
            //if (node is BoneContent)
            {
                foreach (KeyValuePair<string, AnimationContent> k in node.Animations)
                {
                    if (animations.ContainsKey(k.Key))
                    {
                        foreach (KeyValuePair<string, AnimationChannel> c in k.Value.Channels)
                        {
                            animations[k.Key].Channels.Add(c.Key, c.Value);
                        }
                    }
                    else
                    {
                        animations.Add(k.Key, k.Value);
                    }
                }
            }
            foreach (NodeContent child in node.Children)
                FindAnimations(child);
        }
        
        protected override void ProcessVertexChannel(GeometryContent geometry, int vertexChannelIndex, ContentProcessorContext context)
        {
            if (geometry.Vertices.Channels[vertexChannelIndex].Name == VertexChannelNames.Weights())
            {
                VertexChannel<BoneWeightCollection> vc = (VertexChannel<BoneWeightCollection>)geometry.Vertices.Channels[vertexChannelIndex];
                int maxBonesPerVertex = 0;
                for (int i = 0; i < vc.Count; i++)
                {
                    int count = vc[i].Count;
                    if (count > maxBonesPerVertex)
                        maxBonesPerVertex = count;
                }
                Color[] weightsToAdd = new Color[vc.Count];
                Byte4[] indicesToAdd = new Byte4[vc.Count];
                for (int i = 0; i < vc.Count; i++)
                {
                    BoneWeightCollection bwc = vc[i];
                    bwc.NormalizeWeights(4);
                    int count = bwc.Count;
                    if (count>maxBonesPerVertex)
                        maxBonesPerVertex = count;
                    Vector4 bi = new Vector4();
                    bi.X = count > 0 ? BoneIndex(bwc[0].BoneName) : (byte)0;
                    bi.Y = count > 1 ? BoneIndex(bwc[1].BoneName) : (byte)0;
                    bi.Z = count > 2 ? BoneIndex(bwc[2].BoneName) : (byte)0;
                    bi.W = count > 3 ? BoneIndex(bwc[3].BoneName) : (byte)0;
                    indicesToAdd[i] = new Byte4(bi);
                    Vector4 bw = new Vector4();
                    bw.X = count > 0 ? bwc[0].Weight : 0;
                    bw.Y = count > 1 ? bwc[1].Weight : 0;
                    bw.Z = count > 2 ? bwc[2].Weight : 0;
                    bw.W = count > 3 ? bwc[3].Weight : 0;
                    weightsToAdd[i] = new Color(bw);
                }
                geometry.Vertices.Channels.Remove(vc);
                geometry.Vertices.Channels.Add<Byte4>(VertexElementUsage.BlendIndices.ToString(), indicesToAdd);
                geometry.Vertices.Channels.Add<Color>(VertexElementUsage.BlendWeight.ToString(), weightsToAdd);
            }
            else
            {
                base.ProcessVertexChannel(geometry, vertexChannelIndex, context);
            }
        }

        protected byte BoneIndex(string boneName)
        {
            if (boneIndices.ContainsKey(boneName))
            {
                return (byte)boneIndices[boneName];
            }
            else
            {
                boneIndices.Add(boneName, bones.Count);
                bones.Add(boneName);
                return (byte)boneIndices[boneName];
            }
        }
    }
}


