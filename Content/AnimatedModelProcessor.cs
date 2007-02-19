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
using System.Xml;
using System.Collections.ObjectModel;

namespace Animation.Content
{
    /// <summary>
    /// Processes a NodeContent object that was imported by SkinnedModelImporter
    /// and attaches animation data to its tag
    /// </summary>
    [ContentProcessor(DisplayName="Model - Animation Library")]
    public class AnimatedModelProcessor : ModelProcessor
    {

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
            this.input = input;
            this.context = context;

            // Get the process model minus the animation data
            ModelContent c = base.Process(input, context);
            // Attach the animation and skinning data to the models tag
            FindAnimations(input);
            Dictionary<string, object> dict = new Dictionary<string, object>();
            XmlDocument xmlDoc = ReadAnimationXML(input);
            if (xmlDoc != null)
            {
                SubdivideAnimations(animations, xmlDoc);
            }
            dict.Add("SkinnedBones", bones.ToArray());
            AnimationContentDictionary processedAnims
                = new AnimationContentDictionary();
            foreach (AnimationContent anim in animations.Values)
            {
                AnimationContent processedAnim = ProcessAnimation(anim);
                processedAnims.Add(processedAnim.Name, processedAnim);

            }
            dict.Add("Animations", processedAnims);

            foreach (ModelMeshContent meshContent in c.Meshes)
                ReplaceBasicEffects(meshContent);
            c.Tag = dict;

            return c;
        }
        protected ReadOnlyCollection<string> SkinnedBoneNames
        { get { return bones.AsReadOnly(); } }
        protected ContentProcessorContext ProcessorContext
        { get { return context; } }
        protected virtual AnimationContent
            ProcessAnimation(AnimationContent animation)
        {
            AnimationProcessor ap = new AnimationProcessor();
            AnimationContent newAnim = ap.Interpolate(animation);
            newAnim.Name = animation.Name;
            return newAnim;
; 
        }

        protected virtual void SubdivideAnimations(
            AnimationContentDictionary animDict, XmlDocument doc)
        {

            foreach (XmlElement child in doc)
            {
                string animName = child["name"].InnerText;

                double animTicksPerSecond = 1.0, secondsPerTick = 0;
                if (child["tickspersecond"] != null)
                {
                    animTicksPerSecond = double.Parse(child["tickspersecond"].InnerText);
                }
                secondsPerTick = 1.0 / animTicksPerSecond;
                if (animDict.ContainsKey(animName))
                {

                    AnimationContent anim = animDict[animName];
                    animDict.Remove(animName);
                    XmlNodeList subAnimations = child.GetElementsByTagName("animationsubset");
                    foreach (XmlElement subAnim in subAnimations)
                    {
                        AnimationContent newAnim = new AnimationContent();
                        XmlElement subAnimNameElement = subAnim["name"];

                        if (subAnimNameElement != null)
                            newAnim.Name = subAnimNameElement.InnerText;

                        long startTime, endTime;
                        if (subAnim["starttime"] != null)
                        {

                            startTime = TimeSpan.FromSeconds(double.Parse(subAnim["starttime"].InnerText)).Ticks;
                        }
                        else
                        {
                            double seconds = 
                                double.Parse(subAnim["startframe"].InnerText) * secondsPerTick;

                            startTime = TimeSpan.FromSeconds(
                                seconds).Ticks;
                        }
                        if (subAnim["endtime"] != null)
                        {
                            endTime = TimeSpan.FromSeconds(double.Parse(subAnim["endtime"].InnerText)).Ticks;
                        }
                        else
                        {
                            double seconds = double.Parse(subAnim["endframe"].InnerText)
                                * secondsPerTick;
                            endTime = TimeSpan.FromSeconds(
                                seconds).Ticks;
                        }

                        foreach (KeyValuePair<string, AnimationChannel> k in anim.Channels)
                        {
                            long currentStartDiff, currentEndDiff, 
                                bestStartDiff=long.MaxValue, bestEndDiff=long.MaxValue;
                            int startIndex = -1;
                            int endIndex = -1;
                            AnimationChannel newChan = new AnimationChannel();
                            AnimationChannel oldChan = k.Value;

                            for (int i = 0; i < oldChan.Count; i++)
                            {
                                long ticks = oldChan[i].Time.Ticks;
                                currentStartDiff = Math.Abs(startTime - ticks);
                                currentEndDiff = Math.Abs(endTime - ticks);
                                if (startIndex == -1 || currentStartDiff<bestStartDiff)
                                {
                                    startIndex = i;
                                    bestStartDiff = currentStartDiff;
                                }


                                if (endIndex == -1 || currentEndDiff<bestEndDiff)
                                {
                                    endIndex = i;
                                    bestEndDiff = currentEndDiff;
                                }


                            }


                            
                            for (int i = startIndex; i <= endIndex; i++)
                            {
                                AnimationKeyframe frame = oldChan[i];
                                long time;
                                if (frame.Time.Ticks < startTime)
                                    time = 0;
                                else if (frame.Time.Ticks > endTime)
                                    time = endTime - startTime;
                                else
                                    time = frame.Time.Ticks - startTime;
                                AnimationKeyframe keyframe = new AnimationKeyframe(
                                    TimeSpan.FromTicks(time),
                                    frame.Transform);
                                
                                newChan.Add(keyframe);
                            }
                            
                            
                            newAnim.Channels.Add(k.Key, newChan);
                            if (newChan[newChan.Count - 1].Time > newAnim.Duration)
                                newAnim.Duration = newChan[newChan.Count - 1].Time;


                        }
        
                        animDict.Add(newAnim.Name, newAnim);


                    }

                }
            }
        }


        private XmlDocument ReadAnimationXML(NodeContent root)
        {
            XmlDocument doc = null;
            string filePath = Path.GetFullPath(root.Identity.SourceFilename);
            string fileName = Path.GetFileName(filePath);
            fileName = Path.GetDirectoryName(filePath);
            if (fileName!="")
                fileName += "\\";
            fileName += System.IO.Path.GetFileNameWithoutExtension(filePath)
                + "animation.xml";
            bool animXMLExists = File.Exists(fileName);
            if (animXMLExists)
            {
                doc = new XmlDocument();
                doc.Load(fileName);
            }
            return doc;
        }

        private void FlattenSkeleton(NodeContent node)
        {
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

        protected virtual void ReplaceBasicEffect(SkinningType skinningType,
            ModelMeshPartContent meshPart)
        {
            BasicMaterialContent basic = meshPart.Material as BasicMaterialContent;
            if (basic != null)
            {
                PaletteSourceCode source;
                if (context.TargetPlatform != TargetPlatform.Xbox360)
                {
                    source = new PaletteSourceCode(56);
                }
                else
                {
                    source = new PaletteSourceCode(40);
                }
                PaletteInfoProcessor processor = new PaletteInfoProcessor();
                meshPart.Material = processor.Process(
                    new PaletteInfo(source.SourceCode4BonesPerVertex,
                    source.PALETTE_SIZE, basic), context);
            }
        }

        private void ReplaceBasicEffects(ModelMeshContent input)
        {
            foreach (ModelMeshPartContent part in input.MeshParts)
            {
                SkinningType skinType = ContentUtil.CheckSkinningType(part.GetVertexDeclaration());
                if (skinType != SkinningType.None)
                {
                    ReplaceBasicEffect(skinType, part);

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
                VertexChannel<BoneWeightCollection> vc = 
                    (VertexChannel<BoneWeightCollection>)geometry.Vertices.Channels[vertexChannelIndex];
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


