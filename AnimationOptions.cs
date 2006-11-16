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

    /// <summary>
    /// Determines the quality of an animation
    /// </summary>
    public enum AnimationQuality
    {
        /// <summary>
        /// If interpolations are precomputed, time step is very small, else, interpolation
        /// will be performed during the draw method
        /// </summary>
        Best,
        /// <summary>
        /// If interpolations are precomputed, time step is equal to game update time, else,
        /// interpolation will be performed during the update method
        /// </summary>
        Good,
        /// <summary>
        /// If interpolations are precomputed, time step is large, else, interpolation
        /// will be performed during every other update method
        /// </summary>
        Poor
    }

    /// <summary>
    /// Contains options that determine how an animation is updated and rendered
    /// </summary>
    public struct AnimationOptions
    {
        #region Member Variables
        private InterpolationMethod interpMethod;
        // True if bone pose table is used
        private bool precomputeInterp;
        private AnimationQuality quality;
        // True if the models bones should never be changed.  Since they are used to
        // calculate combined transforms, this causes some overhead
        private bool preserveBones;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of AnimationOptions.
        /// </summary>
        /// <param name="interpMethod">The method of interpolation</param>
        /// <param name="precomputeInterpolation">True if interpolations should be precomputed</param>
        /// <param name="quality">Quality of the animation</param>
        /// <param name="preserveBones">True if the model bones should not be changed by the
        /// animation controller</param>
        public AnimationOptions(InterpolationMethod interpMethod,
            bool precomputeInterpolation,
            AnimationQuality quality,
            bool preserveBones)
        {
            this.interpMethod = interpMethod;
            this.precomputeInterp = precomputeInterpolation;
            this.quality = quality;
            this.preserveBones = preserveBones;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The quality of the animation
        /// </summary>
        public AnimationQuality Quality
        {
            get { return quality; }
            set { quality = value; }
        }

        /// <summary>
        /// The method of interpolation between animation keys
        /// </summary>
        public InterpolationMethod InterpolationMethod
        {
            get { return interpMethod; }
            set { interpMethod = value; }
        }

        /// <summary>
        /// True if interpolations are precomputed
        /// </summary>
        public bool PrecomputeInterpolations
        {
            get { return precomputeInterp; }
            set { precomputeInterp = value; }
        }

        /// <summary>
        /// True if the model's bones should never be changed
        /// </summary>
        public bool PreserveBones
        {
            get { return preserveBones; }
            set { preserveBones = value; }
        }
        #endregion
    }
}
