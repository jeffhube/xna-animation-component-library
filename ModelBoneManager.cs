/*
 * ModelBoneManager.cs
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
    public interface IBone
    {
        IList<IBone> Children { get;}
        IBone Parent { get;}
        Matrix Transform { get;}
        int Index { get;}
        string Name { get;}

    }
    public sealed class ModelBoneManager
    {
        private SortedDictionary<string, BoneNode> boneDict;
        private BoneNode[] bones;
        private BoneNode[] parentlessBones = null;
        public class BoneNode
        {
            private int index;
            private ReadOnlyCollection<BoneNode> children;
            private readonly BoneNode parent;
            private Matrix transform;
            private readonly string name;

            internal BoneNode(ModelBoneManager manager,
                BoneNode parent, ModelBone bone) 
            {
                BoneNode[] childrenArray = new BoneNode[bone.Children.Count];
                transform = bone.Transform;

                index = bone.Index;
                name = bone.Name == null ? null : bone.Name.ToString();

                if (!manager.boneDict.ContainsKey(this.name))
                {
                    manager.boneDict.Add(this.name, this);
                }
                this.parent = parent;
                for (int i = 0; i < bone.Children.Count; i++)
                {
                    childrenArray[i] = new BoneNode(manager, this, bone.Children[i]);
                }
                manager.bones[index] = this;
                children = new List<BoneNode>(childrenArray).AsReadOnly();


            }

            internal BoneNode(ModelBoneManager manager,
                    BoneNode parent, IBone bone)
            {
                BoneNode[] childrenArray = new BoneNode[bone.Children.Count];
                transform = bone.Transform;

                index = bone.Index;
                name = bone.Name == null ? null : bone.Name.ToString();
                if (!manager.boneDict.ContainsKey(this.name))
                {
                    manager.boneDict.Add(this.name, this);
                }
                this.parent = parent;
                for (int i = 0; i < bone.Children.Count; i++)
                {
                    childrenArray[i] = new BoneNode(manager, this, bone.Children[i]);
                }
                manager.bones[Index] = this;
                children = new List<BoneNode>(childrenArray).AsReadOnly();


            }
            public ReadOnlyCollection<BoneNode> Children
            {
                get { return children; }
            }
            public int Index
            {
                get { return index; }
            }
            public Matrix Transform
            {
                get { return transform; }
                set { transform = value; }
            }
            public BoneNode Parent
            {
                get { return parent; }
            }
            public string Name
            {
                get { return name; }
            }
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

        public void CopyBoneTransformsTo(Matrix[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = this.bones[i].Transform;
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

        public ModelBoneManager(ICollection<IBone> boneCollection)
        {
            List<BoneNode> parentlessBoneList = new List<BoneNode>();
            this.bones = new BoneNode[boneCollection.Count];
            boneDict = new SortedDictionary<string, BoneNode>();
            foreach (IBone b in boneCollection)
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
