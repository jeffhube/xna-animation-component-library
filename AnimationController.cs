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

namespace XCLNA.XNA.Animation
{
    


    /// <summary>
    /// Used for events dealing with an animation controller.
    /// </summary>
    /// <param name="sender">The AnimationController that raised this event.</param>
    public delegate void AnimationEventHandler(object sender,
        EventArgs e);

    /// <summary>
    /// An interface used by BonePose that allows an animation to affect the bone
    /// as a function of time.
    /// </summary>
    public interface IAnimationController : IUpdateable
    {
        event AnimationEventHandler AnimationEnded;
        bool IsLooping { get; set;}
        long Duration { get;}
        void GetCurrentBoneTransform(BonePose pose, out Matrix transform);
        long ElapsedTime { get;set;}
        double SpeedFactor { get;set;}
        bool ContainsAnimationTrack(BonePose pose);
    }
    
    /// <summary>
    /// Controls an animation by advancing it's time and affecting
    /// bone transforms
    /// </summary>
    public class AnimationController : GameComponent, IAnimationController

    {
        // Contains the interpolated transforms for all bones in an
        // animation
        private AnimationInfo animation;
        // Multiplied by the time whenever the animation is advanced; determines
        // the playback speed of the animation
        private double speedFactor = 1.0;
        // The elapsed time in the animation, can not be greater than the
        // animation duration
        private long elapsedTime = 0;

        int boneIndex;

        // Used as a buffer to store the total elapsed ticks every frame so that
        // a new long chunk doesn't have to be allocated every frame by every
        // controller
        private long elapsed;


        /// <summary>
        /// Fired when the controller is not looping and the animation has ended.
        /// </summary>
        public event AnimationEventHandler AnimationEnded;
        private bool isLooping = true;

        /// <summary>
        /// Creates a new animation controller.
        /// </summary>
        /// <param name="sourceAnimation">The source animation that the controller will use.
        /// This is stored in the ModelAnimator class.</param>
        /// <param name="animator">The ModelAnimator that will use this controller.</param>
        public AnimationController(
            Game game,
            AnimationInfo sourceAnimation) : base(game)
        {
            animation = sourceAnimation;
            base.UpdateOrder = 0;
            game.Components.Add(this);



        }

        protected virtual void OnAnimationEnded(EventArgs args)
        {
            if (AnimationEnded != null)
                AnimationEnded(this, args);
        }

        public override void Update(GameTime gameTime)
        {
            elapsed = (long)(speedFactor * gameTime.ElapsedGameTime.Ticks);
            if (isLooping)
            {
                if (elapsed != 0)
                {
                    elapsedTime = (elapsedTime + elapsed);
                    if (elapsedTime > animation.Duration)
                    {
                        OnAnimationEnded(null);
                        elapsedTime %= (animation.Duration + 1);
                    }
                }
            }
            else if (elapsedTime != animation.Duration)
            {
                if (elapsed != 0)
                {
                    elapsedTime = elapsedTime + elapsed;
                    if (elapsedTime >= animation.Duration || elapsedTime < 0)
                    {
                        elapsedTime = animation.Duration;
                        OnAnimationEnded(null);
                    }
                }
            }
        }

        public bool IsLooping
        {
            get { return isLooping; }
            set 
            {
                isLooping = value;           
            }
        }

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
        /// Gets or sets the elapsed time for the animation affected by the
        /// controller.
        /// </summary>
        public long ElapsedTime
        {
            get { return elapsedTime; }
            set
            {
                if (value < 0)
                    value = animation.Duration - Math.Abs(value);
                elapsedTime = value % (animation.Duration+1);
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

        public void GetCurrentBoneTransform(BonePose pose, out Matrix transform)
        {
            BoneKeyframeCollection channel = animation.AnimationChannels[pose.Name];
            boneIndex = channel.GetIndexByTime(elapsedTime);
            transform = channel[boneIndex].Transform;
        }



        public bool ContainsAnimationTrack(BonePose pose)
        {
            return animation.AnimationChannels.AffectedBones.Contains(
                pose.Name);
        }

    }

}
