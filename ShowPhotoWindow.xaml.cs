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
        Graphic exifP;

        public Graphic PointProperty
        {
            get 
            { 
                return exifP;
            }
            set 
            { 
                if (exifP == null) exifP = value;
            }
        }

        public ShowPhotoWindow(Graphic exifPoints)
        {
            InitializeComponent();

            Graphic point = exifPoints;

            // set起動（代入）
            this.PointProperty = point;

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
            if (latlon != null)
            {
                var longitude = latlon.X;
                var latitude = latlon.Y;
                lat.Text = latitude.ToString();
                lon.Text = longitude.ToString();
                dir.Text = point.Attributes["Direction"].ToString();
                if (point.Attributes["Category"] != null)
                {
                    cat.Text = point.Attributes["Category"].ToString();
                }
            }
            else
            {
                MessageBox.Show("It does not have location of the Point.");
            }
        }

        public void Click_AddCategory(object sender, RoutedEventArgs e)
        {
            // get起動 (参照)
            var point = this.PointProperty;
            point.Attributes["Category"] = category.Text;
            cat.Text = point.Attributes["Category"].ToString();
        }
    }
}
