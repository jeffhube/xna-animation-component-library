/*
 * ModelBoneManager.cs
 * Manages bone collections, much like a Model
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

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Animation
{
    internal sealed class ModelBoneManager
    {
        private SortedDictionary<string, BoneNode> boneDict;
        private BoneNode[] bones;
        private BoneNode[] parentlessBones = null;
        private int boneNum = 0;
        internal class BoneNode
        {
            internal BoneNode(ModelBoneManager manager,
                BoneNode parent, ModelBone bone) 
            {
                Children = new BoneNode[bone.Children.Count];
                Transform = bone.Transform;

                Index = bone.Index;
                Name = bone.Name == null ? null : bone.Name.ToString();
                if (manager.boneDict.ContainsKey(Name))
                {
                    while (manager.boneDict.ContainsKey(Name))
                    {
                        Name = "Bone" + manager.boneNum.ToString();
                        manager.boneNum++;
                    }

                }
                manager.boneDict.Add(this.Name, this);
                this.Parent = parent;
                for (int i = 0; i < bone.Children.Count; i++)
                {
                    Children[i] = new BoneNode(manager, this, bone.Children[i]);
                }
                manager.bones[Index] = this;


            }
            internal int Index;
            internal readonly BoneNode[] Children;
            internal readonly BoneNode Parent;
            internal Matrix Transform;
            internal readonly string Name;
        }

        public BoneNode this[int index]
        {
            get { return bones[index]; }
        }
        public BoneNode this[string name]
        {
            get { return boneDict[name]; }
        }


        private void CopyAbsoluteBoneTransformsTo(Matrix[] bones, BoneNode node)
        {
            foreach (BoneNode b in node.Children)
            {
                bones[b.Index] = b.Transform * bones[node.Index];
                CopyAbsoluteBoneTransformsTo(bones, b);
            }
        }

        public int Count
        { get { return bones.Length; } }

        public void CopyAbsoluteBoneTransformsTo(Matrix[] bones)
        {
            foreach (BoneNode b in parentlessBones)
            {
                bones[b.Index] = b.Transform;
                CopyAbsoluteBoneTransformsTo(bones, b);
            }
  

        }

        public void CopyBoneTransformsFrom(ModelBoneCollection collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                bones[i].Transform = collection[i].Transform;
            }
        }

        public void CopyBoneTransformsFrom(Matrix[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
                this.bones[i].Transform = bones[i];
        }



        public ModelBoneManager(ModelBoneCollection boneCollection)
        {
            List<BoneNode> parentlessBoneList = new List<BoneNode>();
            this.bones = new BoneNode[boneCollection.Count];
            boneDict = new SortedDictionary<string, BoneNode>();
            foreach (ModelBone b in boneCollection)
            {
                if (b.Parent == null)
                {
                    BoneNode node = new BoneNode(this, null, b);
                    parentlessBoneList.Add(node);
                }
            }
            parentlessBones = parentlessBoneList.ToArray();
        }
    }
}
