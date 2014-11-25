using Microsoft.Win32;
using System;
using System.Data;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using swc = System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;


namespace EXIFcoordinator
{
    /// <summary>
    /// Interaction logic for ImportEXIFWindow.xaml
    /// </summary>
    public partial class ImportEXIFWindow : Window
    {
        Esri.ArcGISRuntime.Controls.MapView myMapView;
        public ImportEXIFWindow(Esri.ArcGISRuntime.Controls.MapView mapview)
        {
            myMapView = mapview;
            InitializeComponent();
        }

        private void Click_OK(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        // Browser1起動、テキストボックス1にパスを入力。
        private void Click_Browse(object sender, RoutedEventArgs e)
        {
            // ファイル選択
            //System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            //openFileDialog1.Filter = "JPEG files (*.jpg)|*.jpg";
            //openFileDialog1.RestoreDirectory = true;
            //openFileDialog1.Multiselect = true;

            //if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    path1.Text = openFileDialog1.FileName;
            //}

            // ディレクトリ選択
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult dr = fbd.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                path1.Text = fbd.SelectedPath;
            }
        }

        public double ByteToDegree(byte[] gpsLocationRef, byte[] gpsLocation)
        {
            //direction
            int sign = 0;
            string value_lon1 = System.Text.Encoding.ASCII.GetString(gpsLocationRef);
            value_lon1 = value_lon1.Trim(new char[] { '\0' });
            if (value_lon1 == "E") { sign = 1; }
            else if (value_lon1 == "W") { sign = -1; }
            else if (value_lon1 == "N") { sign = 1; }
            else if (value_lon1 == "S") { sign = -1; }
            //longitude
            UInt32 deg_numerator = BitConverter.ToUInt32(gpsLocation, 0);
            UInt32 deg_denominator = BitConverter.ToUInt32(gpsLocation, 4);
            UInt32 min_numerator = BitConverter.ToUInt32(gpsLocation, 8);
            UInt32 min_denominator = BitConverter.ToUInt32(gpsLocation, 12);
            UInt32 sec_numerator = BitConverter.ToUInt32(gpsLocation, 16);
            UInt32 sec_denominator = BitConverter.ToUInt32(gpsLocation, 20);
            double deg = (double)deg_numerator / (double)deg_denominator;
            double min = (double)min_numerator / (double)min_denominator;
            double sec;
            if (sec_denominator == 0) { sec = 0; }
            else { sec = (double)sec_numerator / (double)sec_denominator; }
            double deg10 = sign * ((sec / 60.0 + min) / 60 + deg);
            return deg10;
        }

        public int ByteToDirection(byte[] GPSImgDirectionRef, byte[] GPSImgDirection)
        {
            //string sign = null;
            string value_dir = System.Text.Encoding.ASCII.GetString(GPSImgDirectionRef);
            value_dir = value_dir.Trim(new char[] { '\0' });
            //if (value_dir == "T") { sign = "T"; }
            //else if (value_dir == "M") { sign = "M"; }
            UInt16 dir_numerator = BitConverter.ToUInt16(GPSImgDirection, 0);
            UInt16 dir_denominator = BitConverter.ToUInt16(GPSImgDirection, 4);
            int direction = (int)dir_numerator / (int)dir_denominator;
            return direction;
        }

        public void GetGPSLocation(string filename)
        {
            // GPS Tags(ID) Reference 
            // http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/GPS.html


            GPSInfo_lat.Clear();
            GPSInfo_lon.Clear();
            GPSInfo_dir.Clear();

            try
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(filename);

                System.Drawing.Imaging.PropertyItem gpsLatitudeRef = bmp.GetPropertyItem(1);
                System.Drawing.Imaging.PropertyItem gpsLatitude = bmp.GetPropertyItem(2);
                System.Drawing.Imaging.PropertyItem gpsLongitudeRef = bmp.GetPropertyItem(3);
                System.Drawing.Imaging.PropertyItem gpsLongitude = bmp.GetPropertyItem(4);
                System.Drawing.Imaging.PropertyItem gpsImgDirectionRef = bmp.GetPropertyItem(16);
                System.Drawing.Imaging.PropertyItem gpsImgDirection = bmp.GetPropertyItem(17);
                //Display Latitude
                var lat_deg10 = ByteToDegree(gpsLatitudeRef.Value, gpsLatitude.Value);
                GPSInfo_lat.Text += string.Format("{0}", lat_deg10);
                //Display Longitude
                var lon_deg10 = ByteToDegree(gpsLongitudeRef.Value, gpsLongitude.Value);
                GPSInfo_lon.Text += string.Format("{0}", lon_deg10);
                //Display Direction
                var direction = ByteToDirection(gpsImgDirectionRef.Value, gpsImgDirection.Value);
                GPSInfo_dir.Text += string.Format("{0}", direction);

                // EXIFの緯度経度をポイントで表示
                double lat = double.Parse(GPSInfo_lat.Text);
                double lon = double.Parse(GPSInfo_lon.Text);
                //var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)myMapView.Map.Layers["MyGraphicsLayer"];
                //var myPointSymbol = (Esri.ArcGISRuntime.Symbology.SimpleMarkerSymbol)LayoutRoot.Resources["MyPointSymbol"];
                //var myGraphic = MainWindow.MappingPoints(lat, lon, direction, filename, myPointSymbol);

                var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)myMapView.Map.Layers["MyGraphicsLayer"];
                var myPictureMarkerSymbol = MainWindow.ArrowSymbol(direction);
                var myGraphic = MainWindow.MappingPoints(lat, lon, direction, filename, myPictureMarkerSymbol);
                myGraphicsLayer.Graphics.Add(myGraphic);

            }
            catch (System.Exception)
            {
                System.Windows.MessageBox.Show(
                    System.IO.Path.GetFileName(filename) + "\n does not have GPS information.");
            }

        }

        // Browser2起動、テキストボックス2にパスを入力。
        public void Click_GetGPSInfo(object sender, RoutedEventArgs e)
        {
            string pathname = path1.Text;
            string[] files = System.IO.Directory.GetFiles(pathname, "*.jpg");
            foreach (var filename in files)
            {
                GetGPSLocation(filename);
            }
        }


        // ステータスバー
        private void MakeProgressBar(object sender, RoutedEventArgs e)
        {
            sbar.Items.Clear();
            swc.TextBlock txtb = new swc.TextBlock();
            txtb.Text = "Progress of download.";
            sbar.Items.Add(txtb);
            swc.ProgressBar progressbar = new swc.ProgressBar();
            Duration duration = new Duration(TimeSpan.FromSeconds(10));
            DoubleAnimation doubleanimation = new DoubleAnimation(100.0, duration);
            progressbar.BeginAnimation(swc.ProgressBar.ValueProperty, doubleanimation);
            swc.ToolTip ttprogbar = new swc.ToolTip();
            ttprogbar.Content = "Shows the progress of a download.";
            progressbar.ToolTip = (ttprogbar);
            sbar.Items.Add(progressbar);
        }

    }
}
