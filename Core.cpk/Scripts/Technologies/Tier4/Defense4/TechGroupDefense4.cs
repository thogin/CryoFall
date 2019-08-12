﻿namespace AtomicTorch.CBND.CoreMod.Technologies.Tier4.Defense4
{
    using AtomicTorch.CBND.CoreMod.Technologies.Tier3.Chemistry2;
    using AtomicTorch.CBND.CoreMod.Technologies.Tier3.Defense3;
    using AtomicTorch.CBND.CoreMod.Technologies.Tier3.Industry3;

    public class TechGroupDefense4 : TechGroup
    {
        public override string Description => "High-tech protection technologies.";

        public override string Name => "Defense 4";

        public override TechTier Tier => TechTier.Tier4;

        protected override void PrepareTechGroup(Requirements requirements)
        {
            requirements.AddGroup<TechGroupDefense3>(completion: 1);
            requirements.AddGroup<TechGroupIndustry3>(completion: 0.2);
            requirements.AddGroup<TechGroupChemistry2>(completion: 0.2);
        }
    }
}