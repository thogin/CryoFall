﻿namespace AtomicTorch.CBND.CoreMod.Items.Seeds
{
    using System.Collections.Generic;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Farms;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Vegetation;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Vegetation.Plants;

    public class ItemSeedsChiliPepper : ProtoItemSeed
    {
        public override string Description => GetProtoEntity<ItemSeedsBellPepper>().Description;

        public override string Name => "Chili pepper seeds";

        protected override void PrepareProtoItemSeed(
            out IProtoObjectVegetation objectPlantProto,
            List<IProtoObjectFarm> allowedToPlaceAt)
        {
            objectPlantProto = GetPlant<ObjectPlantChiliPepper>();

            allowedToPlaceAt.Add(GetPlot<ObjectFarmPlot>());
        }
    }
}