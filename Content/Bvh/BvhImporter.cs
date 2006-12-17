/*
 * BvhImporter.cs
 * Copyright (c) 2006 Michael Nikonov
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
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.Xml;
using System.Globalization;
#endregion

namespace Animation.Content
{
    /// <summary>
    /// Imports BVH (Biovision hierarchical) animation data.
    /// </summary>
    [ContentImporter(".BVH", CacheImportedData = true, DefaultProcessor = "AnimationProcessor",
        DisplayName = "BVH - Animation Library")]
    public sealed class BvhImporter : ContentImporter<BoneContent>
    {
        private ContentImporterContext context;
        private StreamReader reader;
        private NamedValueDictionary<BoneContent> bones;
        private ContentIdentity contentId;
        private int currentLine = 0;
        private BoneContent root;
        private BoneContent bone;
        int frames = 0;
        double frameTime = 0.0;
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
        /// Imports BVH (Biovision hierarchical) animation data.
        /// Stores animation data in root bone.
        /// </summary>
        public override BoneContent Import(string filename, ContentImporterContext context)
        {

            CultureInfo culture = new CultureInfo("en-US");
            System.Threading.Thread currentThread = System.Threading.Thread.CurrentThread;
            currentThread.CurrentCulture = culture;
            currentThread.CurrentUICulture = culture;
            this.context = context;
            contentId = new ContentIdentity(filename);

            AnimationContent animation = new AnimationContent();
            animation.Name = Path.GetFileNameWithoutExtension(filename);
            animation.Identity = contentId;

            bones = new NamedValueDictionary<BoneContent>();
            reader = new StreamReader(filename);
            String line;
            if ((line = readLine()) != "HIERARCHY")
            {
                throw new InvalidContentException("no HIERARCHY found", cId);
            }
            bone = root;
            while ((line = readLine()) != "MOTION")
            {
                if (line == null)
                    throw new InvalidContentException("premature end of file", cId);
                string keyword = line.Split(' ')[0];
                if (keyword == "ROOT" || keyword == "JOINT" || line == "End Site")
                {
                    BoneContent newBone = new BoneContent();
                    if (keyword == "JOINT" || line == "End Site")
                    {
                        bone.Children.Add(newBone);
                    }
                    if (keyword == "ROOT")
                    {
                        root = newBone;
                    }
                    if (keyword == "ROOT" || keyword == "JOINT")
                    {
                        newBone.Name = line.Split(' ')[1];
                        bones.Add(newBone.Name, newBone);
                    }
                    else
                    {
                        newBone.Name = bone.Name + "End";
                    }
                    bone = newBone;
                    if ((line = readLine()) != "{")
                    {
                        throw new InvalidContentException("expected '{' but found " + line, cId);
                    }
                }
                if (line == "}")
                {
                    bone = (BoneContent)bone.Parent;
                }
                if (keyword == "OFFSET")
                {
                    string[] data = line.Split(' ');
                    //couldn't get the .NET 2.0 version of Split() working, 
                    //therefore this ugly hack
                    List<string> coords = new List<string>();
                    foreach (string s in data) {
                        if (s!="OFFSET" && s!="" && s!=null) {
                            coords.Add(s);
                        }
                    }
                    Vector3 v = new Vector3();
                    v.X = float.Parse(coords[0]);
                    v.Y = float.Parse(coords[1]);
                    v.Z = float.Parse(coords[2]);
                    Matrix offset = Matrix.CreateTranslation(v);
                    bone.Transform = offset;
                }
                if (keyword == "CHANNELS")
                {
                    //for now, we assume that channels are rx,ry,rz for each bone except for root
                }
                if (bone != null && line != "End Site")
                {
                }
            }
            if ((line = readLine()) != null)
            {
                string[] data = line.Split(':');
                if (data[0] == "Frames")
                {
                    frames = int.Parse(data[1]);
                }
            }
            if ((line = readLine()) != null)
            {
                string[] data = line.Split(':');
                if (data[0] == "Frame Time")
                {
                    frameTime = double.Parse(data[1]);
                }
            }
            root.Animations.Add(animation.Name, animation);
            foreach (BoneContent b in bones.Values)
            {
                animation.Channels.Add(b.Name, new AnimationChannel());
            }
            int frameNumber = 1;
            while ((line = readLine()) != null)
            {
                string[] ss = line.Split(' ');
                //couldn't get the .NET 2.0 version of Split() working, 
                //therefore this ugly hack
                List<string> data = new List<string>();
                foreach (string s in ss)
                {
                    if (s != "" && s != null)
                    {
                        data.Add(s);
                    }
                }
                Vector3 rootTranslation = new Vector3();
                rootTranslation.X = float.Parse(data[0]);
                rootTranslation.Y = float.Parse(data[1]);
                rootTranslation.Z = float.Parse(data[2]);
                int i=1;
                foreach (BoneContent b in bones.Values)
                {
                    Matrix m = Matrix.CreateRotationX(float.Parse(data[i*3]))
                        * Matrix.CreateRotationY(float.Parse(data[i*3 + 1]))
                        * Matrix.CreateRotationZ(float.Parse(data[i*3 + 2]));
                    if (b == root)
                    {
                        m = Matrix.CreateTranslation(rootTranslation) * m;
                    }
                    TimeSpan time=TimeSpan.FromSeconds(frameTime * frameNumber);
                    AnimationKeyframe keyframe = new AnimationKeyframe(time, m);
                    animation.Channels[b.Name].Add(keyframe);
                    ++i;
                }
                ++frameNumber;
            }
            return root;
        }

        private string readLine()
        {
            string line = reader.ReadLine();
            ++currentLine;
            if (line != null)
                line = line.Trim();
            return line;
        }
    }
}
