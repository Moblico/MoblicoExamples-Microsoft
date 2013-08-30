using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Networking.PushNotifications;
using Windows.System.Profile;
using Windows.Storage.Streams;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PushNotifications-Tablet
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void HandleRegisterButtonClick(object sender, RoutedEventArgs e)
        {
            PushNotificationChannel channel = null;

            try
            {
                channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                channel.PushNotificationReceived += OnPushNotification;

                Log("Received channel: " + channel.Uri.ToString());
                AddDeviceWithChannelUri(channel.Uri);
            }

            catch (Exception ex)
            {
                // Could not create a channel. 
                Log(ex.ToString());
            }


        }

        private async void AddDeviceWithChannelUri(string channelUri)
        {
            Log("Registering device with Moblico.");

            string serverUrl = "https://moblico.net/services/v4/device";

            // Create the web request.
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(serverUrl);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            string username = GetDeviceUniqueID();
            string parameters = String.Format("platformName=WINDOWS_TABLET&apikey={0}&deviceId={1}",
                "YOUR_API_KEY_HERE",
                WebUtility.UrlEncode(channelUri.ToString()));
            if (username != null)
            {
                parameters += "&username=" + WebUtility.UrlEncode(username);
            }

            byte[] parameterData = System.Text.Encoding.UTF8.GetBytes(parameters);

            // Write the channel URI to the request stream.
            Stream requestStream = await webRequest.GetRequestStreamAsync();
            requestStream.Write(parameterData, 0, parameterData.Length);

            try
            {
                // Get the response from the server.
                WebResponse response = await webRequest.GetResponseAsync();
                StreamReader requestReader = new StreamReader(response.GetResponseStream());
                string webResponse = requestReader.ReadToEnd();
                RegisterButton.IsEnabled = true;
                Log("Success!");
            }

            catch (Exception ex)
            {
                // Could not send channel URI to server.
                RegisterButton.IsEnabled = true;
                Log(String.Format("An error occurred: {0}", ex.ToString()));
            }
        }

        private void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
        {
            String notificationContent = String.Empty;

            switch (e.NotificationType)
            {
                case PushNotificationType.Badge:
                    notificationContent = e.BadgeNotification.Content.GetXml();
                    break;

                case PushNotificationType.Tile:
                    notificationContent = e.TileNotification.Content.GetXml();
                    break;

                case PushNotificationType.Toast:
                    notificationContent = e.ToastNotification.Content.GetXml();
                    break;

                case PushNotificationType.Raw:
                    notificationContent = e.RawNotification.Content;
                    break;
            }

            e.Cancel = true;

            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                Log(String.Format("Received Notification {0}:\n{1}", DateTime.Now.ToString(), notificationContent))).AsTask().Wait();
        }


 
 
        private void Log(String message)
        {
            System.Diagnostics.Debug.WriteLine(message);

            if (RegistrationResultsTextBox.Text == null)
            {
                RegistrationResultsTextBox.Text = "";
            }

            RegistrationResultsTextBox.Text += message + "\n";
        }
        
        public static string GetDeviceUniqueID()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return Convert.ToBase64String(bytes);
        }


    }
}
