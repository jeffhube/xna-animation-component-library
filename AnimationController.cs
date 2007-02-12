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

namespace Animation
{
    /// <summary>
    /// Used for when an animation controller affects a new bone or does not
    /// affect a bone that it used to affect.
    /// </summary>
    /// <param name="sender">The controller that has gained a new bone to affect
    /// or lost an old bone that was affected.</param>
    /// <param name="pose">The bone for which the event refers.</param>
    public delegate void BonePoseEventHandler(AnimationController sender,
    BonePose pose);

    /// <summary>
    /// Used for events dealing with an animation controller.
    /// </summary>
    /// <param name="sender">The AnimationController that fired this event.</param>
    public delegate void AnimationEventHandler(AnimationController sender);


    /// <summary>
    /// Controls an animation by advancing it's time and affecting
    /// bone transforms
    /// </summary>
    public class AnimationController : GameComponent

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
        // Stores the frame index that is most likely to be the current
        // animation index for any bone affected by the animation.  This is
        // used internally by the BonePose class to find the true current
        // frame in the animation.
        private int defaultFrameNum;
        // Used as a buffer to store the total elapsed ticks every frame so that
        // a new long chunk doesn't have to be allocated every frame by every
        // controller
        private long elapsed;
        // Contains the bones affected by the animation that the controller
        // is currently moderating
        private AffectedBoneCollection affectedBonePoses, affectedBlendBonePoses;

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
            affectedBonePoses = new AffectedBoneCollection(this);
            affectedBlendBonePoses = new AffectedBoneCollection(this);
            game.Components.Add(this);
        }



        /// <summary>
        /// Gets the bone poses currently affected by this controller.
        /// </summary>
        public AffectedBoneCollection AffectedBones
        {
            get { return affectedBonePoses; }
        }

        public AffectedBoneCollection AffectedBlendBones
        {
            get { return affectedBlendBonePoses; }
        }



        public override void Update(GameTime gameTime)
        {
            elapsed = (long)(speedFactor * gameTime.ElapsedGameTime.Ticks);
            if (isLooping)
            {
                if (elapsed != 0)
                {
                    elapsedTime = (elapsedTime + elapsed) % animation.Duration;
                    if (elapsedTime < 0)
                        elapsedTime = animation.Duration;
                }
                defaultFrameNum = (int)(elapsedTime / Util.TICKS_PER_60FPS);
            }
            else if (elapsedTime != animation.Duration)
            {
                if (elapsed != 0)
                {
                    elapsedTime = elapsedTime + elapsed;
                    if (elapsedTime >= animation.Duration || elapsedTime < 0)
                    {
                        elapsedTime = animation.Duration;
                        if (AnimationEnded != null)
                            AnimationEnded(this);
                    }
                }
                defaultFrameNum = animation.MaxNumFrames - 1;
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


        // see private member variable comment for more info
        internal int DefaultFrameNum
        {
            get { return defaultFrameNum; }
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
                elapsedTime = value % animation.Duration;
                defaultFrameNum = (int)(elapsedTime / Util.TICKS_PER_60FPS);
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


        /// <summary>
        /// A collection of bone poses affected by an animation controller.
        /// </summary>
        public class AffectedBoneCollection : ICollection<BonePose>
        {
            // Most of the stuff in this class is pretty standard, but it 
            // had to be implemented via an interface because I wanted to
            // fire an event whenever a bone is added or removed.
            private AnimationController anim;
            private List<BonePose> bones = new List<BonePose>();
            private Dictionary<string, BonePose> boneDict 
                = new Dictionary<string, BonePose>();
            /// <summary>
            /// Fired when the controller affects a new bone.
            /// </summary>
            public event BonePoseEventHandler BoneAdded;
            /// <summary>
            /// Fired when the controller no longer affects a bone that it used to
            /// affect.
            /// </summary>
            public event BonePoseEventHandler BoneRemoved;
            internal AffectedBoneCollection(AnimationController anim)
            {
                this.anim = anim;
            }


            private void OnBoneAdded(BonePose addedBone)
            {

                bones.Add(addedBone);
                if (addedBone.Name != null)
                {
                    boneDict.Add(addedBone.Name, addedBone);
                }
                if (BoneAdded != null)
                    BoneAdded(anim, addedBone);

            }
            private void OnBoneRemoved(BonePose removedBone)
            {
                bones.Remove(removedBone);
                if (removedBone.Name != null)
                {
                    boneDict.Remove(removedBone.Name);
                }
                if (BoneRemoved != null)
                    BoneRemoved(anim, removedBone);
            }


            internal void InternalAdd(BonePose item)
            {
                OnBoneAdded(item);
            }



            internal void InternalAddRange(ICollection<BonePose> bones)
            {
                foreach (BonePose a in bones)
                    InternalAdd(a);
            }


            internal void InternalClear()
            {

                while (bones.Count > 0)
                {
                    OnBoneRemoved(bones[0]);
                }
            }




            public void CopyTo(BonePose[] array, int arrayIndex)
            {
                bones.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return bones.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }


            internal bool InternalRemove(BonePose item)
            {
                if (bones.Contains(item))
                {
                    OnBoneRemoved(item);
                    return true;
                }
                return false;
            }

   




            public IEnumerator<BonePose> GetEnumerator()
            {
                return bones.GetEnumerator();
            }



            void ICollection<BonePose>.Add(BonePose item)
            {
                throw new NotSupportedException("Collection is read-only.");
            }

            void ICollection<BonePose>.Clear()
            {
                throw new NotSupportedException("Collection is read-only.");
            }


            bool ICollection<BonePose>.Remove(BonePose item)
            {
                throw new NotSupportedException("Collection is read-only.");
            }

 



            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return bones.GetEnumerator();
            }





            public bool Contains(BonePose item)
            {
                return bones.Contains(item);
            }

            public bool Contains(string boneName)
            {
                return boneDict.ContainsKey(boneName);
            }

            public BonePose this[string boneName]
            {
                get { return boneDict[boneName]; }
            }

        }

    }

}
