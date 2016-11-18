﻿
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Networking.Proximity;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using NdefLibrary.Ndef;
using NdefLibraryUwp.Ndef;
using Windows.Foundation.Metadata;


namespace NFC_King.Pages
{
    
    public sealed partial class Aplicativo : Page
    {
        private ProximityDevice _device;
        private long _subscriptionIdNdef;
        private long _publishingMessageId;
        private readonly CoreDispatcher _dispatcher;
        private readonly ResourceLoader _loader = new ResourceLoader();


        public Aplicativo()
        {
            InitializeComponent();
           


            //SetStatusOutput(string.Format(_loader.GetString("FirstStatus"), _subscriptionIdNdef));

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            // Update enabled / disabled state of buttons in the User Interface
            UpdateUiForNfcStatusAsync();
        }
        public Type DestinationPage { get; set; }


        private void BtnInitNfc_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            // Initialize NFC
            _device = ProximityDevice.GetDefault();
            // Subscribe for arrived / departed events
            if (_device != null)
            {
                _device.DeviceArrived += NfcDeviceArrived;
                _device.DeviceDeparted += NfcDeviceDeparted;
            }
            // Update status text for UI
            SetStatusOutput(_loader.GetString(_device != null ? "StatusInitialized" : "StatusInitFailed"));
            // Update enabled / disabled state of buttons in the User Interface
            UpdateUiForNfcStatusAsync();
        }

        #region Device Arrived / Departed
        private void NfcDeviceDeparted(ProximityDevice sender)
        {
            TxtStatus.Text = "Dispositivo desconectado";
            SetStatusOutput(_loader.GetString("DeviceDeparted"));
            SetStatusImage(null);
        }

        private void NfcDeviceArrived(ProximityDevice sender)
        {
            TxtStatus.Text = "Dispositivo conectado";
            SetStatusOutput(_loader.GetString("DeviceArrived"));
        }
        #endregion

        #region Subscribe for tags
        // ----------------------------------------------------------------------------------------------------
        private void BtnSubscribeNdef_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            // Only subscribe for messages if no NDEF subscription is already active
            if (_subscriptionIdNdef != 0) return;
            // Ask the proximity device to inform us about any kind of NDEF message received from
            // another device or tag.
            // Store the subscription ID so that we can cancel it later.
            _subscriptionIdNdef = _device.SubscribeForMessage("NDEF", MessageReceivedHandler);
            // Update status text for UI
            SetStatusOutput(string.Format(_loader.GetString("StatusSubscribed"), _subscriptionIdNdef));
            // Update enabled / disabled state of buttons in the User Interface
            UpdateUiForNfcStatusAsync();
        }

        private async void MessageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            // Get the raw NDEF message data as byte array
            var rawMsg = message.Data.ToArray();

            NdefMessage ndefMessage;
            try
            {
                // Let the NDEF library parse the NDEF message out of the raw byte array
                ndefMessage = NdefMessage.FromByteArray(rawMsg);
            }
            catch (NdefException e)
            {
                SetStatusOutput(string.Format(_loader.GetString("InvalidNdef"), e.Message));
                return;
            }

            // Analysis result
            var tagContents = new StringBuilder();

            // Parse tag contents
            try
            {
                // Clear bitmap if the last tag contained an image
                SetStatusImage(null);

                // Parse the contents of the tag
                await ParseTagContents(ndefMessage, tagContents);

                // Update status text for UI
                SetStatusOutput(string.Format(_loader.GetString("StatusTagParsed"), tagContents));
            }
            catch (Exception ex)
            {
                SetStatusOutput(string.Format(_loader.GetString("StatusNfcParsingError"), ex.Message));
            }

        }

        private async Task ParseTagContents(NdefMessage ndefMessage, StringBuilder tagContents)
        {
            // Loop over all records contained in the NDEF message
            foreach (NdefRecord record in ndefMessage)
            {
                // --------------------------------------------------------------------------
                // Print generic information about the record
                if (record.Id != null && record.Id.Length > 0)
                {
                    // Record ID (if present)
                    tagContents.AppendFormat("Id: {0}\n", Encoding.UTF8.GetString(record.Id, 0, record.Id.Length));
                }
                // Record type name, as human readable string
                tagContents.AppendFormat("Type name: {0}\n", ConvertTypeNameFormatToString(record.TypeNameFormat));
                // Record type
                if (record.Type != null && record.Type.Length > 0)
                {
                    tagContents.AppendFormat("Record type: {0}\n",
                        Encoding.UTF8.GetString(record.Type, 0, record.Type.Length));
                }

                // --------------------------------------------------------------------------
                // Check the type of each record
                // Using 'true' as parameter for CheckSpecializedType() also checks for sub-types of records,
                // e.g., it will return the SMS record type if a URI record starts with "sms:"
                // If using 'false', a URI record will always be returned as Uri record and its contents won't be further analyzed
                // Currently recognized sub-types are: SMS, Mailto, Tel, Nokia Accessories, NearSpeak, WpSettings
                var specializedType = record.CheckSpecializedType(true);

                if (specializedType == typeof(NdefMailtoRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract Mailto record info
                    var mailtoRecord = new NdefMailtoRecord(record);
                    tagContents.Append("-> Mailto record\n");
                    tagContents.AppendFormat("Address: {0}\n", mailtoRecord.Address);
                    tagContents.AppendFormat("Subject: {0}\n", mailtoRecord.Subject);
                    tagContents.AppendFormat("Body: {0}\n", mailtoRecord.Body);
                }
                else if (specializedType == typeof(NdefUriRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract URI record info
                    var uriRecord = new NdefUriRecord(record);
                    tagContents.Append("-> URI record\n");
                    tagContents.AppendFormat("URI: {0}\n", uriRecord.Uri);
                }
                else if (specializedType == typeof(NdefSpRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract Smart Poster info
                    var spRecord = new NdefSpRecord(record);
                    tagContents.Append("-> Smart Poster record\n");
                    tagContents.AppendFormat("URI: {0}", spRecord.Uri);
                    tagContents.AppendFormat("Titles: {0}", spRecord.TitleCount());
                    if (spRecord.TitleCount() > 1)
                        tagContents.AppendFormat("1. Title: {0}", spRecord.Titles[0].Text);
                    tagContents.AppendFormat("Action set: {0}", spRecord.ActionInUse());
                    // You can also check the action (if in use by the record), 
                    // mime type and size of the linked content.
                }
                else if (specializedType == typeof(NdefVcardRecordBase))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract business card info
                    var vcardRecord = new NdefVcardRecord(record);
                    tagContents.Append("-> Business Card record" + Environment.NewLine);
                    var contact = vcardRecord.ContactData;

                    // Contact has phone or email info set? Use contact manager to show the contact card
                    await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (contact.Emails.Any() || contact.Phones.Any())
                        {
                            //var rect = GetElementRect(StatusOutput);
                            //ContactManager.ShowContactCard(contact, rect, Placement.Below);
                        }
                        else
                        {
                            // No phone or email set - contact manager would not show the contact card.
                            // -> parse manually
                            tagContents.AppendFormat("Name: {0}\n", contact.DisplayName);
                            tagContents.Append("[not parsing other values in the demo app]");
                        }
                    });
                }
                else if (specializedType == typeof(NdefLaunchAppRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract LaunchApp record info
                    var launchAppRecord = new NdefLaunchAppRecord(record);
                    tagContents.Append("-> LaunchApp record" + Environment.NewLine);
                    if (!string.IsNullOrEmpty(launchAppRecord.Arguments))
                        tagContents.AppendFormat("Arguments: {0}\n", launchAppRecord.Arguments);
                    if (launchAppRecord.PlatformIds != null)
                    {
                        foreach (var platformIdTuple in launchAppRecord.PlatformIds)
                        {
                            if (platformIdTuple.Key != null)
                                tagContents.AppendFormat("Platform: {0}\n", platformIdTuple.Key);
                            if (platformIdTuple.Value != null)
                                tagContents.AppendFormat("App ID: {0}\n", platformIdTuple.Value);
                        }
                    }
                }
                else if (specializedType == typeof(NdefMimeImageRecordBase))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract Image record info
                    var imgRecord = new NdefMimeImageRecord(record);
                    tagContents.Append("-> MIME / Image record" + Environment.NewLine);
                    _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => SetStatusImage(await imgRecord.GetImageAsBitmap()));

                }
                else
                {
                    // Other type, not handled by this demo
                    tagContents.Append("NDEF record not parsed by this demo app" + Environment.NewLine);
                }
            }
        }

        private Rect GetElementRect(FrameworkElement element)
        {
            var elementTransform = element.TransformToVisual(null);
            var point = elementTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }
        #endregion

        #region NFC Publishing
        // ----------------------------------------------------------------------------------------------------

        private void BtnWriteLaunchApp_Click(object sender, RoutedEventArgs e)
        {
            // Create a new LaunchApp record to launch this app
            // The app will print the arguments when it is launched (see MainPage.OnNavigatedTo() method)
            var record = new NdefLaunchAppRecord { Arguments = "Hello World" };

            // WindowsPhone is the pre-defined platform ID for WP8.
            // You can get the application ID from the WMAppManifest.xml file
            //record.AddPlatformAppId("WindowsPhone", "{544ec154-b521-4d73-9405-963830adb213}");

            // The app platform for a Windows 8 computer is Windows. 
            // The format of the proximity app Id is <package family name>!<app Id>. 
            // You can get the package family name from the Windows.ApplicationModel.Package.Current.Id.FamilyName property. 
            // You must copy the app Id value from the Id attribute of the Application element in the 
            // package manifest for your app.

            var familyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            //var appId = Windows.ApplicationModel.Store.CurrentApp.AppId;    // Crashes when app is not installed from the app store!
            var appId = "8bf48432-f9c8-48cd-a014-c44d868347dc";
            // Issue on Windows 10: http://stackoverflow.com/questions/34221812/how-to-launch-my-app-via-nfc-tag
            // Issue on Windows 10: https://social.msdn.microsoft.com/Forums/sqlserver/en-US/c9653f06-0d48-498f-9b3e-335435780fd4/cw81windows-81-app-license-error-0x803f6107?forum=wpdevelop
            record.AddPlatformAppId("Windows", "{" + familyName + "!" + appId + "}");
            record.AddPlatformAppId("WindowsPhone", appId);

            // Publish the record using the proximity device
            PublishRecord(record, true);
        }








        private async void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (TxtBoxReceiver.Text == "")
            {
                MessageDialog showDialog = new MessageDialog("Destinatário Vazio. Preencha o campo do destinatário e tente novamente.");

                showDialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                showDialog.DefaultCommandIndex = 0;
                var result = await showDialog.ShowAsync();
                campovazio();

            }
            
            if (TxtBoxMessage.Text == "")
            {
                MessageDialog showDialog = new MessageDialog("Mensagem vazia. Preencha o campo de mensagem e tente novamente.");

                showDialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                showDialog.DefaultCommandIndex = 0;
                var result = await showDialog.ShowAsync();
                campovazio();

            }
            else
            {

                // Initialize NFC
                _device = ProximityDevice.GetDefault();
                // Subscribe for arrived / departed events
                if (_device != null)
                {
                    _device.DeviceArrived += NfcDeviceArrived;
                    _device.DeviceDeparted += NfcDeviceDeparted;
                }
                // Update status text for UI
                SetStatusOutput(_loader.GetString(_device != null ? "StatusInitialized" : "StatusInitFailed"));
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();

                // Only subscribe for messages if no NDEF subscription is already active
                if (_subscriptionIdNdef != 0) return;
                // Ask the proximity device to inform us about any kind of NDEF message received from
                // another device or tag.
                // Store the subscription ID so that we can cancel it later.
                _subscriptionIdNdef = _device.SubscribeForMessage("NDEF", MessageReceivedHandler);
                // Update status text for UI
                SetStatusOutput(string.Format(_loader.GetString("StatusSubscribed"), _subscriptionIdNdef));
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
                // Stop publishing the message
                StopPublishingMessage(false);
                // Update status text for UI
                SetStatusOutput(_loader.GetString("StatusMessageWritten"));
                

                // Create a new mailto record, set the relevant properties for the email
                var record = new NdefSmsRecord { SmsNumber = TxtBoxReceiver.Text, SmsBody = TxtBoxMessage.Text };
                PublishRecord(record, true);
                sucesso();
                StopPublishingMessage(true);
                StopSubscription(true);
                

            }
        }
        public async void campovazio()
        {
            MessageDialog showDialog = new MessageDialog("Endereço vazio. Preencha o campo do endereço e tente novamente.");

            showDialog.Commands.Add(new UICommand("Ok") { Id = 0 });
            showDialog.DefaultCommandIndex = 0;
            var result = await showDialog.ShowAsync();
        }
        public void aproximatag()
        {
            MessageDialog showDialog = new MessageDialog("Aproxime a tag");

            showDialog.Commands.Add(new UICommand("Ok") { Id = 0 });
            showDialog.DefaultCommandIndex = 0;
            var result = showDialog.ShowAsync();
        }
        public void sucesso()
        {
            MessageDialog showDialog = new MessageDialog("Sucesso");
            showDialog.Commands.Add(new UICommand("Ok") { Id = 0 });
            showDialog.DefaultCommandIndex = 0;

            var result = showDialog.ShowAsync();
        }



        private void TagLockedHandler(ProximityDevice sender, long messageid)
        {

        }

        private void PublishRecord(NdefRecord record, bool writeToTag)
        {
            if (_device == null) return;
            // Make sure we're not already publishing another message
            StopPublishingMessage(false);
            // Wrap the NDEF record into an NDEF message
            var message = new NdefMessage { record };
            // Convert the NDEF message to a byte array
            var msgArray = message.ToByteArray();
            try
            {
                // Publish the NDEF message to a tag or to another device, depending on the writeToTag parameter
                // Save the publication ID so that we can cancel publication later
                _publishingMessageId = _device.PublishBinaryMessage((writeToTag ? "NDEF:WriteTag" : "NDEF"), msgArray.AsBuffer(), MessageWrittenHandler);
                // Update status text for UI
                SetStatusOutput(string.Format(_loader.GetString(writeToTag ? "StatusWriteToTag" : "StatusWriteToDevice"), msgArray.Length, _publishingMessageId));
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
            }
            catch (Exception e)
            {
                SetStatusOutput(string.Format(_loader.GetString("StatusPublicationError"), e.Message));
            }
        }

        private void MessageWrittenHandler(ProximityDevice sender, long messageid)
        {
            // Stop publishing the message
            StopPublishingMessage(false);
            // Update status text for UI
            SetStatusOutput(_loader.GetString("StatusMessageWritten"));
        }
        #endregion

        #region Managing Subscriptions
        private void BtnStopSubscription_Click(object sender, RoutedEventArgs e)
        {
            // Stop NDEF subscription and print status update to the UI
            StopSubscription(true);
        }

        private async void StopSubscription(bool writeToStatusOutput)
        {
            if (_subscriptionIdNdef != 0 && _device != null)
            {
                // Ask the proximity device to stop subscribing for NDEF messages
                _device.StopSubscribingForMessage(_subscriptionIdNdef);
                _subscriptionIdNdef = 0;
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
                // Update status text for UI - only if activated
                if (writeToStatusOutput) SetStatusOutput(_loader.GetString("StatusSubscriptionStopped"));
            }
        }

        private void BtnStopPublication_Click(object sender, RoutedEventArgs e)
        {
            StopPublishingMessage(true);
        }

        private void StopPublishingMessage(bool writeToStatusOutput)
        {
            if (_publishingMessageId != 0 && _device != null)
            {
                // Stop publishing the message
                _device.StopPublishingMessage(_publishingMessageId);
                _publishingMessageId = 0;
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
                // Update status text for UI - only if activated
                if (writeToStatusOutput) SetStatusOutput(_loader.GetString("StatusPublicationStopped"));
            }
        }
        #endregion

        #region UI Management

        private void SetStatusOutput(string newStatus)
        {
            // Update the status output UI element in the UI thread
            // (some of the callbacks are in a different thread that wouldn't be allowed
            // to modify the UI thread)
            //_dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { if (StatusOutput != null) StatusOutput.Text = newStatus; });
        }

        private void SetStatusImage(WriteableBitmap newImg)
        {
            // Update the status output UI element in the UI thread
            // (some of the callbacks are in a different thread that wouldn't be allowed
            // to modify the UI thread)
            _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //StatusImg.Source = newImg;
                if (newImg != null)
                {
                    //StatusImg.Width = newImg.PixelWidth;
                   // StatusImg.Height = newImg.PixelHeight;
                }
            });
        }

        private string ConvertTypeNameFormatToString(NdefRecord.TypeNameFormatType tnf)
        {
            // Each record contains a type name format, which defines which format
            // the type name is actually in.
            // This method converts the constant to a human-readable string.
            string tnfString;
            switch (tnf)
            {
                case NdefRecord.TypeNameFormatType.Empty:
                    tnfString = "Empty NDEF record (does not contain a payload)";
                    break;
                case NdefRecord.TypeNameFormatType.NfcRtd:
                    tnfString = "NFC RTD Specification";
                    break;
                case NdefRecord.TypeNameFormatType.Mime:
                    tnfString = "RFC 2046 (Mime)";
                    break;
                case NdefRecord.TypeNameFormatType.Uri:
                    tnfString = "RFC 3986 (Url)";
                    break;
                case NdefRecord.TypeNameFormatType.ExternalRtd:
                    tnfString = "External type name";
                    break;
                case NdefRecord.TypeNameFormatType.Unknown:
                    tnfString = "Unknown record type; should be treated similar to content with MIME type 'application/octet-stream' without further context";
                    break;
                case NdefRecord.TypeNameFormatType.Unchanged:
                    tnfString = "Unchanged (partial record)";
                    break;
                case NdefRecord.TypeNameFormatType.Reserved:
                    tnfString = "Reserved";
                    break;
                default:
                    tnfString = "Unknown";
                    break;
            }
            return tnfString;
        }

        public async void UpdateUiForNfcStatusAsync()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

            });
        }
        #endregion

      
    }
}
