﻿namespace AtomicTorch.CBND.CoreMod.Vehicles
{
    using System.Collections.Generic;
    using AtomicTorch.CBND.CoreMod.CharacterSkeletons;
    using AtomicTorch.CBND.CoreMod.ItemContainers.Vehicles;
    using AtomicTorch.CBND.CoreMod.Items;
    using AtomicTorch.CBND.CoreMod.StaticObjects;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Explosives;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.WorldObjects.Vehicle;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.State;
    using AtomicTorch.CBND.GameApi.Data.Weapons;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting.ClientComponents;

    public abstract class ProtoVehicleMech
        <TVehiclePrivateState,
         TVehiclePublicState,
         TVehicleClientState>
        : ProtoVehicle
            <TVehiclePrivateState,
                TVehiclePublicState,
                TVehicleClientState>
        where TVehiclePrivateState : VehicleMechPrivateState, new()
        where TVehiclePublicState : VehicleMechPublicState, new()
        where TVehicleClientState : VehicleClientState, new()
    {
        public abstract BaseItemsContainerMechEquipment EquipmentItemsContainerType { get; }

        public override bool IsHealthbarDisplayedWhenPiloted => true;

        public override bool IsHeavyVehicle => true;

        public override ITextureResource MapIcon => new TextureResource("Icons/MapExtras/VehicleMech");

        public override float ObjectSoundRadius => 2;

        public override SoundResource SoundResourceLightsToggle { get; }
            = new SoundResource("Objects/Vehicles/Mech/ToggleLight");

        public override SoundResource SoundResourceVehicleDismount { get; }
            = new SoundResource("Objects/Vehicles/Mech/Dismount");

        public override SoundResource SoundResourceVehicleMount { get; }
            = new SoundResource("Objects/Vehicles/Mech/Mount");

        public override void ClientOnVehicleDismounted(IDynamicWorldObject vehicle)
        {
            base.ClientOnVehicleDismounted(vehicle);

            var skeletonRenderer = GetClientState(vehicle).SkeletonRenderer;
            if (skeletonRenderer is null)
            {
                return;
            }

            // switch animation to idle and then to offline to ensure both are played
            skeletonRenderer.SetAnimationFrame(0, "Idle", timePositionFraction: 0);
            skeletonRenderer.SetAnimation(0, "Offline", isLooped: false);
        }

        public override BaseUserControlWithWindow ClientOpenUI(IWorldObject worldObject)
        {
            var privateState = GetPrivateState((IDynamicWorldObject)worldObject);
            return WindowObjectVehicle.Open((IDynamicWorldObject)worldObject,
                                            vehicleExtraControl: new ControlMechEquipment(privateState));
        }

        public override void ServerOnDestroy(IDynamicWorldObject gameObject)
        {
            base.ServerOnDestroy(gameObject);

            // try drop extra containers on the ground
            var privateState = GetPrivateState(gameObject);
            DropItemsToTheGround(privateState.EquipmentItemsContainer);
            //DropItemsToTheGround(privateState.FuelItemsContainer); // consider fuel items were destroyed during the explosion

            void DropItemsToTheGround(IItemsContainer itemsContainer)
            {
                if (itemsContainer?.OccupiedSlotsCount == 0)
                {
                    return;
                }

                var groundContainer = ObjectGroundItemsContainer.ServerTryDropOnGroundContainerContent(
                    gameObject.Tile,
                    itemsContainer);

                if (groundContainer != null)
                {
                    // set custom timeout for the dropped ground items container
                    ObjectGroundItemsContainer.ServerSetDestructionTimeout(
                        (IStaticWorldObject)groundContainer.Owner,
                        DestroyedCargoDroppedItemsDestructionTimeout.TotalSeconds);
                }
            }
        }

        public override IItemsContainer SharedGetHotbarItemsContainer(IDynamicWorldObject vehicle)
        {
            return GetPrivateState(vehicle).EquipmentItemsContainer;
        }

        protected override void ClientInitializeVehicle(ClientInitializeData data)
        {
            base.ClientInitializeVehicle(data);

            var vehicle = data.GameObject;
            var publicState = data.PublicState;
            var clientState = data.ClientState;

            // re-create rendering when turret items changed
            publicState.ClientSubscribe(c => c.ProtoItemLeftTurretSlot,
                                        _ => vehicle.ClientInitialize(),
                                        clientState);

            publicState.ClientSubscribe(c => c.ProtoItemRightTurretSlot,
                                        _ => vehicle.ClientInitialize(),
                                        clientState);
        }

        protected override void ClientSetupRendering(ClientInitializeData data)
        {
            base.ClientSetupRendering(data);

            var publicState = data.PublicState;

            var skeletonRenderer = data.ClientState.SkeletonRenderer;
            if (skeletonRenderer is null)
            {
                return;
            }

            this.SharedGetSkeletonProto(data.GameObject, out var protoSkeleton, out _);
            SetupItem(publicState.ProtoItemLeftTurretSlot);
            SetupItem(publicState.ProtoItemRightTurretSlot);

            skeletonRenderer.SetAnimationFrame(0, "Offline", timePositionFraction: 1);

            void SetupItem(IProtoItem protoItem)
            {
                if (protoItem is IProtoItemWithCharacterAppearance protoItemWithCharacterAppearance)
                {
                    protoItemWithCharacterAppearance.ClientSetupSkeleton(
                        item: null,
                        character: null,
                        (ProtoCharacterSkeleton)protoSkeleton,
                        skeletonRenderer,
                        skeletonComponents: new List<IClientComponent>());
                }
            }
        }

        protected override void PrepareProtoVehicleDestroyedExplosionPreset(
            out double damageRadius,
            out ExplosionPreset explosionPreset,
            out DamageDescription damageDescriptionCharacters)
        {
            damageRadius = 6;
            explosionPreset = ExplosionPresets.VeryLarge;

            damageDescriptionCharacters = new DamageDescription(
                damageValue: 100,
                armorPiercingCoef: 0,
                finalDamageMultiplier: 1,
                rangeMax: damageRadius,
                damageDistribution: new DamageDistribution(DamageType.Kinetic, 1));
        }

        protected override void ServerInitializeVehicle(ServerInitializeData data)
        {
            base.ServerInitializeVehicle(data);

            var worldObject = data.GameObject;
            var privateState = data.PrivateState;

            // setup equipment items container
            var equipmentItemsContainer = privateState.EquipmentItemsContainer;
            var equipmentItemsSlotsCount = this.EquipmentItemsContainerType.TotalSlotsCount;
            if (equipmentItemsContainer != null)
            {
                // container already created - update it
                Server.Items.SetContainerType(equipmentItemsContainer, this.EquipmentItemsContainerType);
                Server.Items.SetSlotsCount(equipmentItemsContainer,
                                           slotsCount: equipmentItemsSlotsCount);
            }
            else
            {
                equipmentItemsContainer = Server.Items.CreateContainer(
                    owner: worldObject,
                    itemsContainerType: this.EquipmentItemsContainerType,
                    slotsCount: equipmentItemsSlotsCount);

                privateState.EquipmentItemsContainer = equipmentItemsContainer;
            }
        }

        protected override void ServerUpdateVehicle(ServerUpdateData data)
        {
            var privateState = data.PrivateState;
            var publicState = data.PublicState;

            publicState.ProtoItemLeftTurretSlot = privateState.EquipmentItemsContainer.GetItemAtSlot(0)?.ProtoItem;
            // not used yet
            //publicState.ProtoItemSlotRightTurret = privateState.EquipmentItemsContainer.GetItemAtSlot(1)?.ProtoItem;
        }

        protected override void SharedSetupCurrentPlayerUI(IDynamicWorldObject vehicle)
        {
            base.SharedSetupCurrentPlayerUI(vehicle);
            GetClientState(vehicle).UIElementsHolder = new ClientVehicleMechCurrentPlayerUIController(vehicle);
        }
    }

    public abstract class ProtoVehicleMech
        : ProtoVehicleMech
            <VehicleMechPrivateState,
                VehicleMechPublicState,
                VehicleClientState>
    {
    }
}