using Notification_Forwarder.ConfigHelper;
using System;
using System.Linq;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;

namespace Notification_Forwarder
{
    public sealed partial class MainPage : Page
    {
        private async void NotificationHandler(object sender, UserNotificationChangedEventArgs e)
        {
            if (!IsListenerActive) return;
            if (e.ChangeKind != UserNotificationChangedKind.Added) return;
            try
            {
                var notification = Listener.GetNotification(e.UserNotificationId);
                Conf.Log($"received a notification from listener");

                NewNotificationPool.Add(notification);
                Notifications.Add(notification);

                Conf.CurrentConf.AddApp(new AppInfo(notification.AppInfo) { ForwardingEnabled = !Conf.CurrentConf.MuteNewApps });
                var appIndex = Conf.CurrentConf.FindAppIndex(new AppInfo(notification.AppInfo));
                if ((appIndex == -1 && !Conf.CurrentConf.MuteNewApps) ||
                    (appIndex != -1 && Conf.CurrentConf.AppsToForward[appIndex].ForwardingEnabled))
                {
                    Conf.Log($"marked notification #{notification.Id} as pending, app: {notification.AppInfo.AppUserModelId}");
                    UnsentNotificationPool.Add(new Protocol.Notification(notification));
                }
                Conf.CurrentConf.NotificationsReceived++;
            }
            catch (Exception ex)
            {
                Conf.Log($"notification listener failed: {ex.Message}, HRESULT {ex.HResult:x}", LogLevel.Error);
                if (ex.HResult == -2147024891)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => { await NoPermissionDialog(); });
                }
            }
        }
    }
}