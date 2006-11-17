/*
 * AnimatedMeshImporter.cs
 * Helper nested class that imports a mesh that is associated with a model
 * in a .X file.
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.IO;
#endregion

namespace Animation.Content
{

    public partial class AnimatedModelImporter
    {
        /// <summary>
        /// A helper for AnimatedModelImporter that loads Mesh nodes in .X files
        /// </summary>
        private class AnimatedMeshImporter
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
            Vector4[] weights = null;
            // The blend weight indices
            Byte4[] weightIndices = null;
            private AnimatedModelImporter model;
            private XFileTokenizer tokens;
            // This will eventually turn into the Mesh
            private MeshContent mesh;
            // Contains a list of BoneWeightCollections, one for each vertex.
            // Each BoneWeightCollection contains a list of weights and the name
            // of the bone to which that weight belongs.
            private List<BoneWeightCollection> skinInfo =
                new List<BoneWeightCollection>();
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
            /// Creates a new instance of AnimatedMeshImporter
            /// </summary>
            /// <param name="model">The object that is importing the model from
            /// the current .X file</param>
            public AnimatedMeshImporter(AnimatedModelImporter model)
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
                    weights.Add(tokens.NextFloat());

                // Add the matrix that transforms the vertices to the space of the bone.
                // we will need this for skinned animation.
                Matrix blendOffset = tokens.NextMatrix();
                model.ReflectMatrix(ref blendOffset);
                model.blendTransforms.Add(boneName, blendOffset);
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
                    tokens.SkipToken();
                    materials[i] = ImportMaterial();
                }
                // end of material list
                tokens.SkipToken();
            }

            /// <summary>
            /// Loads a custom material.  That is, loads a material with a custom effect.
            /// </summary>
            /// <returns>The custom material</returns>
            private MaterialContent ImportCustomMaterial()
            {
                EffectMaterialContent content = new EffectMaterialContent();
                tokens.SkipName();
                string effectName = model.GetAbsolutePath(tokens.NextString());
    
                content.Effect = new ExternalReference<EffectContent>(effectName);
                
                // Find value initializers for the effect parameters and set the values
                // as indicated
                for (string token = tokens.NextToken(); token != "}"; token = tokens.NextToken())
                {
                    if (token == "EffectParamFloats")
                    {
                        tokens.SkipName();
                        string floatsParamName = tokens.NextString();
                        int numFloats = tokens.NextInt();
                        float[] floats = new float[numFloats];
                        for (int i = 0; i < numFloats; i++)
                            floats[i] = tokens.NextFloat();
                        tokens.SkipToken();       
                        content.OpaqueData.Add(floatsParamName, floats);
                    }
                    else if (token == "EffectParamDWord")
                    {
                        tokens.SkipName();
                        string dwordParamName = tokens.NextString();
                        float dword = tokens.NextFloat();
                        tokens.SkipToken();
                        content.OpaqueData.Add(dwordParamName, dword);
                    }
                    else if (token == "EffectParamString")
                    {
                        tokens.SkipName();
                        string stringParamName = tokens.NextString();
                        string paramValue = tokens.NextString();
                        tokens.SkipToken();
                        content.OpaqueData.Add(stringParamName, paramValue);
                    }
                    if (token == "{")
                        tokens.SkipNode();
                }
                return content;

            }

            // template Material
            // {
            //      ColorRGBA faceColor;
            //      FLOAT power;
            //      ColorRGB specularColor;
            //      ColorRGB emissiveColor;
            //      [...]
            // } 
            /// <summary>
            /// Imports a material, which defines the textures that a mesh uses and the way in which
            /// light reflects off the mesh
            /// </summary>
            private MaterialContent ImportMaterial()
            {
                ExternalReference<TextureContent> texRef = null;
                BasicMaterialContent basicMaterial = new BasicMaterialContent();
                MaterialContent returnMaterial = basicMaterial;
                // make sure name isn't null
                string materialName = tokens.ReadName();
                if (materialName == null)
                    materialName = "";
                // Diffuse color describes how diffuse (directional) light
                // reflects off the mesh
                basicMaterial.DiffuseColor = new Vector3(tokens.NextFloat(),
                    tokens.NextFloat(), tokens.NextFloat());
                // We dont care about the alpha component of diffuse light.
                // I don't even understand what this is useful for.
                tokens.NextFloat();
                // Specular power is inversely exponentially proportional to the
                // strength of specular light
                basicMaterial.SpecularPower = tokens.SkipToken().NextFloat();
                // Specular color describes how specular (directional and shiny)
                // light reflects off the mesh
                basicMaterial.SpecularColor = tokens.NextVector3();
                basicMaterial.EmissiveColor = tokens.NextVector3();
                // Import any textures associated with this material
                for (string token = tokens.NextToken();
                    token != "}"; )
                {
                    if (token == "TextureFilename")
                    {
                        // Get the absolute path of the texture
                        string fileName = tokens.SkipName().NextString();
                        texRef =
                            new ExternalReference<TextureContent>(model.GetAbsolutePath(fileName));
                        tokens.SkipToken();
                    }
                    else if (token == "EffectInstance")
                        returnMaterial = ImportCustomMaterial();
                    else if (token == "{")
                        tokens.SkipNode();
                    token = tokens.NextToken();
                }

                if (returnMaterial is BasicMaterialContent)
                    basicMaterial.Texture = texRef;
                returnMaterial.Name = materialName;
                return returnMaterial;

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
                    AddChannel<Byte4>(VertexElementUsage.BlendIndices.ToString(), weightIndices);
                if (weights != null)
                    AddChannel<Vector4>(VertexElementUsage.BlendWeight.ToString(), weights);
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
                // These two lists hold the data for the two new channels (the weights and indices)
                weights = new Vector4[mesh.Positions.Count];
                weightIndices = new Byte4[mesh.Positions.Count];

                // The index of the position that this vertex refers to
                int index = 0;
                foreach (BoneWeightCollection c in skinInfo)
                {
                    // The number of weights associated with the current vertex
                    int ct = c.Count;
                    Vector4 w = new Vector4();
                    Vector4 i = new Vector4();
                    // Fill in the weights
                    w.X = ct > 0 ? c[0].Weight : 0;
                    w.Y = ct > 1 ? c[1].Weight : 0;
                    w.Z = ct > 2 ? c[2].Weight : 0;
                    w.W = ct > 3 ? c[3].Weight : 0;

                    // If the vertex cotnains no skinning info, assign it to the mesh's root
                    // bone with a weight of 1
                    if (ct > 0 && c[0].BoneName != null)
                        i.X = boneIndices[c[0].BoneName];
                    if (ct == 0)
                        w.X = 1.0f;

                    // Fill in the indices
                    if (c.Count > 1 && c[1].BoneName != null) i.Y = boneIndices[c[1].BoneName];
                    if (c.Count > 2 && c[2].BoneName != null) i.Z = boneIndices[c[2].BoneName];
                    if (c.Count > 3 && c[3].BoneName != null) i.W = boneIndices[c[3].BoneName];
                    Byte4 ib = new Byte4(i);

                    // We have a list of boneweight/bone index objects that are ordered such that
                    // BoneWeightCollection[i] is the weight and index for vertex i.
                    weights[index] = w;
                    weightIndices[index] = new Byte4(i);
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

                // Partion the faces
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
        }


    }


}
