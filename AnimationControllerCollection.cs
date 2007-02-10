/*
 * AnimationControllerCollection.cs
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




    public class AnimationControllerCollection : ICollection<AnimationController>
    {
        private ModelAnimator controller;
        private bool isBlend = false;
        private List<AnimationController> items = new List<AnimationController>();
        private SortedList<string, AnimationController> keyedItems =
            new SortedList<string, AnimationController>();

        internal AnimationControllerCollection(ModelAnimator controller, bool 
            isBlend)
        {
            this.controller = controller;
            this.isBlend = isBlend;
        }



        #region IList<RunningAnimation> Members

        private void NewAnimationAdded(AnimationController newAnim)
        {
            newAnim.BoneAdded += new BonePoseEventHandler(newAnim_BoneAdded);
            newAnim.BoneRemoved += new BonePoseEventHandler(newAnim_BoneRemoved);
            BonePose[] boneArray = new BonePose[newAnim.AffectedBones.Count];
            newAnim.AffectedBones.CopyTo(boneArray,0);

            foreach (BonePose anim in boneArray)
            {
                if (isBlend)
                {
                    if (anim.CurrentBlendAnimation != null)
                    {
                        anim.CurrentBlendAnimation.AffectedBones.Remove(
                            anim);

                    }
                    anim.SetBlendAnimation(newAnim);
                }
                else
                {
                    if (anim.CurrentAnimation != null)
                    {
                        anim.CurrentAnimation.AffectedBones.Remove(
                            anim);

                    }
                    anim.SetRunningAnimation(newAnim);
                }
            }

                    
        }

        void newAnim_BoneRemoved(AnimationController sender, BonePose anim)
        {
            if (isBlend)
            {
                anim.SetBlendAnimation(null);
            }
            else
            {
                anim.SetRunningAnimation(null);
            }
        }

        void newAnim_BoneAdded(AnimationController sender, BonePose anim)
        {
            if (isBlend)
            {
                anim.SetBlendAnimation(sender);
            }
            else
            {
                anim.SetRunningAnimation(sender);
            }
        }

        public AnimationController this[string key]
        {
            get { return keyedItems[key]; }
        }

        #endregion

        #region ICollection<RunningAnimation> Members

        public void Add(string key, AnimationController item)
        {
            items.Add(item);
            keyedItems.Add(key, item);
            NewAnimationAdded(item);

        }

        public void Add(AnimationController item)
        {
            items.Add(item);
            NewAnimationAdded(item);
        }


        public void Clear()
        {
            while (items.Count > 0)
                Remove(items[0]);
        }

        public bool Contains(AnimationController item)
        {
            return items.Contains(item);
        }

        public void CopyTo(AnimationController[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false ; }
        }

        public bool Remove(AnimationController item)
        {
            if (items.Contains(item))
            {
                foreach (BonePose anim in item.AffectedBones)
                {
                    anim.SetRunningAnimation(null);
                }
                item.BoneRemoved -= newAnim_BoneRemoved;
                item.BoneAdded -= newAnim_BoneAdded;
                if (keyedItems.ContainsValue(item))
                    keyedItems.RemoveAt(keyedItems.IndexOfValue(item));
                return true;
            }
            return false;

        }

        public bool Remove(string key)
        {
            if (keyedItems.ContainsKey(key))
            {
                AnimationController item = keyedItems[key];
                items.Remove(item);
                keyedItems.Remove(key);
                item.BoneRemoved -= newAnim_BoneRemoved;
                item.BoneAdded -= newAnim_BoneAdded;
                return true;
            }

            return false;
        }

        #endregion

        #region IEnumerable<RunningAnimation> Members

        public IEnumerator<AnimationController> GetEnumerator()
        {
            return items.GetEnumerator() ;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion
    }
}