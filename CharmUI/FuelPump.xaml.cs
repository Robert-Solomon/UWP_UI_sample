using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Display.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input.ForceFeedback;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CharmUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FuelPump : Page
    {
        bool MediaEnabled = false;
        double FuelFilled = 0;
        double FuelPrice = 0;
        DispatcherTimer DisplayDriver_Timer1 = new DispatcherTimer();
        double ScrollPos = 0;
        uint PosZeroDelay = 10;
        enum PumpStates { StateInitial = 0, StateFuelSelected, StatePumpStarted, StatePumpStopped };
        PumpStates PumpState = PumpStates.StateInitial;
        enum FuelTypes { FuelNotSelected = 0, FuelPremium, FuelRegular, FuelDiesel };
        FuelTypes FuelType = FuelTypes.FuelNotSelected;
        readonly string StrAdBanner = "While you are waiting for your car to fill, just ignore the sign instructing you to never leave the pump unattended, and head into the store to grab a cup of coffee...              ";
        readonly string StrStartFueling = "Start Fueling";
        readonly string StrSelectFuel = "Select Fuel Type";
        readonly string StrPauseStop = "Stop/Pause Pump";
        readonly string StrResumeFueling = "Resume Pump";
        string MediaFolder;
        readonly string MediaIndexFile = "Media.lst";
        readonly string MediaFolderLocalSettingKey = "MediaFolder";

        struct sUri {
            private string display;
            private string file;
            private int width;
            private int height;

            public sUri(string display, int width, int height, string file)
            {
                this.display = display;
                this.width = width;
                this.height = height;
                this.file = file;
            }

            public string Display { get { return display; } }
            public int Width { get { return width; } }
            public int Height { get { return height; } }
            public string File { get { return file; } }
        }

        List<sUri> MediaList = new List<sUri>();

        struct sVid {
            private readonly string display;
            private readonly int col;
            private readonly int row;

            public sVid(string display, int col, int row)
            {
                this.display = display;
                this.col = col;
                this.row = row;
            }

            public string Display { get { return display; } }
            public int Col { get { return col; } }
            public int Row { get { return row; } }
        }

        static readonly sVid[] VidFrameSize = new[]
        {
            new sVid ("Native", 0, 0),
            new sVid ("320x200 (CGA)", 320, 200),
            new sVid ("640x480 (VGA)", 640, 480),
            new sVid ("800x600", 800, 600),
            new sVid ("1280x720 (HD)", 1280, 720),
            new sVid ("1920x1080 (FHD)", 1920, 1080),
        };

        public FuelPump()
        {
            this.InitializeComponent();

            ResetPump();
            DisplayDriver_Timer1.Interval = new TimeSpan(0, 0, 0, 0, 100);
            DisplayDriver_Timer1.Tick += DisplayDriver_TimerTick;

            InitPageFromMedia();
        }

        void InitPageFromMedia(string path = null)
        {
            string _mediaFolder;

            if (path == null)
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                _mediaFolder = (string)localSettings.Values[MediaFolderLocalSettingKey];
            }
            else
                _mediaFolder = path;

            if (_mediaFolder == null)
            {
                DisableMediaControls();
                MediaErrorMessage("No Media Folder configured");
            }
            else if (!File.Exists(_mediaFolder + MediaIndexFile))
            {
                DisableMediaControls();
                MediaErrorMessage("No " + MediaIndexFile + " found in media folder");
            }
            else
            {
                // NOTE - no validation on content of the media.lst file content!!!

                // initialize the media list from file
                string[] lines = System.IO.File.ReadAllLines(_mediaFolder + MediaIndexFile);
                foreach (var line in lines)
                {
                    string[] words = line.Split(',');
                    string display = words[0];
                    int width = Int32.Parse(words[1]);
                    int height = Int32.Parse(words[2]);
                    string file = words[3];
                    MediaList.Add(new sUri(display, width, height, file));
                }

                foreach (var l in MediaList)
                    VidList.Items.Add(l.Display);

                foreach (var s in VidFrameSize)
                    VidSize.Items.Add(s.Display);
                VidSize.SelectedIndex = 0;

                MediaFolder = _mediaFolder;
                EnableMediaControls();
            }
        }

        async void NewMediaFolderAsync()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add(".lst");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                string path = folder.Path + "\\";
                InitPageFromMedia(path);

                if (MediaEnabled)
                {
                    Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values[MediaFolderLocalSettingKey] = path;
                }
            }
            else
            {
                // folder selection canceled
            }
        }

        void DisableMediaControls()
        {
            VidList.IsEnabled = false;
            VidPlay.IsEnabled = false;
            VidSize.IsEnabled = false;
            MediaEnabled = false;
        }

        void EnableMediaControls()
        {
            VidList.IsEnabled = true;
            VidPlay.IsEnabled = true;
            VidSize.IsEnabled = true;
            MediaEnabled = true;
        }

        void MediaErrorMessage(string msg)
        {

        }

        void FuelPump_Loaded(object sender, RoutedEventArgs e)
        {
            if (!MediaEnabled)
            {
                NewMediaFolderAsync();
            }
        }

        void DisplayDriver_TimerTick(object sender, object /*EventArgs*/ e)
        {
            try
            {
                if (PumpState == PumpStates.StatePumpStarted)
                {
                    if (FuelFilled < 99.92)
                        FuelFilled += 0.073;
                    double t = FuelFilled * FuelPrice;
                    SaleString.Text = $"{t:F2}";
                    GallonsString.Text = $"{FuelFilled:F3}";
                }

                ScrollContent.ChangeView(ScrollPos,null,null);
                if (PosZeroDelay > 0)
                {
                    PosZeroDelay--;
                    return;
                }
                if (ScrollContent.HorizontalOffset < (ScrollPos - 100))
                {
                    TxtBanner.Text = StrAdBanner;
                    ScrollPos = 0;
                    PosZeroDelay = 15;
                }
                else
                    ScrollPos += 10;
            }
            catch { }
        }

        private void BtnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
        private void BtnMediaSrc_Click(object sender, RoutedEventArgs e)
        {
            NewMediaFolderAsync();
        }

        private void BtnFuelPremium_Click(object sender, RoutedEventArgs e)
        {
            if (PumpState > PumpStates.StateFuelSelected)
                return;
            FuelSelection(FuelTypes.FuelPremium);
        }

        private void BtnFuelRegular_Click(object sender, RoutedEventArgs e)
        {
            if (PumpState > PumpStates.StateFuelSelected)
                return;
            FuelSelection(FuelTypes.FuelRegular);
        }

        private void BtnFuelDiesel_Click(object sender, RoutedEventArgs e)
        {
            if (PumpState > PumpStates.StateFuelSelected)
                return;
            FuelSelection(FuelTypes.FuelDiesel);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetPump();
        }

        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            switch (PumpState)
            {
                case PumpStates.StateInitial:
                    break;

                case PumpStates.StateFuelSelected:
                    TxtBanner.Text = StrAdBanner;
                    ScrollContent.ChangeView(0, null, null);
                    DisplayDriver_Timer1.Start();
                    BtnStartStop.Content = StrPauseStop;

                    PumpState = PumpStates.StatePumpStarted;
                    break;

                case PumpStates.StatePumpStarted:

                    BtnStartStop.Content = StrResumeFueling;
                    PumpState = PumpStates.StatePumpStopped;
                    break;

                case PumpStates.StatePumpStopped:

                    BtnStartStop.Content = StrPauseStop;
                    PumpState = PumpStates.StatePumpStarted;
                    break;
            }
        }

        private void FuelSelection(FuelTypes ft)
        {
            if (FuelType == ft)
                return;

            FuelType = ft;
            FuelPrice = 0;

            if (ft == FuelTypes.FuelPremium)
            {
                BtnFuelPremium.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0xff, 0x03, 0x03));
                FuelPrice = 3.599;
            }
            else
                BtnFuelPremium.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xff, 0x03, 0x03));

            if (ft == FuelTypes.FuelRegular)
            {
                BtnFuelRegular.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0xff, 0x03, 0x03));
                FuelPrice = 3.299;
            }
            else
                BtnFuelRegular.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xff, 0x03, 0x03));

            if (ft == FuelTypes.FuelDiesel)
            {
                BtnFuelDiesel.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0x0e, 0xff, 0x03));
                FuelPrice = 2.899;
            }
            else
                BtnFuelDiesel.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 0x0e, 0xff, 0x03));

            PriceString.Text = $"{FuelPrice:F3}";

            if (ft != FuelTypes.FuelNotSelected)
            {
                PumpState = PumpStates.StateFuelSelected;
                TxtBanner.Text = StrStartFueling;
                BtnStartStop.Content = StrStartFueling;
                BtnStartStop.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 228, 146, 73));
            }
        }

        private void ResetPump()
        {
            DisplayDriver_Timer1.Stop();

            FuelSelection(FuelTypes.FuelNotSelected);
            SaleString.Text = "00.00";
            GallonsString.Text = "00.000";
            TxtBanner.Text = StrSelectFuel;
            BtnStartStop.Content = StrSelectFuel;
            BtnStartStop.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 228, 146, 73));

            FuelFilled = 0;

            PumpState = PumpStates.StateInitial;
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    if (VidList.SelectedIndex < 0)
                    {
                        toggleSwitch.IsOn = false;
                    }
                    else
                    {
                        if (VidSize.SelectedIndex > 0)
                        {
                            VidFrame.Height = VidFrameSize[VidSize.SelectedIndex].Row;
                            VidFrame.Width = VidFrameSize[VidSize.SelectedIndex].Col;
                        }
                        else
                        {
                            VidFrame.Height = MediaList[VidList.SelectedIndex].Height;
                            VidFrame.Width = MediaList[VidList.SelectedIndex].Width;
                        }
                        string fp = MediaFolder + MediaList[VidList.SelectedIndex].File;
                        VidFrame.Source = new System.Uri(@fp);
                        //VidFrame.Play();
                    }
                }
                else
                {
                    VidFrame.Stop();
                }
            }
        }
    }
}
