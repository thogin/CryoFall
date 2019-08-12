﻿namespace AtomicTorch.CBND.CoreMod.ClientComponents.Misc
{
    using System;
    using AtomicTorch.CBND.CoreMod.Helpers.Client;
    using AtomicTorch.CBND.CoreMod.Items.Explosives.Bombs;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures.LandClaim;
    using AtomicTorch.CBND.CoreMod.Systems.LandClaim;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.HUD.Notifications;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.GameEngine.Common.Primitives;
    using JetBrains.Annotations;

    [UsedImplicitly]
    public class ClientRaidblockWatcher
    {
        public const string Notification_Message_Format =
            @"Repair and building actions are restricted.
              [br]Raid block will expire in: {0}.";

        public const string Notification_Title =
            "This base is under raid block";

        private static HUDNotificationControl currentNotification;

        private static string GetNotificationText(double timeRemains)
        {
            if (timeRemains < 1)
            {
                timeRemains = 1;
            }

            return string.Format(Notification_Message_Format,
                                 ClientTimeFormatHelper.FormatTimeDuration(timeRemains));
        }

        private static void Refresh()
        {
            var position = ClientCurrentCharacterHelper.Character?.TilePosition ?? Vector2Ushort.Zero;
            var areasGroup = LandClaimSystem.ServerFindLandClaimAreasGroup(position,    addGracePadding: false)
                             ?? LandClaimSystem.ServerFindLandClaimAreasGroup(position, addGracePadding: true);

            var lastRaidTime = areasGroup != null
                                   ? LandClaimAreasGroup.GetPublicState(areasGroup).LastRaidTime ?? double.MinValue
                                   : double.MinValue;

            var time = Api.Client.CurrentGame.ServerFrameTimeRounded;
            var timeSinceRaidStart = time - lastRaidTime;
            var timeRemainsToRaidEnd = LandClaimSystem.RaidBlockDurationSeconds - timeSinceRaidStart;
            timeRemainsToRaidEnd = Math.Max(timeRemainsToRaidEnd, 0);

            if (timeRemainsToRaidEnd <= 0)
            {
                // no raid here - hide notification
                currentNotification?.Hide(quick: true);
                currentNotification = null;
                return;
            }

            // raid here, display/update notification
            var text = GetNotificationText(timeRemainsToRaidEnd);
            if (currentNotification != null
                && !currentNotification.IsHiding)
            {
                currentNotification.SetMessage(text);
                return;
            }

            currentNotification = NotificationSystem.ClientShowNotification(
                title: Notification_Title,
                message: text,
                autoHide: false,
                // TODO: add custom icon here, currently we're using a placeholder icon
                icon: Api.GetProtoEntity<ItemBombModern>().Icon);
        }

        private static void Update()
        {
            // schedule next update
            ClientTimersSystem.AddAction(delaySeconds: 0.2, Update);
            Refresh();
        }

        private class Bootstrapper : BaseBootstrapper
        {
            public override void ClientInitialize()
            {
                base.ClientInitialize();
                ClientTimersSystem.AddAction(delaySeconds: 1, Update);
            }
        }
    }
}