/*
 *  AnimatedModelImporter.cs
 *  Imports a DirectX file using the content pipeline.
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
#endregion

namespace Animation.Content
{
    /// <summary>
    /// Imports a directx model that contains skinning info.
    /// </summary>
    [ContentImporter(".X", CacheImportedData = false, DefaultProcessor = "AnimatedModelProcessor")]
    public partial class AnimatedModelImporter : ContentImporter<NodeContent>
    {

        #region Member Variables
        // The file that we are importing
        private string fileName;
        private XFileTokenizer tokens;
        // Stores the root frame
        private NodeContent root;
        // Stores the number of units that represent one second in animation data
        // Is null if the file contains no information, and a default value will be used
        private int? animTicksPerSecond;
        private const int DEFAULT_TICKS_PER_SECOND = 100;
        // Stores information about the current build for the content pipeline
        private ContentImporterContext context;
        // A list of meshes that have been imported
        private List<AnimatedMeshImporter> meshes = new List<AnimatedMeshImporter>();
        // Stores the current bone index while traversing the NodeContent tree
        int curIndex = 0;
        // Contains the skin transform for a particular bone.  See ImportSkinWeights for more
        // info
        private Dictionary<string, Matrix> blendTransforms = new Dictionary<string, Matrix>();
        // Contains a collection of bone name keys that map to the index of the given bone.
        // This allows us to replace BoneWeightCollection lists for each mesh.  These collections
        // originally store the bone name attached to the weight, and boneIndices allows us to
        // replace the name with the index, which is required for skinned animation
        private Dictionary<string, int> boneIndices = new Dictionary<string, int>();
        #endregion

        #region Non Animation Importation Methods
        public override NodeContent Import(string filename, ContentImporterContext context)
        {
            this.fileName = filename;
            this.context = context;
            // Create an instance of a class that splits a .X file into tokens and provides
            // functionality for iterating and parsing the tokens
            tokens = new XFileTokenizer(filename);

            // fill in the tree
            ImportRoot();

            // This fills in the Dictionary that maps bone names to their indices.  Now that
            // all the bones are loaded, we can find this info out.  Their indices are determined
            // in a preorder tree traversal.
            GetBoneIndices(root);

            // Now that we have mapped bone names to their indices, we can create the vertices
            // in each mesh so that they contain indices and weights
            foreach (AnimatedMeshImporter mesh in meshes)
            {
                mesh.AddWeights(boneIndices);
                mesh.CreateGeometry();
            }


            // Allow processor to access any skinning data we might have
            root.OpaqueData.Add("BlendTransforms", blendTransforms);
            return root;
        }

        /// <summary>
        /// Imports the root and animation data associated with it
        /// </summary>
        private void ImportRoot()
        {
            // Read all tokens in the file
            if (!tokens.AtEnd)
            {
                do
                {
                    string next = tokens.NextToken();
                    // If nodes are found in the same scope as the root, add them
                    // as children because the Model class only supports one root
                    // frame
                    if (next == "Frame")
                        if (root == null)
                            root = ImportNode();
                        else
                            root.Children.Add(ImportNode());
                    //template AnimTicksPerSecond
                    // {
                    //     DWORD AnimTicksPerSecond;
                    // } 
                    else if (next == "AnimTicksPerSecond")
                        animTicksPerSecond = tokens.SkipName().NextInt();
                    // See ImportAnimationSet for template info
                    else if (next == "AnimationSet")
                        ImportAnimationSet();
                }
                while (!tokens.AtEnd);
            }
        }

       
        // A frame can store any data, but is constrained such that each frame must haveB
        // a transform matrix for .X meshes.
        // template Frame
        // {
        //    [...]			
        // } 
        /// <summary>
        /// Imports a data Node in a directx file, usually a Frame node.
        /// </summary>
        /// <returns>The imported node</returns>
        private NodeContent ImportNode()
        {
            NodeContent c = new NodeContent();
            c.Name = tokens.ReadName();
            if (c.Name == null)
                c.Name = " ";
            // process all of this frame's children
            for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
            {

                if (next == "Frame")
                    c.Children.Add(ImportNode());
                else if (next == "FrameTransformMatrix")
                    c.Transform = ImportFrameTransformMatrix();
                else if (next == "Mesh")
                {
                    AnimatedMeshImporter meshImporter = new AnimatedMeshImporter(this);
                    c.Children.Add(meshImporter.ImportMesh());
                }
                // Ignore templates, which define new structure that store data
                // for now, we only will work with the standard directx templates
                else if (next == "template")
                    tokens.SkipName().SkipNode();
                // Skin weight nodes can exist either inside meshes or inside frames.
                // When they exist in a frame, they will refer to a universal mesh
                else if (next == "SkinWeights")
                    meshes[0].ImportSkinWeights();
                // an data node that we are uninterested in
                else if (next == "{")
                    tokens.SkipNode();
            }

            return c;
        }

        // template FrameTransformMatrix
        // {
        //     Matrix4x4 frameMatrix;
        // } 
        /// <summary>
        /// Imports a transform matrix attached to a ContentNode
        /// </summary>
        /// <returns>The transform matrix attached to the current ContentNode</returns>
        private Matrix ImportFrameTransformMatrix()
        {
            Matrix m = tokens.SkipName().NextMatrix();
            // Reflect the matrix across the Z axis to swap from left hand to right hand
            // coordinate system
            ReflectMatrix(ref m);
            // skip the "}" at the end of the node
            tokens.SkipToken();
            return m;
        }
        #endregion

        #region Animation Importation Methods

        // template AnimationSet
        // {
        //     [ Animation ]
        // } 
        /// <summary>
        /// Imports an animation set that is added to the AnimationContentDictionary of
        /// the root frame.
        /// </summary>
        private void ImportAnimationSet()
        {
            AnimationContent animSet = new AnimationContent();
            animSet.Name = tokens.ReadName();
            // Give each animation a unique name
            if (animSet.Name == null)
                animSet.Name = "Animation" + root.Animations.Count.ToString();

            // Fill in all the channels of the animation.  Each channel refers to 
            // a single bone's role in the animation.
            for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
            {
                if (next == "Animation")
                {
                    string boneName;
                    AnimationChannel anim = ImportAnimationChannel(out boneName);
                    // Every channel must be attached to a bone!
                    if (boneName == null)
                        throw new Exception("Animation in file is not attached to any joint");
                    // Make sure that the duration of the animation is set to the 
                    // duration of the longest animation channel
                    if (anim[anim.Count - 1].Time > animSet.Duration)
                        animSet.Duration = anim[anim.Count - 1].Time;
                    animSet.Channels.Add(boneName, anim);
                }
                // skip nodes we are uninterested in
                else if (next == "{")
                    tokens.SkipNode();
            }
            root.Animations.Add(animSet.Name, animSet);
        }




        /*
         * template AnimationKey 
         * {
         *     DWORD keyType;
         *     DWORD nKeys;
         *     array TimedFloatKeys keys[nKeys];
         * } 
         * 
         * 
         * template TimedFloatKeys 
         * { 
         *     DWORD time; 
         *     FloatKeys tfkeys; 
         * } 
         * 
         * template FloatKeys
         * {
         *     DWORD nValues;
         *     array float values[nValues];
         * }        
         */
        /// <summary>
        ///  Imports a key frame list associated with an animation channel
        /// </summary>
        /// <param name="keyType">The type of animation keys used by the current channel</param>
        /// <returns>The list of key frames for the given key type in the current channel</returns>
        private AnimationKeyframe[] ImportAnimationKey(out int keyType)
        {
            // These keys can be rotation (0),scale(1),translation(2), or matrix(3 or 4) keys.
            keyType = tokens.SkipName().NextInt();
            // Number of frames in channel
            int numFrames = tokens.NextInt();
            AnimationKeyframe[] frames = new AnimationKeyframe[numFrames];
            // Find the ticks per millisecond that defines how fast the animation should go
            double ticksPerMS = animTicksPerSecond == null ? DEFAULT_TICKS_PER_SECOND / 1000.0 
                : (double)animTicksPerSecond / 1000.0;

            // fill in the frames
            for (int i = 0; i < numFrames; i++)
            {
                // Create a timespan object that represents the time that the current keyframe
                // occurs
                TimeSpan time = new TimeSpan(0, 0, 0, 0,
                    (int)(tokens.NextInt() / ticksPerMS));
                // The number of keys that represents the transform for this keyframe. 
                // Quaternions (rotation keys) have 4,
                // Vectors (scale and translation) have 3,
                // Matrices have 16
                int numKeys = tokens.NextInt();
                Matrix transform = tokens.NextMatrix();
                ReflectMatrix(ref transform);
                // Right now, the importer only supports matrices.
                if (keyType != 3 && keyType != 4)
                    throw new Exception("Only matrix animation keys are currently supported.");
                tokens.SkipToken();
                frames[i] = new AnimationKeyframe(time, transform);
            }
            tokens.SkipToken();
            return frames;
        }



        /*
         * template Animation
         * {
         * [...]
         * }
         */
        /// <summary>
        /// Fills in all the channels of an animation.  Each channel refers to 
        /// a single bone's role in the animation.
        /// </summary>
        /// <param name="boneName">The name of the bone associated with the channel</param>
        /// <returns>The imported animation channel</returns>
        private AnimationChannel ImportAnimationChannel(out string boneName)
        {
            AnimationChannel anim = new AnimationChannel();
            // Store the frames in an array, which acts as an intermediate data set
            // This will allow us to more easily provide support for non Matrix
            // animation keys at a later time
            AnimationKeyframe[] frames = null;
            boneName = null;
            tokens.SkipName();
            for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
            {
                // A set of animation keys
                if (next == "AnimationKey")
                {
                    int keyType;
                    frames = ImportAnimationKey(out keyType);
                }
                // A possible bone name
                else if (next == "{")
                {
                    string token = tokens.NextToken();
                    if (tokens.NextToken() != "}")
                        tokens.SkipNode();
                    else
                        boneName = token;
                }
            }
            // Fill in the channel with the frames
            if (frames != null)
                foreach (AnimationKeyframe f in frames)
                    anim.Add(f);
            return anim;
        }
        #endregion

        #region Other Methods

        /// <summary>
        /// Reflects a matrix across the Z axis by multiplying both the Z
        /// column and the Z row by -1 such that the Z,Z element stays intact.
        /// </summary>
        /// <param name="m">The matrix to be reflected across the Z axis</param>
        private void ReflectMatrix(ref Matrix m)
        {
            m.M13 *= -1;
            m.M23 *= -1;
            m.M33 *= -1;
            m.M43 *= -1;
            m.M31 *= -1;
            m.M32 *= -1;
            m.M33 *= -1;
            m.M34 *= -1;
        }


        // This allows us to replace BoneWeightCollection lists for each mesh.  These collections
        // originally store the bone name attached to the weight, and boneIndices allows us to
        // replace the name with the index, which is required for skinned animation
        /// <summary>
        /// Traverses the tree and fills a collection of bone name keys that map to the index of 
        /// the given bone.
        /// </summary>
        /// <param name="root">The root of the tree that is traversed</param>
        private void GetBoneIndices(NodeContent root)
        {
            if (root.Name != null)
                boneIndices.Add(root.Name, curIndex);
            curIndex++;
            foreach (NodeContent c in root.Children)
                GetBoneIndices(c);
        }
        #endregion

    }
}
