﻿namespace AtomicTorch.CBND.CoreMod.Characters
{
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects;
    using AtomicTorch.CBND.CoreMod.Items.Equipment;
    using AtomicTorch.CBND.CoreMod.Stats;
    using AtomicTorch.CBND.CoreMod.Technologies;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.GameEngine.Common.Extensions;

    public static class SharedCharacterStatsHelper
    {
        public static string GetFullStatTitle(StatName statName)
        {
            var title = statName.GetDescription();

            var attributeSkill = statName.GetAttribute<RelatedToSkillAttribute>();
            var skillName = attributeSkill?.ProtoSkill?.Name;
            if (skillName == null)
            {
                return title;
            }

            // ReSharper disable once CanExtractXamlLocalizableStringCSharp
            return $"({skillName}) {title}";
        }

        public static void RefreshCharacterFinalStatsCache(
            IReadOnlyStatsDictionary protoEffects,
            ICharacterPublicState publicState,
            BaseCharacterPrivateState privateState,
            bool isFirstTime = false)
        {
            var containerEquipment = (publicState as ICharacterPublicStateWithEquipment)?.ContainerEquipment;

            privateState.ContainerEquipmentLastStateHash = containerEquipment?.StateHash;

            FinalStatsCache finalStatsCache;
            using (var tempStatsCache = TempStatsCache.GetFromPool())
            {
                // merge character prototype effects
                tempStatsCache.Merge(protoEffects);

                if (privateState is PlayerCharacterPrivateState playerCharacterPrivateState)
                {
                    // merge skill effects
                    var skills = playerCharacterPrivateState.Skills;
                    skills.SharedFillEffectsCache(tempStatsCache);

                    // merge perks from tech nodes
                    foreach (var techNode in playerCharacterPrivateState.Technologies.Nodes)
                    {
                        foreach (var nodeEffect in techNode.NodeEffects)
                        {
                            if (nodeEffect is TechNodeEffectPerkUnlock perkUnlock)
                            {
                                tempStatsCache.Merge(perkUnlock.Perk.ProtoEffects);
                            }
                        }
                    }
                }

                foreach (var statusEffect in privateState.StatusEffects)
                {
                    var protoStatusEffect = (IProtoStatusEffect)statusEffect.ProtoLogicObject;
                    tempStatsCache.Merge(protoStatusEffect.ProtoEffects);
                }

                if (containerEquipment != null)
                {
                    // merge equipment effects
                    foreach (var item in containerEquipment.Items)
                    {
                        if (item.ProtoGameObject is IProtoItemEquipment protoEquipment
                            && protoEquipment.SharedCanApplyEffects(item, containerEquipment))
                        {
                            tempStatsCache.Merge(protoEquipment.ProtoEffects);
                        }
                    }
                }

                // calculate the final stats cache
                finalStatsCache = tempStatsCache.CalculateFinalStatsCache();
            }

            privateState.FinalStatsCache = finalStatsCache;

            // need to recalculate the weapon cache as it depends on the final cache
            privateState.WeaponState.WeaponCache = null;

            ApplyFinalStatsCache(finalStatsCache, publicState.CurrentStats, isFirstTime);
        }

        private static void ApplyFinalStatsCache(
            FinalStatsCache finalStatsCache,
            CharacterCurrentStats stats,
            bool isFirstTime)
        {
            if (!Api.IsServer)
            {
                return;
            }

            // these values could be set only on the Server-side
            stats.ServerSetHealthMax((float)finalStatsCache[StatName.HealthMax]);
            stats.ServerSetStaminaMax((float)finalStatsCache[StatName.StaminaMax]);

            var playerStats = stats as PlayerCharacterCurrentStats;
            playerStats?.ServerSetFoodMax((float)finalStatsCache[StatName.FoodMax]);
            playerStats?.ServerSetWaterMax((float)finalStatsCache[StatName.WaterMax]);

            if (isFirstTime)
            {
                stats.ServerSetCurrentValuesToMaxValues();
            }
        }
    }
}