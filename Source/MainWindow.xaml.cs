using System.Windows;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Media;
using System;
using System.Windows.Interop;
using AForge.Video.FFMPEG;
using System.IO;
using System.Windows.Media.Animation;
using System.Drawing.Imaging;
using System.Windows.Controls;
using gifmakercode;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {

        OpenFileDialog openFileDialog1;

        long framecount = 0;
        int framesskipped = 0;

        bool horizontalflip = false;
        bool verticalflip   = false;
        int qualitylevel    = 50;
        
        System.Windows.Media.Imaging.Rotation rotationselected = Rotation.Rotate0;
                
        public MainWindow()
        {
            InitializeComponent();

            Converttogif_btn.IsEnabled = false;
            gifcoverage_slider.IsEnabled = false;
            slider.IsEnabled = false;
        }

        private void openfile_btn_Click(object sender, RoutedEventArgs e)
        {
            // Displays an OpenFileDialog so the user can select a Cursor.
            openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Video Files(*.AVI;*.MPEG;*.MPG;*.MP4)|*.AVI;*.MPEG;*.MPG;*.MP4|All files (*.*)|*.*";
            openFileDialog1.Title = "Select a video File";
            
            if (openFileDialog1.ShowDialog() == true)
            {
                MediaTimeline tl = new MediaTimeline(new Uri(openFileDialog1.FileName.ToString()));
                
                mediaElement.Clock = tl.CreateClock(true) as MediaClock;

                mediaElement.MediaOpened += (o, ef) =>
                {
                    slider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                    mediaElement.Clock.Controller.Pause();
                };

                slider.ValueChanged += (o, ef) =>
                {
                    mediaElement.Clock.Controller.Seek(TimeSpan.FromSeconds(slider.Value), TimeSeekOrigin.BeginTime);
                };
                
                VideoFileReader videoreader;
                videoreader = new VideoFileReader();
                // open video file
                videoreader.Open(openFileDialog1.FileName.ToString());
                framecount = videoreader.FrameCount;
                textBox.Text = framecount.ToString();
                videoreader.Close();

                slider.IsEnabled = true;
                gifcoverage_slider.IsEnabled = true;
                Converttogif_btn.IsEnabled = true;

                calculateframeskip(20); // 20 % frames
            }
        }

        private void Converttogif_btn_click(object sender, RoutedEventArgs e)
        {
            VideoFileReader videoreader = new VideoFileReader();
            // open video file
            videoreader.Open(openFileDialog1.FileName.ToString());
                       

            GifBitmapEncoder thegifencoder = new GifBitmapEncoder();

            string filename = "thegiffile.gif";

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (var gif = File.OpenWrite("thegiffile.gif"))
            using (var encoder = new GifMaker(gif))           // customlibs
                                           

            for (int i = 0; i < framecount; i++)
            {
                Bitmap videoFrame = videoreader.ReadVideoFrame();
                //insert jpeg commpression here. 

                BitmapSource imagesrc = Converttobmpscr(videoFrame);

                MemoryStream ms = new MemoryStream(); //saving to memory instead of file

                JpegBitmapEncoder jpegencoder = new JpegBitmapEncoder();
                jpegencoder.FlipHorizontal = horizontalflip;
                jpegencoder.FlipVertical = verticalflip;
                jpegencoder.QualityLevel = qualitylevel;
                jpegencoder.Rotation = rotationselected;
                jpegencoder.Frames.Add(BitmapFrame.Create(imagesrc));
                jpegencoder.Save(ms);

                System.Drawing.Image compr_img = System.Drawing.Image.FromStream(ms);

                //Bitmap compr_frame = new Bitmap(compr_img);
                
                if (i % framesskipped == 0)
                {
                   //IntPtr bmp = compr_frame.GetHbitmap();
                   //BitmapSource src = Imaging.CreateBitmapSourceFromHBitmap(
                   //bmp,
                   //IntPtr.Zero,
                   //Int32Rect.Empty,
                   //BitmapSizeOptions.FromEmptyOptions());
                   //thegifencoder.Frames.Add(BitmapFrame.Create(src));

                    encoder.AddFrame(compr_img);

                }

                videoFrame.Dispose();
                jpegencoder = null;
            }
            
            videoreader.Close();

            //using (FileStream fs = new FileStream("thegiffile.gif", FileMode.Create))            // windows libs
            //{
            //    thegifencoder.Save(fs);
            //}

            thegifencoder = null;           

        }

        private void gifcoverage_slider_changedVal(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            calculateframeskip((int)e.NewValue);
        }


        public void calculateframeskip(int e)
        {
            decimal gifcoverage = 1;
            int gifcoverageroof = 0;
            int percentage = e;

            try
            {
                percentageBox.Text = percentage.ToString();
                gifcoverage = (decimal)(framecount / (framecount * ((double)percentage / 100)));
                gifcoverageroof = Convert.ToInt32(Math.Round(gifcoverage));

                f_skipBox.Text = gifcoverageroof.ToString();
            }
            catch
            {

            }

            framesskipped = gifcoverageroof;
        }

        public BitmapSource Converttobmpscr(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        private void quality_slider_changedVal(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            getslidervalues((int)e.NewValue);
        }

        public void getslidervalues(int e)
        {
            try
            {
                quality_percentage.Text = e.ToString();
                qualitylevel = e;
            }
            catch
            {

            }           
        }

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == null)
                return;           

            try
            {
                switch (radioButton.Name.ToString())
                {
                    case "rot_none":
                        rotationselected = Rotation.Rotate0;
                        break;
                    case "rot_90":
                        rotationselected = Rotation.Rotate90;
                        break;
                    case "rot_180":
                        rotationselected = Rotation.Rotate180;
                        break;
                    case "rot_270":
                        rotationselected = Rotation.Rotate270;
                        break;
                    default:
                        rotationselected = Rotation.Rotate0;
                        break;
                }
            }
            catch
            {

            }            
        }

        private void horflip_enable(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null)
                return;

            if(checkbox.IsChecked == true)
            {
                horizontalflip = true;
            }
            else
            {
                horizontalflip = false;
            }
        }

        private void vertflip_enable(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null)
                return;

            if (checkbox.IsChecked == true)
            {
                verticalflip = true;
            }
            else
            {
                verticalflip = false;
            }
        }
    }
}





