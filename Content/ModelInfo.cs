/*
 *  ModelInfo.cs
 *  Data structure that stores information passed from the content pipeline to the
 *  client program
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

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework;
#endregion

namespace Animation.Content
{
    public struct ModelInfo
    {
        public AnimationContentDictionary Animations;
        // The transforms that transform vertices into bones' local space
        public Dictionary<string, Matrix> BlendTransforms;
    }
}
