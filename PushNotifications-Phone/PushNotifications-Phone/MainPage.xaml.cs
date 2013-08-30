using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PushNotifications-Phone.Resources;
using Microsoft.Phone.Notification;
using System.Text;

namespace PushNotifications-Phone
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void HandleRegisterButtonClick(object sender, RoutedEventArgs e)
        {
            RegisterButton.IsEnabled = false;
            /// Holds the push channel that is created or found.
            HttpNotificationChannel pushChannel;

            // The name of our push channel.
            string channelName = "MoblicoPushExampleChannel";

            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);
                

                pushChannel.Open();

                // Bind this new channel for toast events.
                pushChannel.BindToShellToast();
            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                // Send the URI to Moblico web service.
                AddDeviceWithChannelUri(pushChannel.ChannelUri);
            }
        }

        private void AddDeviceWithChannelUri(Uri channelUri)
        {
            Log("Registering device with Moblico.");

            string path = "https://moblico.net/services/v4/device";

            string username = GetDeviceUniqueID();
            string parameters = String.Format("platformName=WINDOWS&apikey={0}&deviceId={1}",
                "YOUR_API_KEY_HERE",
                HttpUtility.UrlEncode(channelUri.ToString()));
            if (username != null)
            {
                parameters += "&username=" + HttpUtility.UrlEncode(username);
            }

            Uri serviceUri = new Uri(path);

            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            webClient.UploadStringAsync(serviceUri, "POST", parameters);
            webClient.UploadStringCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Dispatcher.BeginInvoke(() => 
                    {
                        RegisterButton.IsEnabled = true;
                        Log(String.Format("An error occurred: {0}", e.Error.Message));
                    });
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        RegisterButton.IsEnabled = true;
                        Log("Success!");
                    });
                }
            };
        }

        void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            string message;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(e.Notification.Body))
            {
                message = reader.ReadToEnd();
            }

            Dispatcher.BeginInvoke(() => 
            {
                Log(String.Format("Received Notification {0}:\n{1}", DateTime.Now.ToShortTimeString(), message));
            });
        }

        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;

            message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            foreach (string key in e.Collection.Keys)
            {
                message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);

                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    relativeUri = e.Collection[key];
                }
            }

            // Display a dialog of all the fields in the toast.
            Dispatcher.BeginInvoke(() => {
                MessageBox.Show(message.ToString());
            });

        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
            {
                Log(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData));
            });
        }

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Send the URI to Moblico web service.
                Log("Received Channel URI from Microsoft");
                AddDeviceWithChannelUri(e.ChannelUri);
            });
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
            string result = null;
            object uniqueId;
            if (Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out uniqueId))
            {
                result = Convert.ToBase64String((byte[])uniqueId);
            }
            return result;
        }
    }
}