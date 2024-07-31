using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using XNode;
using Unity.Mathematics;

namespace VoxelDataGeneratorGraph
{
    public abstract class PointGenerator : ProceduralWorldGraphNodeBase
    {
        [Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public int2 output;
        [Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public PointNodeConnection parent;
        [Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)] 
        public PointNodeConnection child;

        public override bool IsRootNode()
        {
            throw new NotImplementedException();
        }
    }
}
