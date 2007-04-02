/*
 * AnimationController.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace Xclna.Xna.Animation
{
   
    /// <summary>
    /// Used for events dealing with an animation controller.
    /// </summary>
    /// <param name="sender">The AnimationController that raised this event.</param>
    /// <param name="e">The EventArgs.</param>
    public delegate void AnimationEventHandler(object sender,
        EventArgs e);

    /// <summary>
    /// An interface used by BonePose that allows an animation to affect the bone
    /// as a function of time.
    /// </summary>
    public interface IAnimationController : IUpdateable
    {
        /// <summary>
        /// Event fired when an animation ends.
        /// </summary>
        event AnimationEventHandler AnimationEnded;
        /// <summary>
        /// Gets or sets a value indicating whether or not the animation is looping.
        /// </summary>
        bool IsLooping { get; set;}
        /// <summary>
        /// Gets the duration of the animation in ticks.
        /// </summary>
        long Duration { get;}
        /// <summary>
        /// Gets the current transform for the given BonePose object in the animation.
        /// This is only called when a bone pose is affected by the current animation.
        /// </summary>
        /// <param name="pose">The BonePose object querying for the current transform in
        /// the animation.</param>
        /// <param name="transform">The transform of the BonePose object to be set.</param>
        void GetCurrentBoneTransform(BonePose pose, out Matrix transform);
        /// <summary>
        /// Gets or sets the elapsed time in ticks for the animation.
        /// </summary>
        long ElapsedTime { get;set;}
        /// <summary>
        /// Gets or sets the value that is multiplied by the elapsed game time.
        /// </summary>
        double SpeedFactor { get;set;}
        /// <summary>
        /// Gets a value determining whether the animation can potentially affect the
        /// given BonePose.
        /// </summary>
        /// <param name="pose">The BonePose to test.</param>
        /// <returns>True if the animation can affect the bone and contains a track
        /// for it.</returns>
        bool ContainsAnimationTrack(BonePose pose);
    }
    
    /// <summary>
    /// Controls an animation by advancing it's time and affecting
    /// bone transforms
    /// </summary>
    public class AnimationController : GameComponent, IAnimationController
    {

        #region Member Variables
        // Contains the interpolated transforms for all bones in an
        // animation
        private AnimationInfo animation;
        // Multiplied by the time whenever the animation is advanced; determines
        // the playback speed of the animation
        private double speedFactor = 1.0;
        // The elapsed time in the animation, can not be greater than the
        // animation duration
        private long elapsedTime = 0;
        // Used as a buffer to store the total elapsed ticks every frame so that
        private long elapsed;


        /// <summary>
        /// Fired when the controller is not looping and the animation has ended.
        /// </summary>
        public event AnimationEventHandler AnimationEnded;
        // True fi the animation is looping
        private bool isLooping = true;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new animation controller.
        /// </summary>
        /// <param name="game">The game to which this controller will be attached.</param>
        /// <param name="sourceAnimation">The source animation that the controller will use.
        /// This is stored in the ModelAnimator class.</param>
        public AnimationController(
            Game game,
            AnimationInfo sourceAnimation) : base(game)
        {
            animation = sourceAnimation;
            // This is set so that the controller updates before the 
            // ModelAnimator by default
            base.UpdateOrder = 0;
            game.Components.Add(this);
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value that determines if the animation is looping.
        /// </summary>
        public bool IsLooping
        {
            get { return isLooping; }
            set
            {
                isLooping = value;
            }
        }

        /// <summary>
        /// Gets the total duration, in ticks, of the animation.
        /// </summary>
        public long Duration
        {
            get { return animation.Duration; }
        }


        /// <summary>
        /// Gets the source animation that this controller is using.
        /// </summary>
        public AnimationInfo AnimationSource
        {
            get { return animation; }
        }

        /// <summary>
        /// Gets or sets the elapsed time for the animation.
        /// </summary>
        public long ElapsedTime
        {
            get { return elapsedTime; }
            set
            {
                // Perform argument checking
                if (value < 0 || value > animation.Duration)
                    throw new ArgumentOutOfRangeException("ElapsedTime",
                        "When setting the ElapsedTime for an animation, the value " +
                        " must be between 0 and the animation duration.");
                elapsedTime = value;
            }
        }



        /// <summary>
        /// Gets or sets the value that is multiplied by the time when it is
        /// advanced to determine the playback speed of the animation.
        /// </summary>
        public double SpeedFactor
        {
            get { return speedFactor; }
            set { speedFactor = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when the current animation reaches the end.
        /// </summary>
        /// <param name="args">The event args.</param>
        protected virtual void OnAnimationEnded(EventArgs args)
        {
            if (AnimationEnded != null)
                AnimationEnded(this, args);
        }

        /// <summary>
        /// Advances the current time in the animation.
        /// </summary>
        /// <param name="gameTime">Contains the time by which the animation will be advanced</param>
        public override void Update(GameTime gameTime)
        {
            // Speedfactor * elapsed time since last call to update
            elapsed = (long)(speedFactor * gameTime.ElapsedGameTime.Ticks);
            // If the animation is looping
            if (isLooping)
            {
                // Don't do anything if the speedfactor=0
                if (elapsed != 0)
                {
                    elapsedTime = (elapsedTime + elapsed);
                    // If the elapsed time is greater than the duration,
                    // raise the animation ended event and restart the animation
                    if (elapsedTime > animation.Duration)
                    {
                        OnAnimationEnded(null);
                        elapsedTime %= (animation.Duration + 1);
                    }
                }
            }
            // If we aren't looping, don't do anything if the animation is at the end
            else if (elapsedTime != animation.Duration)
            {
                if (elapsed != 0)
                {
                    // Set the elapsed time to the duration if the animation ends and
                    // raise the AnimationEnded event
                    elapsedTime = elapsedTime + elapsed;
                    if (elapsedTime >= animation.Duration || elapsedTime < 0)
                    {
                        elapsedTime = animation.Duration;
                        OnAnimationEnded(null);
                    }
                }
            }
        }


        /// <summary>
        /// Gets the current transform for the given BonePose object in the animation.
        /// This is only called when a bone pose is affected by the current animation.
        /// </summary>
        /// <param name="pose">The BonePose object querying for the current transform in
        /// the animation.</param>
        /// <param name="transform">The transform of the BonePose object to be set.</param>
        public void GetCurrentBoneTransform(BonePose pose, out Matrix transform)
        {
            AnimationChannelCollection channels = animation.AnimationChannels;
            BoneKeyframeCollection channel = channels[pose.Name];
            int boneIndex = channel.GetIndexByTime(elapsedTime);
            transform = channel[boneIndex].Transform;
        }


        /// <summary>
        /// Returns true if the animation contains a track for the given BonePose.
        /// </summary>
        /// <param name="pose">The BonePose to test for track existence.</param>
        /// <returns>True if the animation contains a track for the given BonePose.</returns>
        public bool ContainsAnimationTrack(BonePose pose)
        {
            return animation.AnimationChannels.AffectsBone(pose.Name);
        }

        #endregion

    }

}
