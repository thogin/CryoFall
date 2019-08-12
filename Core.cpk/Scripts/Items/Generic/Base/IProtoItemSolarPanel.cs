﻿namespace AtomicTorch.CBND.CoreMod.Items.Generic
{
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Resources;

    public interface IProtoItemSolarPanel : IProtoItem, IProtoItemWithDurablity
    {
        ushort DurabilityDecreasePerMinuteWhenInstalled { get; }

        double ElectricityProductionPerSecond { get; }

        ITextureResource ObjectSprite { get; }
    }
}