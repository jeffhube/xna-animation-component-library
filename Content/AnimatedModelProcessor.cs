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

namespace XCLNA.XNA.Animation.Content
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
        private List<string> bones=new List<string>();
        private Dictionary<string, int> boneIndices = new Dictionary<string, int>();
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

        /// <summary>
        /// Gets the names of the bones that should be used by the palette.
        /// </summary>
        protected ReadOnlyCollection<string> SkinnedBoneNames
        { get { return bones.AsReadOnly(); } }

        /// <summary>
        /// Gets the processor context.
        /// </summary>
        protected ContentProcessorContext ProcessorContext
        { get { return context; } }

        /// <summary>
        /// Called when an AnimationContent is processed.
        /// </summary>
        /// <param name="animation">The AnimationContent to be processed.</param>
        /// <returns>The processed AnimationContent.</returns>
        protected virtual AnimationContent
            ProcessAnimation(AnimationContent animation)
        {
            AnimationProcessor ap = new AnimationProcessor();
            AnimationContent newAnim = ap.Interpolate(animation);
            newAnim.Name = animation.Name;
            return newAnim;
; 
        }

        /// <summary>
        /// Called when an XML document is read that specifies how animations
        /// should be split.
        /// </summary>
        /// <param name="animDict">The dictionary of animation name/AnimationContent
        /// pairs. </param>
        /// <param name="doc">The Xml document that contains info on how to split
        /// the animations.</param>
        protected virtual void SubdivideAnimations(
            AnimationContentDictionary animDict, XmlDocument doc)
        {
            // Traverse each xml node that represents an animation to be subdivided
            foreach (XmlElement child in doc)
            {
                // The name of the animation to be split
                string animName = child["name"].InnerText;

                // If the tickspersecond node is filled, use that to calculate seconds per tick
                double animTicksPerSecond = 1.0, secondsPerTick = 0;
                if (child["tickspersecond"] != null)
                {
                    animTicksPerSecond = double.Parse(child["tickspersecond"].InnerText);
                }
                secondsPerTick = 1.0 / animTicksPerSecond;

                // Check to see if the animation specified in the xml file exists
                if (animDict.ContainsKey(animName))
                {
                    // Get the animation and remove it from the dict
                    AnimationContent anim = animDict[animName];
                    animDict.Remove(animName);
                    // Get the list of new animations
                    XmlNodeList subAnimations = child.GetElementsByTagName("animationsubset");

                    foreach (XmlElement subAnim in subAnimations)
                    {
                        // Create the new sub animation
                        AnimationContent newAnim = new AnimationContent();
                        XmlElement subAnimNameElement = subAnim["name"];

                        if (subAnimNameElement != null)
                            newAnim.Name = subAnimNameElement.InnerText;

                        // If a starttime node exists, use that to get the start time
                        long startTime, endTime;
                        if (subAnim["starttime"] != null)
                        {

                            startTime = TimeSpan.FromSeconds(double.Parse(subAnim["starttime"].InnerText)).Ticks;
                        }
                        else // else use the secondspertick combined with the startframe node value
                        {
                            double seconds = 
                                double.Parse(subAnim["startframe"].InnerText) * secondsPerTick;

                            startTime = TimeSpan.FromSeconds(
                                seconds).Ticks;
                        }

                        // Same with endtime/endframe
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

                        // Now that we have the start and end times, we associate them with
                        // start and end indices for each animation track/channel
                        foreach (KeyValuePair<string, AnimationChannel> k in anim.Channels)
                        {
                            // The current difference between the start time and the
                            // time at the current index
                            long currentStartDiff;
                            // The current difference between the end time and the
                            // time at the current index
                            long currentEndDiff;
                            // The difference between the start time and the time
                            // at the start index
                            long bestStartDiff=long.MaxValue;
                            // The difference between the end time and the time at
                            // the end index
                            long bestEndDiff=long.MaxValue;

                            // The start and end indices
                            int startIndex = -1;
                            int endIndex = -1;

                            // Create a new channel and reference the old channel
                            AnimationChannel newChan = new AnimationChannel();
                            AnimationChannel oldChan = k.Value;

                            // Iterate through the keyframes in the channel
                            for (int i = 0; i < oldChan.Count; i++)
                            {
                                // Update the startIndex, endIndex, bestStartDiff,
                                // and bestEndDiff
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


                            // Now we have our start and end index for the channel
                            for (int i = startIndex; i <= endIndex; i++)
                            {
                                AnimationKeyframe frame = oldChan[i];
                                long time;
                                // Clamp the time so that it can't be less than the
                                // start time
                                if (frame.Time.Ticks < startTime)
                                    time = 0;
                                // Clamp the time so that it can't be greater than the
                                // end time
                                else if (frame.Time.Ticks > endTime)
                                    time = endTime - startTime;
                                else // Else get the time
                                    time = frame.Time.Ticks - startTime;

                                // Finally... create the new keyframe and add it to the new channel
                                AnimationKeyframe keyframe = new AnimationKeyframe(
                                    TimeSpan.FromTicks(time),
                                    frame.Transform);
                                
                                newChan.Add(keyframe);
                            }
                            
                            // Add the channel and update the animation duration based on the
                            // length of the animation track.
                            newAnim.Channels.Add(k.Key, newChan);
                            if (newChan[newChan.Count - 1].Time > newAnim.Duration)
                                newAnim.Duration = newChan[newChan.Count - 1].Time;


                        }
                        // Add the subdived animation to the dictionary.
                        animDict.Add(newAnim.Name, newAnim);
                    }
                }
            }
        }

        // Reads the XML document associated with the model if it exists.
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

        // Flattens the skeleton so that for all bone indices i,
        // any index < i is a parent and any index > i is a child of bones[i]
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

        /// <summary>
        /// Called when a basic effect is encountered and potentially replaced by
        /// BasicPaletteEffect (if not overridden).
        /// </summary>
        /// <param name="skinningType">The the skinning type of the meshpart.</param>
        /// <param name="meshPart">The MeshPart that contains the BasicMaterialContent.</param>
        protected virtual void ReplaceBasicEffect(SkinningType skinningType,
            ModelMeshPartContent meshPart)
        {
            BasicMaterialContent basic = meshPart.Material as BasicMaterialContent;
            if (basic != null)
            {
                // Create a new PaletteSourceCode object and set its palette size
                // based on the platform since xbox has fewer registers.
                PaletteSourceCode source;
                if (context.TargetPlatform != TargetPlatform.Xbox360)
                {
                    source = new PaletteSourceCode(56);
                }
                else
                {
                    source = new PaletteSourceCode(40);
                }
                // Process the material and set the meshPart material to the new
                // material.
                PaletteInfoProcessor processor = new PaletteInfoProcessor();
                meshPart.Material = processor.Process(
                    new PaletteInfo(source.SourceCode4BonesPerVertex,
                    source.PALETTE_SIZE, basic), context);
            }
        }

        // Go through the modelmeshes and replace all basic effects for skinned models
        // with BasicPaletteEffect.
        private void ReplaceBasicEffects(ModelMeshContent input)
        {
            foreach (ModelMeshPartContent part in input.MeshParts)
            {
                SkinningType skinType = ContentUtil.GetSkinningType(part.GetVertexDeclaration());
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
        /// <param name="node">The root of the tree</param>
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
        
        /// <summary>
        /// Go through the vertex channels in the geometry and replace the 
        /// BoneWeightCollection objects with weight and index channels.
        /// </summary>
        /// <param name="geometry">The geometry to process.</param>
        /// <param name="vertexChannelIndex">The index of the vertex channel to process.</param>
        /// <param name="context">The processor context.</param>
        protected override void ProcessVertexChannel(GeometryContent geometry, int vertexChannelIndex, ContentProcessorContext context)
        {
            if (geometry.Vertices.Channels[vertexChannelIndex].Name == VertexChannelNames.Weights())
            {
                // Skin channels are passed in from importers as BoneWeightCollection objects
                VertexChannel<BoneWeightCollection> vc = 
                    (VertexChannel<BoneWeightCollection>)
                    geometry.Vertices.Channels[vertexChannelIndex];
                int maxBonesPerVertex = 0;
                for (int i = 0; i < vc.Count; i++)
                {
                    int count = vc[i].Count;
                    if (count > maxBonesPerVertex)
                        maxBonesPerVertex = count;
                }

                // Add weights as colors (Converts well to 4 floats)
                // and indices as packed 4byte vectors.
                Color[] weightsToAdd = new Color[vc.Count];
                Byte4[] indicesToAdd = new Byte4[vc.Count];

                // Go through the BoneWeightCollections and create a new
                // weightsToAdd and indicesToAdd array for each BoneWeightCollection.
                for (int i = 0; i < vc.Count; i++)
                {
                    
                    BoneWeightCollection bwc = vc[i];
                    bwc.NormalizeWeights(4);
                    int count = bwc.Count;
                    if (count>maxBonesPerVertex)
                        maxBonesPerVertex = count;

                    // Add the appropriate bone indices based on the bone names in the
                    // BoneWeightCollection
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
                // Remove the old BoneWeightCollection channel
                geometry.Vertices.Channels.Remove(vc);
                // Add the new channels
                geometry.Vertices.Channels.Add<Byte4>(VertexElementUsage.BlendIndices.ToString(), indicesToAdd);
                geometry.Vertices.Channels.Add<Color>(VertexElementUsage.BlendWeight.ToString(), weightsToAdd);
            }
            else
            {
                // No skinning info, so we let the base class process the channel
                base.ProcessVertexChannel(geometry, vertexChannelIndex, context);
            }
        }

        /// <summary>
        /// Returns the index of the matrix palette used for the given bone name.
        /// </summary>
        /// <param name="boneName">The name of the bone to which the index will refer.</param>
        /// <returns>The index of the bone in the matrix palette.</returns>
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


