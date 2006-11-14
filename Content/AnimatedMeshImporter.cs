/*
 *  AnimatedMeshImporter.cs
 *  Helper nested class that imports a mesh that is associated with a model
 *  in a .X file.
 *  Copyright (C) 2006 XNA Animation Component Library CodePlex Project
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
            #region Member Variables
            // The number of vertices in this mesh
            private int numVerts;
            // The number of faces in this mesh
            private int numFaces;
            private AnimatedModelImporter model;
            private XFileTokenizer tokens;
            // This will eventually turn into the ModelMeshPart for this mesh
            private GeometryContent geom;
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
            /// <summary>
            /// Reads in a bone that contains skin weights.  It then adds one bone weight
            //  to every vertex that is influenced by ths bone, which contains the name of the bone and the
            //  weight.
            /// </summary>
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
            /// Reads in the vertex positions and vertex indices for the geometry object
            /// </summary>
            private void InitializeGeometry()
            {
                // This will turn into the ModelMeshPart
                geom = new GeometryContent();
                mesh.Geometry.Add(geom);
                numVerts = tokens.NextInt();
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
                    geom.Vertices.Add(i);

                }

                // Add the indices (turns into the index buffer) that describe the order in which
                // the vertices are rendered
                int numFaces = tokens.NextInt();
                for (int i = 0; i < numFaces; i++)
                {
                    int numVertsPerFace = tokens.NextInt();

                    // If the current primitive is a triangle, just add the face indices to the 
                    // index buffer
                    if (numVertsPerFace == 3)
                        for (int j = 0; j < 3; j++)
                            geom.Indices.Add(tokens.NextInt());
                    // If it is a quad, split it into two triangles
                    else if (numVertsPerFace == 4)
                    {
                        int startIndex = geom.Indices.Count + 2;
                        for (int j = 0; j < 3; j++)
                            geom.Indices.Add(tokens.NextInt());
                        geom.Indices.Add(geom.Indices[startIndex]);
                        geom.Indices.Add(tokens.NextInt());
                        geom.Indices.Add(geom.Indices[startIndex - 2]);
                    }
                    else throw new Exception("Invalid primitive type: only quads and triangles supported.");

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
                List<Vector2> texCoords = new List<Vector2>();
                for (int i = 0; i < numCoords; i++)
                    texCoords.Add(tokens.NextVector2());

                // end of vertex coordinates
                tokens.SkipToken();
                geom.Vertices.Channels.Add("TextureCoordinate0", texCoords);
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

                // Read all the normals
                int numNormals = tokens.NextInt();
                Vector3[] normals = new Vector3[numNormals];
                for (int i = 0; i < numNormals; i++)
                {
                    normals[i] = tokens.NextVector3();
                    // Reflect each normal across z axis to convert it from left hand to right
                    // hand.  We can do this because the inverse transpose of the reflection
                    // matrix across a principal axis is the same as the reflection matrix.
                    normals[i].Z *= -1;
                }

                // enumerating Lists is faster than enumerating arrays for IEnumerable, which is what
                // is used for adding the normals to the geometry channels
                List<Vector3> vertexNormals = new List<Vector3>(new Vector3[numVerts]);
                int index = 0;
                numFaces = tokens.NextInt();
                for (int i = 0; i < numFaces; i++)
                {
                    int numNormalsPerFace = tokens.NextInt();
                    // Add triangle normals
                    for (int j = 0; j < 3; j++)
                        vertexNormals[geom.Indices[index++]] = normals[tokens.NextInt()];
                    // skip last normal if its a quad since we already split the quad into
                    // two triangeles
                    if (numNormalsPerFace == 4)
                    {
                        tokens.NextInt();
                        index += 3;
                    }
                    else if (numNormalsPerFace != 3)
                        throw new Exception("Invalid primitive type: only quads and triangles supported.");

                    tokens.SkipToken();
                }
                // end of mesh normals
                tokens.SkipToken();
                // Add a channel to the geometry.  This channel will eventually turn into
                // the normals for each vertex.
                geom.Vertices.Channels.Add(VertexElementUsage.Normal.ToString(),
                    vertexNormals);
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
                InitializeGeometry();

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
                int numFaceIndices = tokens.NextInt();

                // skip all the indices and their commas/semicolons since
                // we are just going to apply this material to the entire mesh
                tokens.SkipTokens(numFaceIndices * 2);
                // account for blenders mistake of putting an extra semicolon here
                if (tokens.Peek == ";")
                    tokens.SkipToken();
                for (int i = 0; i < numMaterials; i++)
                {
                    tokens.SkipToken();
                    ImportMaterial();
                }
                // end of material list
                tokens.SkipToken();
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
            private void ImportMaterial()
            {
                BasicMaterialContent m = new BasicMaterialContent();
                // make sure name isn't null
                m.Name = "";
                m.Name = tokens.ReadName();
                // Diffuse color describes how diffuse (directional) light
                // reflects off the mesh
                m.DiffuseColor = new Vector3(tokens.NextFloat(),
                    tokens.NextFloat(), tokens.NextFloat());
                // We dont care about the alpha component of diffuse light.
                // I don't even understand what this is useful for.
                tokens.NextFloat();
                // Specular power is inversely exponentially proportional to the
                // strength of specular light
                m.SpecularPower = tokens.SkipToken().NextFloat();
                // Specular color describes how specular (directional and shiny)
                // light reflects off the mesh
                m.SpecularColor = tokens.NextVector3();
                m.EmissiveColor = tokens.NextVector3();

                // Import any textures associated with this material
                for (string token = tokens.NextToken();
                    token != "}"; )
                {
                    if (token == "TextureFilename")
                    {
                        // Get the absolute path of the texture
                        string fileName = tokens.SkipName().NextString();
                        string absoluteModelPath = Path.GetDirectoryName(Path.GetFullPath(model.fileName));
                        string absoluteTexturePath = Path.Combine(absoluteModelPath, fileName);
                        ExternalReference<TextureContent> reference =
                            new ExternalReference<TextureContent>(absoluteTexturePath);
                        m.Texture = reference;
                        tokens.SkipToken();
                    }
                    else if (token == "{")
                        tokens.SkipNode();
                    token = tokens.NextToken();
                }
                geom.Material = m;

            }
            #endregion

            #region Other Methods
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
                List<Vector4> orderedWeights = new List<Vector4>(new Vector4[skinInfo.Count]);
                List<Byte4> indices = new List<Byte4>(new Byte4[skinInfo.Count]);

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

                    // Add the information to the correct vertex
                    orderedWeights[geom.Vertices.PositionIndices[index]] = w;
                    indices[geom.Vertices.PositionIndices[index]] = ib;
                    index++;
                }

                // Create the channels
                geom.Vertices.Channels.Add<Byte4>(VertexElementUsage.BlendIndices.ToString(),
                    indices);
                geom.Vertices.Channels.Add<Vector4>(VertexElementUsage.BlendWeight.ToString(),
                    orderedWeights);

            }
            #endregion
        }


    }


}
