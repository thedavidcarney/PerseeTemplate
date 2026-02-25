using UnityEngine;
using System.Collections.Generic;

namespace DepthProcessing
{
    public abstract class StylePass : DepthPass
    {
        public int OutputWidth { get; protected set; } = 1920;
        public int OutputHeight { get; protected set; } = 1080;
    }
}