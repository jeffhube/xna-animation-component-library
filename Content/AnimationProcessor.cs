/*
 * AnimationProcessor.cs
 * Copyright (c) 2007 Michael Nikonov
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

namespace Animation.Content
{
    /// <summary>
    /// Produces AnimationContentDictionary;
    /// warns of incompatibilities of model and skeleton.
    /// </summary>
    [ContentProcessor(DisplayName = "Animation - Animation Library")]
    public class AnimationProcessor : ContentProcessor<BoneContent,AnimationContentDictionary>
    {
        protected ContentProcessorContext context;
        protected BoneContent inputSkeleton;
        protected NodeContent model;
        protected BoneContent modelSkeleton;


        /// <summary>
        /// Produces ModelAnimationInfo from skeleton and animations.
        /// </summary>
        /// <param name="input">skeleton</param>
        /// <param name="context">The context for this processor</param>
        /// <returns>AnimationContentDictionary</returns>
        public override AnimationContentDictionary Process(BoneContent input, ContentProcessorContext context)
        {
            inputSkeleton = input;
            inputSkeleton.Identity.FragmentIdentifier = "";
            this.context = context;

            string modelFilePath = GetModelFilePath(inputSkeleton.Identity);
            if (modelFilePath != null)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "animation will be checked against model " + modelFilePath);
                ExternalReference<NodeContent> er = new ExternalReference<NodeContent>(modelFilePath);
                model = (NodeContent)context.BuildAndLoadAsset<NodeContent, Object>(er, "PassThroughProcessor");
                modelSkeleton = MeshHelper.FindSkeleton(model);
                checkBones(modelSkeleton, inputSkeleton);
            }
            else
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "corresponding model not found");
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "animation filename should follow the <modelName>_<animationName>.<ext> pattern to get animation skeleton checked against model");
            }

            AnimationContentDictionary animations = Interpolate(input.Animations);
            return animations;
        }

        protected virtual string GetModelFilePath(ContentIdentity animationId)
        {
            string dir = Path.GetDirectoryName(animationId.SourceFilename);
            string animName = Path.GetFileNameWithoutExtension(animationId.SourceFilename);
            if (animName.Contains("_"))
            {
                string modelName = animName.Split('_')[0];
                return Path.GetFullPath(dir + @"\" + modelName + ".fbx");
            }
            else
            {
                return null;
            }
        }

        protected virtual void checkBones(BoneContent modelBone, BoneContent skeletonBone)
        {
            if (modelBone.Name != skeletonBone.Name)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity, 
                "model bone " + modelBone.Name
                    + " does not match skeletone bone " + skeletonBone.Name);
            }
            if (modelBone.Children.Count != skeletonBone.Children.Count)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity, 
                    "model bone " + modelBone.Name + " has " + modelBone.Children.Count
                    + " children but corresponding skeletone bone has " + skeletonBone.Children.Count + " children");
            }
            float diff = modelBone.Transform.Translation.Length() - skeletonBone.Transform.Translation.Length();
            if (diff*diff > 0.0001f)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "model bone " + modelBone.Name + " translation "
                    + "(Lenght=" + modelBone.Transform.Translation.Length() + ")"
                    +" does not match translation of skeletone bone "
                    + skeletonBone.Name
                    + " (Lenght=" + skeletonBone.Transform.Translation.Length() + ")"
                    );
            }
            Dictionary<string, BoneContent> modelBoneDict = new Dictionary<string, BoneContent>();
            foreach (BoneContent mb in modelBone.Children)
            {
                modelBoneDict.Add(mb.Name, mb);
            }
            foreach (BoneContent sb in skeletonBone.Children)
            {
                if (modelBoneDict.ContainsKey(sb.Name))
                {
                    BoneContent mb = modelBoneDict[sb.Name];
                    checkBones(mb, sb);
                }
                else
                {
                    context.Logger.LogWarning("", inputSkeleton.Identity,
                        "skeleton bone " + sb.Name + " was not found in model");
                }
            }
        }
        public virtual AnimationContentDictionary Interpolate(AnimationContentDictionary input)
        {
            AnimationContentDictionary output = new AnimationContentDictionary();
            foreach (string name in input.Keys)
            {
                output.Add(name, Interpolate(input[name]));
            }
            return output;
        }

        // Interpolates an AnimationContent object to 60 fps
        public virtual AnimationContent Interpolate(AnimationContent input)
        {
            AnimationContent output = new AnimationContent();
            long time = 0;
            long animationDuration = input.Duration.Ticks;
            foreach (KeyValuePair<string, AnimationChannel> c in input.Channels)
            {
                time = 0;
                string channelName = c.Key;
                AnimationChannel channel = c.Value;
                AnimationChannel outChannel = new AnimationChannel();
                int currentFrame = 0;
                while (time < animationDuration)
                {
                    if (time > animationDuration)
                        time = animationDuration;
                    AnimationKeyframe keyframe;
                    if (channel.Count == 1 || time < channel[0].Time.Ticks)
                    {
                        keyframe = new AnimationKeyframe(new TimeSpan(time), channel[0].Transform);
                    }
                    else if (channel[channel.Count - 1].Time.Ticks < time)
                    {
                        keyframe = new AnimationKeyframe(new TimeSpan(time), channel[channel.Count - 1].Transform);
                    }
                    else
                    {
                        while (channel[currentFrame + 1].Time.Ticks < time)
                        {
                            currentFrame++;
                        }
                        double interpNumerator = (double)(time - channel[currentFrame].Time.Ticks);
                        double interpDenom = (double)(channel[currentFrame + 1].Time.Ticks - channel[currentFrame].Time.Ticks);
                        double interpAmount = interpNumerator / interpDenom;
                        if (channel[currentFrame].Transform != channel[currentFrame + 1].Transform)
                        {
                            keyframe = new AnimationKeyframe(new TimeSpan(time),
                                Util.SlerpMatrix(
                                channel[currentFrame].Transform,
                                channel[currentFrame + 1].Transform,
                                interpAmount));
                        }
                        else
                        {
                            keyframe = new AnimationKeyframe(new TimeSpan(time),
                                channel[currentFrame].Transform);
                        }
                    }
                    outChannel.Add(keyframe);
                    time += Util.TICKS_PER_60FPS;
                }
                output.Channels.Add(channelName, outChannel);

            }
            output.Duration = input.Duration;
            return output;

        }



    }
}


