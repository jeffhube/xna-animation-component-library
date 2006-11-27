/*
 * AnimationOptions.cs
 * Contains options that determine how an animation is updated and rendered
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
using System.Text;
#endregion

namespace Animation
{
    /// <summary>
    /// Determines how an animation is interpolated
    /// </summary>
    public enum InterpolationMethod
    {
        /// <summary>
        /// Linear interpolation between matrices
        /// </summary>
        Linear,
        /// <summary>
        /// Decompose matrices into scale, translation, and rotation components,
        /// linearly interpolate scale and translation, and perform spherical
        /// linear interpolation on rotation components
        /// </summary>
        SphericalLinear
    }
 
    


}
