/*
 * XMeshImporter.cs
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

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.IO;
#endregion

namespace Animation.Content
{

    internal partial class XModelImporter
    {
        /// <summary>
        /// A helper for XModelImporter that loads Mesh nodes in .X files
        /// </summary>
        private class XMeshImporter
        {
            /// <summary>
            /// Represents a face of the model.  Used internally to buffer mesh data so that
            /// the mesh can be properly split up into ModelMeshParts such that there is
            /// 1 part per material
            /// </summary>
            private struct Face
            {
                // An array that represents the indices of the verts on the mesh that
                // this face contains
                public int[] VertexIndices;
                // The index of materials that determines what material is attached to
                // this face
                public int MaterialIndex;

                // Converts a face with 4 verts into 
                public Face[] ConvertQuadToTriangles()
                {
                    Face[] triangles = new Face[2];
                    triangles[0].VertexIndices = new int[3];
                    triangles[1].VertexIndices = new int[3];
                    for (int i = 0; i < 3; i++)
                    {
                        triangles[0].VertexIndices[i] = VertexIndices[i];
                        triangles[1].VertexIndices[i] = VertexIndices[(i + 2) % 4];
                    }

                    triangles[0].MaterialIndex = MaterialIndex;
                    triangles[1].MaterialIndex = MaterialIndex;
                    return triangles;
                }
            }

            #region Member Variables
            private Face[] faces;
            private Vector2[] texCoords = null;
            private Vector3[] normals = null;
            // We will calculate our own normals if there is not a 1:1 normal to face ratio
            private bool hasNormals;
            // The materials of the mesh
            private MaterialContent[] materials = new MaterialContent[0];
            // The blend weights
            Vector4[] weights = null, weights2 = null;
            // The blend weight indices
            Short4[] weightIndices = null, weightIndices2=null;
            // True if model is skinned & uses 8 bones per vertex max as opposed to 4
            bool useEightBones = false;
            private XModelImporter model;
            private XFileTokenizer tokens;
            // This will eventually turn into the Mesh
            private MeshContent mesh;
            // Contains a list of BoneWeightCollections, one for each vertex.
            // Each BoneWeightCollection contains a list of weights and the name
            // of the bone to which that weight belongs.
            private List<BoneWeightCollection> skinInfo =
                new List<BoneWeightCollection>();
            private SortedDictionary<string, Matrix> skinTransformDictionary =
                new SortedDictionary<string, Matrix>();
            private List<SkinTransform> skinTransforms = new List<SkinTransform>();
            // Is set to true if skinning information has been found for this mesh;
            // this determines whether or not skin weight and skin weight index channels
            // are added
            private bool isSkinned = false;
            // We will give each mesh a unique name because the default model processor
            // doesn't apply the correct transform to meshes that have a null name
            private static int meshID = 0;
            #endregion

            #region Constructors

            /// <summary>
            /// Creates a new instance of XMeshImporter
            /// </summary>
            /// <param name="model">The object that is importing the model from
            /// the current .X file</param>
            public XMeshImporter(XModelImporter model)
            {
                this.tokens = model.tokens;
                this.model = model;
                model.meshes.Add(this);
            }
            #endregion

            #region Vertex Importation Methods

            // "This template is instantiated on a per-mesh basis. Within a mesh, a sequence of n 
            // instances of this template will appear, where n is the number of bones (X file frames) 
            // that influence the vertices in the mesh. Each instance of the template basically defines
            // the influence of a particular bone on the mesh. There is a list of vertex indices, and a 
            // corresponding list of weights.
            // template SkinWeights 
            // { 
            //     STRING transformNodeName; 
            //     DWORD nWeights; 
            //     array DWORD vertexIndices[nWeights]; 
            //     array float weights[nWeights]; 
            //     Matrix4x4 matrixOffset; 
            // }
            // - The name of the bone whose influence is being defined is transformNodeName, 
            // and nWeights is the number of vertices affected by this bone.
            // - The vertices influenced by this bone are contained in vertexIndices, and the weights for 
            // each of the vertices influenced by this bone are contained in weights.
            // - The matrix matrixOffset transforms the mesh vertices to the space of the bone. When concatenated 
            // to the bone's transform, this provides the world space coordinates of the mesh as affected by the bone."
            // (http://msdn.microsoft.com/library/default.asp?url=/library/en-us/
            //  directx9_c/dx9_graphics_reference_x_file_format_templates.asp)
            // 


            // Reads in a bone that contains skin weights.  It then adds one bone weight
            //  to every vertex that is influenced by ths bone, which contains the name of the bone and the
            //  weight.
            public void ImportSkinWeights()
            {
                // We have found a skin weight node so this is a skinned model
                isSkinned = true;
                string boneName = tokens.SkipName().NextString();
                // an influence is an index to a vertex that is affected by the current bone
                int numInfluences = tokens.NextInt();
                List<int> influences = new List<int>();
                List<float> weights = new List<float>();
                for (int i = 0; i < numInfluences; i++)
                    influences.Add(tokens.NextInt());
                for (int i = 0; i < numInfluences; i++)
                {
                    weights.Add(tokens.NextFloat());
                    if (weights[i] == 0)
                    {
                        influences[i] = -1;
                    }
                }
                influences.RemoveAll(delegate(int i) { return i == -1; });
                weights.RemoveAll(delegate(float f) { return f == 0; });

                // Add the matrix that transforms the vertices to the space of the bone.
                // we will need this for skinned animation.
                Matrix blendOffset = tokens.NextMatrix();
                Util.ReflectMatrix(ref blendOffset);
                skinTransformDictionary.Add(boneName, blendOffset);
                // end of skin weights
                tokens.SkipToken();

                // add a new name/weight pair to every vertex influenced by the bone
                for (int i = 0; i < influences.Count; i++)
                    skinInfo[influences[i]].Add(new BoneWeight(boneName,
                        weights[i]));
            }

            /// <summary>
            /// Reads in the vertex positions and vertex indices for the mesh
            /// </summary>
            private void InitializeMesh()
            {
                // This will turn into the ModelMeshPart
                //geom = new GeometryContent();
               // mesh.Geometry.Add(geom);
                int numVerts = tokens.NextInt();
                // read the verts and create one boneweight collection for each vert
                // which will represent that vertex's skinning info (which bones influence it
                // and the weights of each bone's influence on the vertex)
                for (int i = 0; i < numVerts; i++)
                {
                    skinInfo.Add(new BoneWeightCollection());
                    Vector3 v = tokens.NextVector3();
                    // Reflect each vertex across z axis to convert it from left hand to right
                    // hand
                    v.Z *= -1;
                    mesh.Positions.Add(v);
                }

                // Add the indices that describe the order in which
                // the vertices are rendered
                int numFaces = tokens.NextInt();
                faces = new Face[numFaces];
                for (int i = 0; i < numFaces; i++)
                {
                    int numVertsPerFace = tokens.NextInt();
                    faces[i].VertexIndices = new int[numVertsPerFace];
                    for (int j = 0; j < numVertsPerFace; j ++)
                        faces[i].VertexIndices[j] = tokens.NextInt();
                    tokens.SkipToken();
                }

            }

            // template MeshTextureCoords
            // {
            //      DWORD nTextureCoords;
            //      array Coords2d textureCoords[nTextureCoords] ;
            // } 
            /// <summary>
            /// Imports the texture coordinates associated with the current mesh.
            /// </summary>
            private void ImportTextureCoords()
            {
                tokens.SkipName();
                int numCoords = tokens.NextInt();
                texCoords = new Vector2[numCoords];
                for (int i = 0; i < numCoords; i++)
                    texCoords[i] = tokens.NextVector2();
                // end of vertex coordinates
                tokens.SkipToken();
            }

            // template MeshNormals
            // {
            //     DWORD nNormals;
            //     array Vector normals[nNormals];
            //     DWORD nFaceNormals;
            //     array MeshFace faceNormals[nFaceNormals];
            // } 
            /// <summary>
            /// Imports the normals associated with the current mesh.
            /// </summary>
            private void ImportNormals()
            {
                tokens.ReadName();
                hasNormals = true;

                int numNormals = tokens.NextInt();
                if (numNormals == mesh.Positions.Count)
                    normals = new Vector3[numNormals];
                for (int i = 0; i < numNormals; i++)
                {
                    Vector3 norm = tokens.NextVector3();
                    if (numNormals == mesh.Positions.Count)
                    {
                        normals[i] = norm;
                        normals[i].Z *= -1;
                    }
                }

                int numFaces = tokens.NextInt();
                for (int i = 0; i < numFaces; i++)
                {
                    int numNormalsPerFace = tokens.NextInt();
                    tokens.SkipTokens(2*numNormalsPerFace+1);
                }
                // end of mesh normals
                tokens.SkipToken();
            }



            // template Mesh
            // {
            //      DWORD nVertices;
            //      array Vector vertices[nVertices];
            //      DWORD nFaces;
            //      array MeshFace faces[nFaces];
            //      [...]
            // }
            /// <summary>
            /// Imports a mesh.
            /// </summary>
            /// <returns>The imported mesh</returns>
            public NodeContent ImportMesh()
            {
                // Initialize mesh
                mesh = new MeshContent();
                mesh.Name = tokens.ReadName();
                if (mesh.Name == null)
                {
                    mesh.Name = "Mesh" + meshID.ToString();
                    meshID++;
                }
                // Read in vertex positions and vertex indices
                InitializeMesh();

                // Fill in the geometry channels and materials for this mesh
                for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
                {
                    if (next == "MeshNormals")
                        ImportNormals();
                    else if (next == "MeshTextureCoords")
                        ImportTextureCoords();
                    else if (next == "SkinWeights")
                        ImportSkinWeights();
                    else if (next == "MeshMaterialList")
                        ImportMaterialList();
                    else if (next == "Frame")
                        mesh.Children.Add(model.ImportNode());
                    else if (next == "{")
                        tokens.SkipNode();
                    else if (next == "}")
                        break;
                }
                return mesh;
            }
            #endregion

            #region Material Importation Methods
            // template MeshMaterialList
            // {
            //      DWORD nMaterials;
            //      DWORD nFaceIndexes;
            //      array DWORD faceIndexes[nFaceIndexes];
            //      [Material]
            // } 
            /// <summary>
            /// Imports a material list that contains the materials used by the current mesh.
            /// </summary>
            private void ImportMaterialList()
            {
                int numMaterials = tokens.SkipName().NextInt();
                materials = new MaterialContent[numMaterials];
                int numFaces = tokens.NextInt();

                // skip all the indices and their commas/semicolons since
                // we are just going to apply this material to the entire mesh
                for (int i = 0; i < numFaces; i++)
                    faces[i].MaterialIndex = tokens.NextInt();
                // account for blenders mistake of putting an extra semicolon here
                if (tokens.Peek == ";")
                    tokens.SkipToken();
                for (int i = 0; i < numMaterials; i++)
                {
                    if (tokens.Peek == "{")
                    {
                        tokens.SkipToken();
                        string materialReference = tokens.NextToken();
                        tokens.SkipToken();
                        materials[i] = model.materials[materialReference];
                    }
                    else
                    {
                        tokens.SkipToken();
                        materials[i] = model.ImportMaterial();
                    }
                }
                // end of material list
                tokens.SkipToken();
            }


            #endregion

            #region Other Methods

            /// <summary>
            /// Adds all the buffered channels to the mesh and merges duplicate positions/verts
            /// </summary>
            private void AddAllChannels()
            {

                if (normals != null)
                    AddChannel<Vector3>(VertexElementUsage.Normal.ToString(), normals);
                else if (hasNormals)
                    MeshHelper.CalculateNormals(mesh, true);
                if (texCoords != null)
                    AddChannel<Vector2>("TextureCoordinate0", texCoords);
                if (weightIndices != null)
                {
                    AddChannel<Short4>(VertexElementUsage.BlendIndices.ToString()+"0", weightIndices);
                    if (useEightBones)
                        AddChannel<Short4>(VertexElementUsage.BlendIndices.ToString()+"1", weightIndices2);
                }
                if (weights != null)
                {
                    AddChannel<Vector4>(VertexElementUsage.BlendWeight.ToString()+"0", weights);
                    if (useEightBones)
                        AddChannel<Vector4>(VertexElementUsage.BlendWeight.ToString()+"1", weights2);
                }
                
                MeshHelper.MergeDuplicatePositions(mesh, 0);
                MeshHelper.MergeDuplicateVertices(mesh);
                MeshHelper.OptimizeForCache(mesh);

                
            }

            /// <summary>
            /// Adds a channel to the mesh
            /// </summary>
            /// <typeparam name="T">The structure that stores the channel data</typeparam>
            /// <param name="channelName">The type of channel</param>
            /// <param name="channelItems">The buffered items to add to the channel</param>
            private void AddChannel<T>(string channelName, T[] channelItems)
            {
                foreach (GeometryContent geom in mesh.Geometry)
                {
                    T[] channelData = new T[geom.Vertices.VertexCount];
                    for (int i = 0; i < channelData.Length; i ++)
                        channelData[i] = channelItems[geom.Vertices.PositionIndices[i]];
                    geom.Vertices.Channels.Add<T>(channelName, channelData);
                }
            }
            /// <summary>
            /// Converts the bone weight collections into working vertex channels by using the
            /// provided bone index dictionary.  Converts bone names into indices.
            /// </summary>
            /// <param name="boneIndices">A dictionary that maps bone names to their indices</param>
            public void AddWeights(Dictionary<string, int> boneIndices)
            {
                if (!isSkinned)
                    return;

                Dictionary<string, int> meshBoneIndices = new Dictionary<string, int>();
                int currentIndex = 0;
                // The bone indices are already sorted by index
                foreach (KeyValuePair<string, int> k in boneIndices)
                {
                    if (skinTransformDictionary.ContainsKey(k.Key))
                    {
                        meshBoneIndices.Add(k.Key, currentIndex++);
                        SkinTransform transform = new SkinTransform();
                        transform.BoneName = k.Key;
                        transform.Transform = skinTransformDictionary[k.Key];
                        skinTransforms.Add(transform);
                    }
                }


                // These two lists hold the data for the two new channels (the weights and indices)
                weights = new Vector4[mesh.Positions.Count];
                weights2 = new Vector4[mesh.Positions.Count];
                weightIndices = new Short4[mesh.Positions.Count];
                weightIndices2 = new Short4[mesh.Positions.Count];

                // The index of the position that this vertex refers to
                int index = 0;
                foreach (BoneWeightCollection c in skinInfo)
                {
                    

                    // The number of weights associated with the current vertex
                    int ct = c.Count;

                    Vector4 w = new Vector4();
                    Vector4 w2 = new Vector4();
                    short i0 = 0, i1 = 0, i2 = 0, i3 = 0, i4 = 0, i5 = 0, i6 = 0, i7 = 0;
                    // Fill in the weights
                    w.X = ct > 0 ? c[0].Weight : 0;
                    w.Y = ct > 1 ? c[1].Weight : 0;
                    w.Z = ct > 2 ? c[2].Weight : 0;
                    w.W = ct > 3 ? c[3].Weight : 0;


                    // If the vertex cotnains no skinning info, assign it to the mesh's root
                    // bone with a weight of 1
                    if (ct > 0 && c[0].BoneName != null)
                        i0 = (short)meshBoneIndices[c[0].BoneName];
                    if (ct == 0)
                        w.X = 1.0f;

                    // Fill in the indices
                    if (c.Count > 1 && c[1].BoneName != null) i1 = (short)meshBoneIndices[c[1].BoneName];
                    if (c.Count > 2 && c[2].BoneName != null) i2 = (short)meshBoneIndices[c[2].BoneName];
                    if (c.Count > 3 && c[3].BoneName != null) i3 = (short)meshBoneIndices[c[3].BoneName];

                    // If the count is greater then 4 for any bone on a mesh, then use the 8 bone
                    // shader
                    if (ct > 4)
                    {
                        useEightBones = true;
                        w2.X = ct > 4 ? c[4].Weight : 0;
                        w2.Y = ct > 5 ? c[5].Weight : 0;
                        w2.Z = ct > 6 ? c[6].Weight : 0;
                        w2.W = ct > 7 ? c[7].Weight : 0;
                        if (c.Count > 4 && c[4].BoneName != null) i4 = (short)meshBoneIndices[c[4].BoneName];
                        if (c.Count > 5 && c[5].BoneName != null) i5 = (short)meshBoneIndices[c[5].BoneName];
                        if (c.Count > 6 && c[6].BoneName != null) i6 = (short)meshBoneIndices[c[6].BoneName];
                        if (c.Count > 7 && c[7].BoneName != null) i7 = (short)meshBoneIndices[c[7].BoneName];
                    }

                    // We have a list of boneweight/bone index objects that are ordered such that
                    // BoneWeightCollection[i] is the weight and index for vertex i.
                    weights[index] = w;
                    weights2[index] = w2;
                    weightIndices[index] = new Short4(i0, i1, i2, i3);
                    weightIndices2[index] = new Short4(i4, 0, 0, 0);
                    index++;
                }

            }
            #endregion

            /// <summary>
            /// Creates the ModelMeshParts-to-be (geometry) by splitting up the mesh
            /// via materials
            /// </summary>
            public void CreateGeometry()
            {
                // Number of geometries to create
                int numPartions = materials.Length == 0
                    ? 1 : materials.Length;
                // An array of the faces that each geometry will contain
                List<Face>[] partitionedFaces = new List<Face>[numPartions];

                // Partion the faces.  Each face has a material index, and
                // each geometry has its own material, so the material index
                // refers to the geometry index for the face.
                for (int i = 0; i < partitionedFaces.Length; i++)
                    partitionedFaces[i] = new List<Face>();
                for (int i = 0; i < faces.Length; i++)
                {
                    if (faces[i].VertexIndices.Length == 4)
                        partitionedFaces[faces[i].MaterialIndex].AddRange(
                            faces[i].ConvertQuadToTriangles());
                    else
                        partitionedFaces[faces[i].MaterialIndex].Add(faces[i]);
                }

                // Add the partioned faces to their respective geometries
                int index = 0;
                foreach (List<Face> faceList in partitionedFaces)
                {
                    GeometryContent geom = new GeometryContent();
                    mesh.Geometry.Add(geom);
                    for (int i = 0; i < faceList.Count * 3; i++)
                        geom.Indices.Add(i);
                    foreach (Face face in faceList)
                        geom.Vertices.AddRange(face.VertexIndices);              
                    if (materials.Length > 0)
                        geom.Material = materials[index++];
                }

                // Add the channels to the geometries
                AddAllChannels();
                

            }

            public SkinTransform[] SkinTransforms
            {
                get { return skinTransforms.Count > 0 ? skinTransforms.ToArray() : null; }
            }
        }


    }


}