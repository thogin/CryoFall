﻿namespace AtomicTorch.CBND.CoreMod.Systems.Technologies
{
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.CoreMod.UI;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core.Menu;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Technologies;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting;

    /// <summary>
    /// This class notifies player when enough learning points accumulated to research new technologies.
    /// </summary>
    public class ClientLearningPointsNotifier : BaseBootstrapper
    {
        public const string NotificationSpendLearningPointsReminder =
            @"You have more than {0} learning points (LP) accumulated. You should consider investing them into unlocking new technologies.
              [br](Click to open the technologies menu.)";

        private static readonly TextureResource NotificationIcon
            = new TextureResource("Technologies/GenericGroupIcon");

        private ushort lastLearningPoints;

        private static PlayerCharacterTechnologies CurrentTechnologies
            => ClientComponentTechnologiesWatcher.CurrentTechnologies;

        public override void ClientInitialize()
        {
            ClientComponentTechnologiesWatcher.CurrentTechnologiesChanged += this.CurrentTechnologiesChangedHandler;
            ClientComponentTechnologiesWatcher.LearningPointsChanged += this.LearningPointsChangedHandler;
        }

        private static void ShowNotificationLearnExtraTechnologies(object points)
        {
            var storage = Client.Storage.GetSessionStorage(nameof(ClientLearningPointsNotifier)
                                                           + nameof(ShowNotificationLearnExtraTechnologies));
            if (storage.TryLoad(out bool _))
            {
                // this message was already was displayed once during this play session
                return;
            }

            // remember that we already showed this message once
            storage.Save(true);

            if (IsEditor)
            {
                // do not show this notification in Editor
                return;
            }

            var notification = NotificationSystem.ClientShowNotification(
                CoreStrings.Technology,
                string.Format(NotificationSpendLearningPointsReminder, points),
                icon: NotificationIcon,
                onClick: Menu.Open<WindowTechnologies>,
                autoHide: false);

            notification.SetupAutoHideChecker(
                // hide when menu is opened
                Menu.IsOpened<WindowTechnologies>);

            ClientTimersSystem.AddAction(delaySeconds: 60,
                                         () => notification.Hide(quick: false));
        }

        private void CurrentTechnologiesChangedHandler()
        {
            this.RefreshLastLearningPointsValue();
        }

        private ushort GetCurrentLearningPoints()
        {
            return ClientComponentTechnologiesWatcher.CurrentTechnologies?.LearningPoints ?? 0;
        }

        private void LearningPointsChangedHandler()
        {
            var lp = this.GetCurrentLearningPoints();
            if (this.lastLearningPoints == lp)
            {
                return;
            }

            try
            {
                if (this.lastLearningPoints > lp)
                {
                    // learning points decreased - don't show any notifications
                    return;
                }

                var researchedCount = CurrentTechnologies.Nodes.Count;
                int lpThreshhold = 100;
                if (researchedCount > 0
                    && lp >= 100
                    && this.lastLearningPoints < lpThreshhold)
                {
                    // player has too much free LP
                    ShowNotificationLearnExtraTechnologies(lpThreshhold);
                }
            }
            finally
            {
                this.RefreshLastLearningPointsValue();
            }
        }

        private void RefreshLastLearningPointsValue()
        {
            this.lastLearningPoints = this.GetCurrentLearningPoints();
        }
    }
}