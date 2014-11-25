using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EXIFcoordinator;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Geometry;

namespace EXIFcoordinator
{
    /// <summary>
    /// Interaction logic for ShowPhotoWindow.xaml
    /// </summary>
    public partial class ShowPhotoWindow : Window
    {
        public ShowPhotoWindow(Esri.ArcGISRuntime.Layers.Graphic exifPoints)
        {
            Graphic point = exifPoints; 
            InitializeComponent();
            // タイトルにファイル名を表示
            filename.Text = System.IO.Path.GetFileName(point.Attributes["Path"].ToString());
            // jpgを表示
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(point.Attributes["Path"].ToString());
            bitmap.EndInit();
            pointimage.Source = bitmap;
            // 緯度・経度・方角を表示
            var latlon = point.Geometry as MapPoint;
            if(latlon != null)
            {
                var longitude = latlon.X;
                var latitude = latlon.Y;
                lat.Text = latitude.ToString();
                lon.Text = longitude.ToString();
                dir.Text = point.Attributes["Direction"].ToString();
            }
            else
            {
                MessageBox.Show("It does not have location of the Point.");
            }
        }

        private void Click_Next(object sender, RoutedEventArgs e)
        {
            //GraphicsLayer graphicslayer = new GraphicsLayer();
            //var x = graphicslayer.ID; 
        }
    }
}
