/*
 * ModelAnimationInfo.cs
 * Data structure that stores information passed from the content pipeline to the
 * client program
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

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework;
#endregion

namespace Animation.Content
{
    /// <summary>
    /// Contains animation info for a model.
    /// </summary>
    public struct ModelAnimationInfo
    {
        /// <summary>
        /// The set of animations for a model.
        /// </summary>
        public AnimationContentDictionary Animations;
        /// <summary>
        /// The transforms that transform vertices into bone's local space
        /// </summary>
        public Dictionary<string, Matrix> BlendTransforms;
    }
}
