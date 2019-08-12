﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Props.Pool
{
    public class ObjectPropPoolL : ProtoObjectProp
    {
        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            AddRectangleWithHitboxes(data, size: (1, 1));
        }
    }
}