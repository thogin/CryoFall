﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Props.Bunker
{
    using AtomicTorch.CBND.GameApi.Data.World;

    public class ObjectPropBunkerBL : ProtoObjectProp
    {
        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("#",
                         "#");
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            AddRectangleWithHitboxes(data, size: (0.6, 2), offset: (0.4, 0));
        }
    }
}