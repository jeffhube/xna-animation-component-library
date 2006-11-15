/*
 *  AnimatedModelProcessor.cs
 *  Processes NodeContent root into a model and attaches animation data to its tag.
 *  Copyright (C) 2006  XNA Animation Component Library CodePlex Project
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework;

namespace Animation.Content
{
    /// <summary>
    /// Processes a NodeContent object that was imported by SkinnedModelImporter
    /// and attaches animation data to its tag
    /// </summary>
    [ContentProcessor]
    public class AnimatedModelProcessor : ModelProcessor
    {
        // stores all animations for the model
        private AnimationContentDictionary animations = new AnimationContentDictionary();

        /// <summary>Processes a SkinnedModelImporter NodeContent root</summary>
        /// <param name="input">The root of the X file tree</param>
        /// <param name="context">The context for this processor</param>
        /// <returns>A model with animation data on its tag</returns>
        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            // Get the process model minus the animation data
            ModelContent c = base.Process(input, context);
            
            // Attach the animation and skinning data to the models tag
            ModelInfo info = new ModelInfo();
            FindAnimations(input);
            info.Animations = animations;
            // If we used default importer we can't do any skinning
            if (input.OpaqueData.ContainsKey("BlendTransforms"))
                info.BlendTransforms = (Dictionary<string, Matrix>)input.OpaqueData["BlendTransforms"];
            else
                info.BlendTransforms = new Dictionary<string, Matrix>();
            c.Tag = info;

            return c;
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
