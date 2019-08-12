﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Misc
{
    using System;
    using System.Collections.Generic;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.CoreMod.ItemContainers;
    using AtomicTorch.CBND.CoreMod.Items;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.Skills;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Creative;
    using AtomicTorch.CBND.CoreMod.Systems.InteractionChecker;
    using AtomicTorch.CBND.CoreMod.Systems.ItemDurability;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.WorldObjects.TinkerTable;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.WorldObjects.TinkerTable.Data;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.State;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting.Network;
    using AtomicTorch.GameEngine.Common.Helpers;
    using AtomicTorch.GameEngine.Common.Primitives;

    public class ObjectTinkerTable
        : ProtoObjectStructure
          <ObjectTinkerTable.PrivateState,
              StaticObjectPublicState,
              StaticObjectClientState>,
          IInteractableProtoStaticWorldObject
    {
        public const string ErrorMessage_ComponentItemsRequried = "Need more components.";

        public const string ErrorMessage_Input = "Place two identical items to repair.";

        public const string ErrorMessage_NoRepairNecessary = "No repair necessary.";

        public const string ErrorMessage_OutputIsFull = "Remove the item from the output slot first.";

        public const string ErrorTitle = "Cannot repair";

        private static ObjectTinkerTable instance;

        private static readonly Lazy<IReadOnlyList<ProtoItemWithCount>> LazyRequiredRepairComponentItems
            = new Lazy<IReadOnlyList<ProtoItemWithCount>>(SharedSetupRequriedRepairComponents);

        public static IReadOnlyList<ProtoItemWithCount> RequiredRepairComponentItems
            => LazyRequiredRepairComponentItems.Value;

        public override string Description =>
            "This work station is especially convenient to tinker with or repair different items.";

        public bool IsAutoEnterPrivateScopeOnInteraction => true;

        public override string Name => "Tinker table";

        public override ObjectSoundMaterial ObjectSoundMaterial => ObjectSoundMaterial.Metal;

        public override double ObstacleBlockDamageCoef => 1;

        public override float StructurePointsMax => 500;

        public static void ClientRepair(IStaticWorldObject tinkerTableObject)
        {
            if (ValidateCanRepair(Client.Characters.CurrentPlayerCharacter,
                                  tinkerTableObject,
                                  out var errorMessage))
            {
                instance.CallServer(_ => _.ServerRemote_Repair());
                return;
            }

            NotificationSystem.ClientShowNotification(
                ErrorTitle,
                errorMessage,
                NotificationColor.Bad,
                icon: instance.Icon);
        }

        public static double SharedCalculateResultDurabilityFraction(
            IItem inputItem1,
            IItem inputItem2,
            ICharacter character)
        {
            var result = ItemDurabilitySystem.SharedGetDurabilityPercent(inputItem1)
                         + ItemDurabilitySystem.SharedGetDurabilityPercent(inputItem2)
                         + SkillMaintenance.SharedGetCurrentBonusPercent(character);

            result = MathHelper.Clamp(result, 0, 100);
            return result / 100.0;
        }

        public static bool ValidateCanRepair(
            ICharacter character,
            IStaticWorldObject tinkerTableObject,
            out string errorMessage)
        {
            var worldObjectPrivateState = GetPrivateState(tinkerTableObject);
            var containerInput = worldObjectPrivateState.ContainerInput;
            var containerOutput = worldObjectPrivateState.ContainerOutput;

            var inputItem1 = containerInput.GetItemAtSlot(0);
            var inputItem2 = containerInput.GetItemAtSlot(1);

            if (inputItem1 == null
                || inputItem2 == null
                || inputItem1.ProtoItem != inputItem2.ProtoItem)
            {
                errorMessage = ErrorMessage_Input;
                return false;
            }

            var resultItemProto = (IProtoItemWithDurablity)inputItem1.ProtoGameObject;
            if (!resultItemProto.IsRepairable)
            {
                throw new Exception("Cannot repair: the input items prototype is not repairable");
            }

            var durabilityItem1 = ItemDurabilitySystem.SharedGetDurabilityFraction(inputItem1);
            var durabilityItem2 = ItemDurabilitySystem.SharedGetDurabilityFraction(inputItem2);

            if (durabilityItem1 >= 1.0
                || durabilityItem2 >= 1.0)
            {
                // one of the items is 100% green
                errorMessage = ErrorMessage_NoRepairNecessary;
                return false;
            }

            var thresholdGreen = ItemDurabilitySystem.ThresholdFractionGreenStatus;
            if (durabilityItem1 >= thresholdGreen
                && durabilityItem2 >= thresholdGreen)
            {
                // both items are green
                errorMessage = ErrorMessage_NoRepairNecessary;
                return false;
            }

            if (!SharedValidateHasRequiredComponentItems(character))
            {
                errorMessage = ErrorMessage_ComponentItemsRequried;
                return false;
            }

            if (containerOutput.GetItemAtSlot(0) != null)
            {
                errorMessage = ErrorMessage_OutputIsFull;
                return false;
            }

            errorMessage = null;
            return true;
        }

        public override Vector2D SharedGetObjectCenterWorldOffset(IWorldObject worldObject)
        {
            return (1, 0.65);
        }

        BaseUserControlWithWindow IInteractableProtoStaticWorldObject.ClientOpenUI(IStaticWorldObject worldObject)
        {
            return this.ClientOpenUI(new ClientObjectData(worldObject));
        }

        void IInteractableProtoStaticWorldObject.ServerOnClientInteract(ICharacter who, IStaticWorldObject worldObject)
        {
        }

        void IInteractableProtoStaticWorldObject.ServerOnMenuClosed(ICharacter who, IStaticWorldObject worldObject)
        {
        }

        protected override void ClientInteractStart(ClientObjectData data)
        {
            InteractableStaticWorldObjectHelper.ClientStartInteract(data.GameObject);
        }

        protected BaseUserControlWithWindow ClientOpenUI(ClientObjectData data)
        {
            return WindowTinkerTable.Open(
                new ViewModelWindowTinkerTable(data.GameObject,
                                               data.PrivateState));
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("##");
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryOther>();

            build.StagesCount = 10;
            build.StageDurationSeconds = BuildDuration.Short;
            build.AddStageRequiredItem<ItemPlanks>(count: 10);
            build.AddStageRequiredItem<ItemIngotSteel>(count: 2);

            repair.StagesCount = 10;
            repair.StageDurationSeconds = BuildDuration.Short;
            repair.AddStageRequiredItem<ItemPlanks>(count: 5);
        }

        protected override void PrepareProtoStaticWorldObject()
        {
            base.PrepareProtoStaticWorldObject();
            instance = this;
        }

        protected override void ServerInitialize(ServerInitializeData data)
        {
            base.ServerInitialize(data);

            var worldObject = data.GameObject;
            var privateState = data.PrivateState;

            if (privateState.ContainerInput == null)
            {
                privateState.ContainerInput =
                    Server.Items.CreateContainer<ItemsContainerTinkerTableInput>(worldObject,
                                                                                 slotsCount: 2);
            }

            if (privateState.ContainerOutput == null)
            {
                privateState.ContainerOutput =
                    Server.Items.CreateContainer<ItemsContainerOutput>(worldObject,
                                                                       slotsCount: 1);
            }
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle((1.9, 0.8), offset: (0.05, 0))
                .AddShapeRectangle((2, 1),     offset: (0, 0),   group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle((1.6, 0.25), offset: (0.2, 0.9), group: CollisionGroups.HitboxRanged)
                .AddShapeRectangle((2, 1.2),   offset: (0, 0),   group: CollisionGroups.ClickArea);
        }

        private static IReadOnlyList<ProtoItemWithCount> SharedSetupRequriedRepairComponents()
        {
            var items = new InputItems();
            items.Add<ItemDuctTape>(count: 1);
            return items.AsReadOnly();
        }

        private static bool SharedValidateHasRequiredComponentItems(ICharacter character)
        {
            if (CreativeModeSystem.SharedIsInCreativeMode(character))
            {
                return true;
            }

            foreach (var requiredItem in RequiredRepairComponentItems)
            {
                if (!character.ContainsItemsOfType(requiredItem.ProtoItem, requiredItem.Count))
                {
                    // some item is not available
                    return false;
                }
            }

            return true;
        }

        private void ServerRemote_Repair()
        {
            var character = ServerRemoteContext.Character;
            var tinkerTableObject =
                InteractionCheckerSystem.SharedGetCurrentInteraction(character) as IStaticWorldObject;
            this.VerifyGameObject(tinkerTableObject);

            var worldObjectPrivateState = GetPrivateState(tinkerTableObject);
            var containerInput = worldObjectPrivateState.ContainerInput;
            var containerOutput = worldObjectPrivateState.ContainerOutput;
            var inputItem1 = containerInput.GetItemAtSlot(0);
            var inputItem2 = containerInput.GetItemAtSlot(1);

            if (!ValidateCanRepair(character, tinkerTableObject, out var error))
            {
                Logger.Warning(tinkerTableObject + " cannot repair: " + error, character);
                return;
            }

            if (!CreativeModeSystem.SharedIsInCreativeMode(character))
            {
                InputItemsHelper.ServerDestroyItems(character, RequiredRepairComponentItems);
            }

            var resultDurabilityFraction = SharedCalculateResultDurabilityFraction(inputItem1, inputItem2, character);

            Server.Items.DestroyItem(inputItem2);
            Server.Items.MoveOrSwapItem(inputItem1,
                                        containerOutput,
                                        out _);

            var resultItemProto = (IProtoItemWithDurablity)inputItem1.ProtoGameObject;
            var resultItemPrivateState = inputItem1.GetPrivateState<IItemWithDurabilityPrivateState>();
            resultItemPrivateState.DurabilityCurrent = (uint)Math.Round(
                resultDurabilityFraction * resultItemProto.DurabilityMax,
                MidpointRounding.AwayFromZero);

            character.ServerAddSkillExperience<SkillMaintenance>(
                SkillMaintenance.ExperiencePerItemRepaired);

            Logger.Info(
                $"Item repaired: {inputItem1}. Second item was destroyed to use for repair components: {inputItem2}");
        }

        public class PrivateState : StructurePrivateState
        {
            [SyncToClient]
            public IItemsContainer ContainerInput { get; set; }

            [SyncToClient]
            public IItemsContainer ContainerOutput { get; set; }
        }
    }
}