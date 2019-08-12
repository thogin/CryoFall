﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Deposits
{
    using AtomicTorch.CBND.GameApi.Data.World;

    public interface IProtoObjectDeposit : IProtoStaticWorldObject
    {
        double DecaySpeedMultiplierWhenExtractingActive { get; }

        /// <summary>
        /// Total lifetime of the deposit.
        /// Could be zero in case of infinite deposit.
        /// </summary>
        double LifetimeTotalDurationSeconds { get; }

        void ServerOnExtractorDestroyedForDeposit(IStaticWorldObject worldObjectDeposit);
    }
}