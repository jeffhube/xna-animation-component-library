/*
 * AsfImporter.cs
 * Imports Acclaim AMC (motion capture data) files using the content pipeline.
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using System.IO;
using System.Globalization;
#endregion

namespace Animation.Content
{
    /// <summary>
    /// Imports Acclaim AMC (motion capture data).
    /// For a foo_bar.amc, expects skeleton in a file named foo.asf.
    /// Returns a skeleton with Animations in root bone. 
    /// </summary>
    [ContentImporter(".AMC", CacheImportedData = true, DefaultProcessor = "AnimationProcessor",
        DisplayName="Acclaim AMC - Animation Library")]
    public sealed class AmcImporter : ContentImporter<BoneContent>
    {

        #region Member Variables
        private long ticksPerFrame = TimeSpan.TicksPerSecond/24;
        private NamedValueDictionary<BoneContent> bones;
        private ContentImporterContext context;
        private StreamReader reader;
        private ContentIdentity contentId;
        private IFormatProvider format = (IFormatProvider)CultureInfo.InvariantCulture.NumberFormat;
        private int currentLine = 0;

        private ContentIdentity cId
        {
            get
            {
                contentId.FragmentIdentifier = "line " + currentLine.ToString();
                return contentId;
            }
        }
        #endregion            


        /// <summary>
        /// Imports Acclaim AMC (motion capture data).
        /// For a foo_bar.amc, expects skeleton in a file named foo.asf.
        /// Returns a skeleton with Animations in root bone. 
        /// </summary>
        public override BoneContent Import(string filename, ContentImporterContext context)
        {
            this.context = context;
            contentId = new ContentIdentity(filename);
            reader = new StreamReader(filename);

            AnimationContent animation = new AnimationContent();
            animation.Name = Path.GetFileNameWithoutExtension(filename);
            animation.Identity = contentId;

            string dir=Path.GetDirectoryName(filename);
            string asfFilename=animation.Name.Split('_')[0]+".asf";
            AsfImporter asfImporter = new AsfImporter();
            BoneContent root = asfImporter.Import(dir+@"\"+asfFilename, context);
            context.Logger.LogWarning("", contentId, "using skeleton from {0}", asfFilename);
            bones = asfImporter.Bones;

            int frameNumber = 1;
            int maxFrameNumber = 0;
            string line;
            animation.Channels.Add("root", new AnimationChannel());
            while ((line = readLine()) != null)
            {
                if (line[0]!='#' && line[0]!=':')
                {
                    int fn = 0;
                    if (int.TryParse(line, out fn))
                    {
                        ++frameNumber;// = int.Parse(line);
                        maxFrameNumber = Math.Max(frameNumber, maxFrameNumber);
                        TimeSpan time = TimeSpan.FromTicks(frameNumber * ticksPerFrame);
                        animation.Channels["root"].Add(new AnimationKeyframe(time, Matrix.Identity));
                    }
                    else
                    {
                        string[] s=line.Split(' ');
                        string bone = s[0];
                        if (!animation.Channels.ContainsKey(bone))
                        {
                            animation.Channels.Add(bone, new AnimationChannel());
                        }
                        AnimationChannel channel = animation.Channels[bone];
                        AnimationKeyframe keyframe = importKeyframe(s, frameNumber);
                        if (keyframe!=null) 
                            channel.Add(keyframe);
                    }
                }
            }
            animation.Duration = TimeSpan.FromTicks(maxFrameNumber * ticksPerFrame);
            context.Logger.LogImportantMessage("imported {0} animation frames for {1} bones", 
                maxFrameNumber, animation.Channels.Count);
            root.Animations.Add(animation.Name, animation);
            return root;
        }

        /// <summary>
        ///  Imports a keyframe
        /// </summary>
        private AnimationKeyframe importKeyframe(string[] s, int frameNumber)
        {
            AnimationKeyframe keyframe = null;
            if (!bones.ContainsKey(s[0]))
                throw new InvalidContentException("skeleton does not have bone " + s[0], cId);
            BoneContent bone = bones[s[0]];
            if (bone.OpaqueData.ContainsKey("dof"))
            {
                string[] dof = (string[])bone.OpaqueData["dof"];
                int dataLength = s.Length - 1;
                if (dataLength != dof.Length)
                    throw new InvalidContentException("AFS DOF specifies "+dof.Length
                        +" values but AMC has "+dataLength, cId);
                Matrix transform = Matrix.Identity;
                Vector3 t=new Vector3();
                float rx=0;
                float ry=0;
                float rz=0;
                for (int i = 0; i < dataLength; i++)
                {
                    float data = float.Parse(s[i + 1],format);
                    if (dof[i] == "tx")
                        t.X = data;
                    else if (dof[i] == "ty")
                        t.Y = data;
                    else if (dof[i] == "tz")
                        t.Z = data;
                    else if (dof[i] == "rx")
                        rx = data;
                    else if (dof[i] == "ry")
                        ry = data;
                    else if (dof[i] == "rz")
                        rz = data;
                }
                transform = Matrix.CreateTranslation(t)
                    * Matrix.CreateRotationX(MathHelper.ToRadians(rx))
                    * Matrix.CreateRotationY(MathHelper.ToRadians(ry))
                    * Matrix.CreateRotationZ(MathHelper.ToRadians(rz));
                TimeSpan time = TimeSpan.FromTicks(frameNumber * ticksPerFrame);
                keyframe = new AnimationKeyframe(time, transform);
            }
            return keyframe;
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
