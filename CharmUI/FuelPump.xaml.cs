using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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
        readonly double FuelPricePremium = 3.599;
        readonly double FuelPriceRegular = 3.299;
        readonly double FuelPriceDiesel = 2.899;
        DispatcherTimer PumpDisplayTimer = new DispatcherTimer();
        DispatcherTimer SlideshowTimer = new DispatcherTimer();
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
        readonly string ImageKeyword = "(IMG)";
        readonly string SlideShowString = ">>>   SLIDE SHOW   <<<";
        bool SlidesFound = false;
        int SlideShowNextIndex = 0;
        bool SlideShowON = false;
        StorageFolder MediaFolder;
        readonly string MediaIndexFile = "Media.lst";
        readonly string MediaFolderToken = "PickedMediaFolder";

        struct sUri
        {
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
        List<sUri> SlideList = new List<sUri>();

        struct sVid
        {
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
            new sVid ("854x480 (480p)", 854, 480),
            new sVid ("800x600", 800, 600),
            new sVid ("1280x720 (HD-720p)", 1280, 720),
            new sVid ("1920x1080 (FHD-1080p)", 1920, 1080),
        };

        public FuelPump()
        {
            this.InitializeComponent();

            PremiumPrice.Text = $"{FuelPricePremium:F3}";
            RegularPrice.Text = $"{FuelPriceRegular:F3}";
            DieselPrice.Text = $"{FuelPriceDiesel:F3}";

            ResetPump();
            PumpDisplayTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            PumpDisplayTimer.Tick += PumpDisplay_TimerTick;

            SlideshowTimer.Interval = new TimeSpan(0, 0, 0, 0, 5000);
            SlideshowTimer.Tick += Slideshow_TimerTick;

            DisableMediaControls();
        }

        async Task InitPageMediaAsync(StorageFolder vidIxFolder = null)
        {
            StorageFolder _mediaFolder = vidIxFolder;

            if (_mediaFolder == null)
            {
                string faToken = ApplicationData.Current.LocalSettings.Values[MediaFolderToken] as string;
                if (faToken != null)
                {
                    try
                    {
                        _mediaFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(faToken);
                    }
                    catch (Exception e)
                    {
                        // the folder previously stored must have been deleted, let's clean up
                        StorageApplicationPermissions.FutureAccessList.Clear();
                        ApplicationData.Current.LocalSettings.Values[MediaFolderToken] = null;
                    }
                }
            }

            if (_mediaFolder == null)
                _mediaFolder = KnownFolders.VideosLibrary;

            StorageFile vidIxFile;
            try
            {
                vidIxFile = await _mediaFolder.GetFileAsync(MediaIndexFile);
            }
            catch (Exception e)
            {
                DisableMediaControls();
                MediaErrorMessage("Error opening " + MediaIndexFile + e.ToString());
                return;
            }

            // NOTE - no validation on content of the media.lst file content!!!

            // initialize the media list from file
            MediaList.Clear();
            SlideList.Clear();
            var lines = await FileIO.ReadLinesAsync(vidIxFile);
            foreach (var line in lines)
            {
                string[] words = line.Split(',');
                string display = words[0];
                int width = Int32.Parse(words[1]);
                int height = Int32.Parse(words[2]);
                string file = words[3];
                if (display.Equals(ImageKeyword))
                {
                    SlideList.Add(new sUri("", width, height, file));
                }
                else
                {
                    MediaList.Add(new sUri(display, width, height, file));
                }
            }

            VidList.Items.Clear();
            SlidesFound = false;
            if (SlideList.Count > 0)
            {
                VidList.Items.Add(SlideShowString);
                SlidesFound = true;
            }
            foreach (var l in MediaList)
                VidList.Items.Add(l.Display);
            VidList.SelectedIndex = 0;

            VidSize.Items.Clear();
            foreach (var s in VidFrameSize)
                VidSize.Items.Add(s.Display);
            VidSize.SelectedIndex = 0;

            MediaFolder = _mediaFolder;
            EnableMediaControls();

            return;
        }

        async Task NewMediaFolderAsync()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await InitPageMediaAsync(folder);
                if (MediaEnabled)
                {
                    string faToken = ApplicationData.Current.LocalSettings.Values[MediaFolderToken] as string;

                    // get faToken from params

                    if (faToken == null)
                    {
                        faToken = StorageApplicationPermissions.FutureAccessList.Add(folder);
                        ApplicationData.Current.LocalSettings.Values[MediaFolderToken] = faToken;
                    }
                    else
                    {
                        StorageApplicationPermissions.FutureAccessList.AddOrReplace(faToken, folder);
                    }
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

        async void FuelPump_Loaded(object sender, RoutedEventArgs e)
        {
            await InitPageMediaAsync();
        }

        void PumpDisplay_TimerTick(object sender, object /*EventArgs*/ e)
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

                ScrollContent.ChangeView(ScrollPos, null, null);
                if (PosZeroDelay > 0)
                {
                    PosZeroDelay--;
                }
                else if (ScrollContent.HorizontalOffset < (ScrollPos - 100))
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
        void Slideshow_TimerTick(object sender, object /*EventArgs*/ e)
        {
            ShowNextImage();
        }

        private void BtnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
        private async void BtnMediaSrc_Click(object sender, RoutedEventArgs e)
        {
            await NewMediaFolderAsync();
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
                    PumpDisplayTimer.Start();
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
                PremiumPrice.Visibility = Visibility.Visible;
            }
            else
            {
                BtnFuelPremium.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xff, 0x03, 0x03));
                PremiumPrice.Visibility = Visibility.Collapsed;
            }

            if (ft == FuelTypes.FuelRegular)
            {
                BtnFuelRegular.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0xff, 0x03, 0x03));
                FuelPrice = 3.299;
                RegularPrice.Visibility = Visibility.Visible;
            }
            else
            {
                BtnFuelRegular.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x80, 0xff, 0x03, 0x03));
                RegularPrice.Visibility = Visibility.Collapsed;
            }

            if (ft == FuelTypes.FuelDiesel)
            {
                BtnFuelDiesel.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0x0e, 0xff, 0x03));
                FuelPrice = 2.899;
                DieselPrice.Visibility = Visibility.Visible;
            }
            else
            {
                BtnFuelDiesel.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 0x0e, 0xff, 0x03));
                DieselPrice.Visibility = Visibility.Collapsed;
            }

            if (ft == FuelTypes.FuelNotSelected)
            {
                PremiumPrice.Visibility = Visibility.Visible;
                RegularPrice.Visibility = Visibility.Visible;
                DieselPrice.Visibility = Visibility.Visible;
            }
            else
            {
                PumpState = PumpStates.StateFuelSelected;
                TxtBanner.Text = StrStartFueling;
                BtnStartStop.Content = StrStartFueling;
                BtnStartStop.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 228, 146, 73));
            }
        }

        private void ResetPump()
        {
            PumpDisplayTimer.Stop();

            FuelSelection(FuelTypes.FuelNotSelected);
            SaleString.Text = "00.00";
            GallonsString.Text = "00.000";
            TxtBanner.Text = StrSelectFuel;
            BtnStartStop.Content = StrSelectFuel;
            BtnStartStop.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 228, 146, 73));

            FuelFilled = 0;

            PumpState = PumpStates.StateInitial;
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
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
                        if (SlidesFound && VidList.SelectedIndex == 0)
                        {
                            ImageFrame.Visibility = Visibility.Visible;
                            VidFrame.Visibility = Visibility.Collapsed;

                            // we are starting a slide show
                            ShowNextImage();
                            SlideShowON = true;
                            SlideshowTimer.Start();
                        }
                        else
                        {
                            ImageFrame.Visibility = Visibility.Collapsed;
                            VidFrame.Visibility = Visibility.Visible;

                            // we are playing a video
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

                            try
                            {
                                string fileName = MediaList[VidList.SelectedIndex].File;
                                StorageFile mediaFile = await MediaFolder.GetFileAsync(fileName);
                                var stream = await mediaFile.OpenAsync(FileAccessMode.Read);
                                VidFrame.SetSource(stream, mediaFile.ContentType);
                                VidFrame.Play();
                            }
                            catch (FileNotFoundException)
                            {
                                MediaErrorMessage("Media file could not be found");
                            }
                            catch (UnauthorizedAccessException)
                            {
                                MediaErrorMessage("Access to media file denied");
                            }
                            catch (Exception ex)
                            {
                                MediaErrorMessage("Unexpected error: " + ex.ToString());
                            }
                        }
                    }
                }
                else
                {
                    if (SlideShowON)
                    {
                        SlideShowON = false;
                        SlideshowTimer.Stop();
                    }
                    VidFrame.Stop();
                }
            }
        }

        private async void ShowNextImage()
        {
            string fileName = SlideList[SlideShowNextIndex].File;
            if (++SlideShowNextIndex >= SlideList.Count)
                SlideShowNextIndex = 0;

            StorageFile picFile = await MediaFolder.GetFileAsync(fileName);

            // Open a stream for the selected file.
            // The 'using' block ensures the stream is disposed
            // after the image is loaded.
            using (Windows.Storage.Streams.IRandomAccessStream fileStream =
                await picFile.OpenAsync(FileAccessMode.Read))
            {
                // Set the image source to the selected bitmap.
                BitmapImage bitmapImage = new BitmapImage();

                bitmapImage.SetSource(fileStream);
                ImageFrame.Source = bitmapImage;
            }
        }

        private void ImageFailedEventHandler(object sender, ExceptionRoutedEventArgs e)
        {
            MediaErrorMessage(e.ToString());
        }
        private void ImageOpenedEventHandler(object sender, RoutedEventArgs e)
        {
            MediaErrorMessage(e.ToString());
        }
    }
}
