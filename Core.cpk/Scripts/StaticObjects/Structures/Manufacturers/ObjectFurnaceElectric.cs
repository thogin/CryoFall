﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Manufacturers
{
    using AtomicTorch.CBND.CoreMod.ClientComponents.Rendering.Lighting;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.State;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;
    using AtomicTorch.GameEngine.Common.Primitives;

    public class ObjectFurnaceElectric : ProtoObjectManufacturer
    {
        private const float VerticalOffset = 0.25f; // vertical offset of the entire thing

        private readonly TextureResource textureFurnaceActive;

        public ObjectFurnaceElectric()
        {
            this.textureFurnaceActive = new TextureResource(
                this.GenerateTexturePath() + "Active",
                isTransparent: false);
        }

        public override byte ContainerFuelSlotsCount => 0;

        public override byte ContainerInputSlotsCount => 8;

        public override byte ContainerOutputSlotsCount => 4;

        public override string Description =>
            "Uses electricity and induction heating method for faster smelting and resource processing.";

        public override double ElectricityConsumptionPerSecondWhenActive => 4;

        public override bool IsAutoSelectRecipe => false;

        public override bool IsFuelProduceByproducts => false;

        public override double ManufacturingSpeedMultiplier =>
            2; // twice as fast, but we have to accomodate for this increased speed by increasing energy usage compared to regular furnace

        public override string Name => "Electric furnace";

        public override ObjectSoundMaterial ObjectSoundMaterial => ObjectSoundMaterial.Metal;

        public override double ObstacleBlockDamageCoef => 1.0;

        public override float StructurePointsMax => 1500;

        protected override void ClientInitialize(ClientInitializeData data)
        {
            base.ClientInitialize(data);

            var publicState = data.PublicState;

            var worldObject = data.GameObject;
            var sceneObject = Client.Scene.GetSceneObject(worldObject);

            // setup light source
            var lightSource = ClientLighting.CreateLightSourceSpot(
                Client.Scene.GetSceneObject(worldObject),
                color: LightColors.WoodFiring,
                size: 3.5,
                spritePivotPoint: (0.5, 0.5),
                positionOffset: (1.12, 0.51 + VerticalOffset));

            Vector2D animatedSpritePositionOffset = (195 / 256.0,
                                                     VerticalOffset + 83 / 256.0);

            var clientState = worldObject.GetClientState<StaticObjectClientState>();

            var overlayRenderer = Client.Rendering.CreateSpriteRenderer(
                sceneObject,
                this.textureFurnaceActive,
                DrawOrder.Default,
                positionOffset: animatedSpritePositionOffset,
                spritePivotPoint: Vector2D.Zero);
            overlayRenderer.DrawOrderOffsetY = VerticalOffset - animatedSpritePositionOffset.Y - 0.01;

            var soundEmitter = this.ClientCreateActiveStateSoundEmitterComponent(worldObject);
            soundEmitter.Volume = 0.7f;

            publicState.ClientSubscribe(
                s => s.IsActive,
                callback: RefreshActiveState,
                subscriptionOwner: clientState);

            RefreshActiveState(publicState.IsActive);

            void RefreshActiveState(bool isActive)
            {
                overlayRenderer.IsEnabled = isActive;
                lightSource.IsEnabled = isActive;
                soundEmitter.IsEnabled = isActive;
            }
        }

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY = VerticalOffset;
            renderer.PositionOffset = (0, VerticalOffset);
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("##",
                         "##");
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryIndustry>();

            build.StagesCount = 10;
            build.StageDurationSeconds = BuildDuration.Medium;
            build.AddStageRequiredItem<ItemWire>(count: 5);
            build.AddStageRequiredItem<ItemIngotSteel>(count: 4);
            build.AddStageRequiredItem<ItemPlastic>(count: 1);
            build.AddStageRequiredItem<ItemComponentsElectronic>(count: 1);

            repair.StagesCount = 10;
            repair.StageDurationSeconds = BuildDuration.Medium;
            repair.AddStageRequiredItem<ItemWire>(count: 5);
            repair.AddStageRequiredItem<ItemIngotSteel>(count: 2);
            repair.AddStageRequiredItem<ItemPlastic>(count: 1);
        }

        protected override void PrepareDefense(DefenseDescription defense)
        {
            defense.Set(ObjectDefensePresets.Tier1);
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle(size: (1.8, 1.2),
                                   offset: (0.1, 0 + VerticalOffset))
                .AddShapeRectangle(size: (1.6, 1.65),
                                   offset: (0.2, 0.1 + VerticalOffset),
                                   group: CollisionGroups.HitboxMelee)
                .AddShapeRectangle(size: (1.6, 0.6),
                                   offset: (0.2, 0.85 + VerticalOffset),
                                   group: CollisionGroups.HitboxRanged)
                .AddShapeRectangle(size: (1.6, 1.65),
                                   offset: (0.2, 0.1 + VerticalOffset),
                                   group: CollisionGroups.ClickArea);
        }
    }
}