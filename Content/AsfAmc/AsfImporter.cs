/*
 * AsfImporter.cs
 * Imports Acclaim ASF (motion capture skeleton) using the content pipeline.
 * Part of XNA Animation Component library, which is a library for animation
 * in XNA
 * 
 * Copyright (C) 2006 Michael Nikonov
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
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.Xml;
#endregion

namespace Animation.Content
{
    /// <summary>
    /// Imports Acclaim ASF (motion capture skeleton).
    /// Stores DOF spec (degrees of freedom) as string tagged "dof" in OpaqueData for each bone.
    /// </summary>
    [ContentImporter(".ASF", CacheImportedData = true, DefaultProcessor = "AnimationProcessor",
        DisplayName = "Acclaim ASF - Animation Library")]
    public sealed class AsfImporter : ContentImporter<BoneContent>
    {
        private ContentImporterContext context;
        private StreamReader reader;
        private NamedValueDictionary<BoneContent> bones;
        private ContentIdentity contentId;
        private int currentLine=0;

        private ContentIdentity cId
        {
            get
            {
                contentId.FragmentIdentifier = "line " + currentLine.ToString();
                return contentId;
            }
        }
        public NamedValueDictionary<BoneContent> Bones
        {
            get { return bones; }
        }

        /// <summary>
        /// Imports Acclaim AFS (motion capture skeleton).
        /// Stores dof spec (degrees of freedom) as string in OpaqueData for each bone.
        /// </summary>
        public override BoneContent Import(string filename, ContentImporterContext context)
        {
            this.context = context;
            contentId = new ContentIdentity(filename);
            BoneContent root = new BoneContent();
            root.Name = "root";
            root.Identity = cId;
            root.Transform = Matrix.Identity;
            bones = new NamedValueDictionary<BoneContent>();
            bones.Add("root", root);
            reader = new StreamReader(filename);
            String line;
            while ((line = readLine()) != ":bonedata")
            {
                if (line == null) throw new InvalidContentException("no bone data found", cId);
            }
            BoneContent bone;
            while ((bone=importBone()) != null)
            {
                bones.Add(bone.Name, bone);
            }
            importHierarchy();
            foreach (BoneContent b in bones.Values)
            {
                if (b.Name != "root" && b.Parent == null)
                {
                    throw new InvalidContentException("incomplete hierarchy - bone " + b + " has no parent", cId);
                }
            }
            return root;
        }

        private string readLine()
        {
            string line=reader.ReadLine();
            ++currentLine;
            if (line != null) 
                line=line.Trim();
            return line;
        }

        private BoneContent importBone()
        {
            /* example (dof and limits are optional): 
             * 
             *   begin
                     id 2 
                     name lfemur
                     direction 0.34202 -0.939693 0  
                     length 7.16147  
                     axis 0 0 20  XYZ
                    dof rx ry rz
                    limits (-160.0 20.0)
                           (-70.0 70.0)
                           (-60.0 70.0)
                  end
                */
            BoneContent bone = new BoneContent();
            String line;
            line = readLine();
            if (line == ":hierarchy") 
                return null;
            if (line!="begin")
                throw new InvalidContentException("no hierarchy found", cId);
            line = readLine().Trim(); //id 1
            bone.Name = importStrings("name")[0];
            bone.Identity = cId;
            float[] direction = importFloats("direction");
            float length = importFloats("length")[0];
            string[] axis = importStrings("axis");

            // now skip optional "limits" as we don't need them
            for (int i = 0; i < 5; i++)
            {
                line = readLine();
                if (line == null)
                    throw new InvalidContentException("melformed bone data for bone " + bone.Name, cId);
                if (line == "end")
                    break;
                if (line.Contains("dof "))
                {
                    string[] dof = line.Substring(4).Split(' ');
                    bone.OpaqueData.Add("dof", dof);
                }
            }
            Vector3 v = new Vector3(direction[0], direction[1], direction[2]);
            Matrix m = Matrix.Identity;
            m.Forward = v;
            m.Translation = v * length;
            bone.Transform = m;
            return bone;
        }

        private string[] importStrings(string keyword)
        {
            string line = readLine();
            if (line == null)
                throw new InvalidContentException("premature end of file", cId);
            string[] tokens = line.Split(' ');
            if (tokens[0] != keyword)
                throw new InvalidContentException("expected '" + keyword + "' but found '" + tokens[0]+"'", cId);
            return line.Substring(tokens[0].Length + 1).Trim().Split(' ');
        }

        private float[] importFloats(string keyword)
        {
            string[] strings = importStrings(keyword);
            float[] f=new float[strings.Length];
            for (int i = 0; i < strings.Length; i++)
            {
                f[i] = float.Parse(strings[i]);
            }
            return f;
        }

        private void importHierarchy()
        {
            string line;
            line = readLine(); //begin
            while ((line = readLine()) != null)
            {
                if (line == "end")
                    break;
                string[] s = line.Split(' ');
                string parent = s[0];
                if (!bones.ContainsKey(parent))
                    throw new InvalidContentException("unknown bone "+parent, cId);
                string[] children = line.Substring(parent.Length+1).Split(' ');
                foreach(string child in children)
                {
                    if (!bones.ContainsKey(child))
                        throw new InvalidContentException("unknown bone " + child, cId);
                    bones[parent].Children.Add(bones[child]);
                }
            }
        }
    }
}
