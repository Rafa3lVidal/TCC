
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



// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NFC_King.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Email : Page
    {
        private ProximityDevice _device;
        private long _subscriptionIdNdef;
        private long _publishingMessageId;
        private readonly CoreDispatcher _dispatcher;
        private readonly ResourceLoader _loader = new ResourceLoader();


        public Email()
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
        
      
        private void DispositivoDesconectado(ProximityDevice sender)
        {
            TxtStatus.Text = "Dispositivo Desconectado";
        }

        private void DispositivoConectado(ProximityDevice sender)
        {
            TxtStatus.Text = "Dispositivo Conectado";
        }
    
        // ----------------------------------------------------------------------------------------------------
 

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
               
                return;
            }

            // Analysis result
            var tagContents = new StringBuilder();

            // Parse tag contents
            try
            {
                // Clear bitmap if the last tag contained an image
                

                // Parse the contents of the tag
                await ParseTagContents(ndefMessage, tagContents);

                // Update status text for UI
                
            }
            catch (Exception ex)
            {
               
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
       

        #region NFC Publishing
        // ----------------------------------------------------------------------------------------------------
        

        private void BtnGravaURI_Click(object sender, RoutedEventArgs e)
        {

            // Inicializa o Dispositivo
            _device = ProximityDevice.GetDefault();
            // Subscribe for arrived / departed events
            if (_device != null)
            {
                _device.DeviceArrived += DispositivoConectado;
                _device.DeviceDeparted += DispositivoDesconectado;
                TxtStatus.Text = "Aproxime a tag";
            }
         

            // Update status text for UI

            // Update enabled / disabled state of buttons in the User Interface
            UpdateUiForNfcStatusAsync();

            // Only subscribe for messages if no NDEF subscription is already active
            if (_subscriptionIdNdef != 0) return;
            TxtStatus.Text = "Dispositivo Suportado";
            // Ask the proximity device to inform us about any kind of NDEF message received from
            // another device or tag.
            // Store the subscription ID so that we can cancel it later.
            _subscriptionIdNdef = _device.SubscribeForMessage("NDEF", MessageReceivedHandler);
            // Update status text for UI
           
            // Update enabled / disabled state of buttons in the User Interface
            UpdateUiForNfcStatusAsync();
            // Stop publishing the message
            StopPublishingMessage(false);
            // Update status text for UI
           

            string email = TxtBoxEndEmail.Text;
            string assunto = TxtBoxAssuntoEmail.Text;
            string mensagem = TxtBoxMsgEmail.Text;

            // Create a new mailto record, set the relevant properties for the email
            var record = new NdefMailtoRecord
            {
                Address = email,
                Subject = assunto,
                Body = mensagem
            };
            // Publish the record using the proximity device
            PublishRecord(record, true);


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
                TxtStatus.Text = "Aproxime a tag";
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
            }
            catch (Exception e)
            {
               
            }
        }

        private void MessageWrittenHandler(ProximityDevice sender, long messageid)
        {
            // Stop publishing the message
            StopPublishingMessage(false);
            // Update status text for UI
            TxtStatus.Text = "Sucesso!";
        }
        #endregion

        #region Managing Subscriptions
      
        private void ParaGravacao(bool writeToStatusOutput)
        {
            if (_subscriptionIdNdef != 0 && _device != null)
            {
                // Ask the proximity device to stop subscribing for NDEF messages
                _device.StopSubscribingForMessage(_subscriptionIdNdef);
                _subscriptionIdNdef = 0;
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
                // Update status text for UI - only if activated
                
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
               
            }
        }
        #endregion

        #region UI Management


      

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
