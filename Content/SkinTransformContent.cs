using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace XCLNA.XNA.Animation.Content
{
    public struct SkinTransformContent
    {
        /// <summary>
        /// The name of the bone attached to the transform
        /// </summary>
        public string BoneName;
        /// <summary>
        /// The transform for the bone
        /// </summary>
        public Matrix Transform;
    }
}
