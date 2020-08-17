using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;

namespace NextLevelLibrary
{
    public class SkeletonFormat : STSkeleton
    {
        public Dictionary<uint, int> BoneHashToID = new Dictionary<uint, int>();
    }
}
