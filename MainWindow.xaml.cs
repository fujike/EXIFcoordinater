using System;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Tasks.Query;
using Esri.ArcGISRuntime.Geometry;
using System.Globalization;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using EXIFcoordinator;
using Microsoft.Win32;
using Esri.ArcGISRuntime.LocalServices;
using Esri.ArcGISRuntime.Symbology;



namespace EXIFcoordinator
{    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            var sel = combo.SelectedItem as ComboBoxItem;
            if (sel.Tag == null) { return; }

            // Find and remove the current basemap layer from the map
            if (MyMap == null) { return; }
            var oldBasemap = MyMap.Layers["BaseMap"];
            MyMap.Layers.Remove(oldBasemap);

            // Create a new basemap layer
            var newBasemap = new Esri.ArcGISRuntime.Layers.ArcGISTiledMapServiceLayer();

            // Set the ServiceUri with the url defined for the ComboBoxItem's Tag
            newBasemap.ServiceUri = sel.Tag.ToString();

            // Give the layer the same ID so it can still be found with the code above
            newBasemap.ID = "BaseMap";

            // Insert the new basemap layer as the first (bottom) layer in the map
            MyMap.Layers.Insert(0, newBasemap);
        }

        #region Event Handler for Buttons

        // Import
        private void Import_Photos(object sender, RoutedEventArgs e)
        {
            var window = new ImportEXIFWindow(MyMapView);
            var result = window.ShowDialog();
        }

        // Move points
        private async void MovePoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)this.MyMapView.Map.Layers["MyGraphicsLayer"];
                // Get the Editor associated with the MapView. The Editor enables drawing and editing graphic objects.
                Esri.ArcGISRuntime.Controls.Editor myEditor = MyMapView.Editor;
                Esri.ArcGISRuntime.Geometry.MapPoint myPoint = await myEditor.RequestPointAsync();
                // Translate the MapPoint into Microsoft Point object.
                System.Windows.Point myWindowsPoint = MyMapView.LocationToScreen(myPoint);
                //Esri.ArcGISRuntime.Data.FeatureTable myFeatureTable = myGraphicLayer.FeatureTable;
                Graphic exifPoints = await myGraphicsLayer.HitTestAsync(MyMapView, myWindowsPoint);

                if (myPoint != null && exifPoints != null)
                {
                    NewPoint(exifPoints);
                }
            }
            catch
            {
                MessageBox.Show("Try again!");
            }
        }


        public async void NewPoint(Graphic exifPoints)
        {
            var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)this.MyMapView.Map.Layers["MyGraphicsLayer"];
            Esri.ArcGISRuntime.Controls.Editor myEditor = MyMapView.Editor;

            var wgs84 = new Esri.ArcGISRuntime.Geometry.SpatialReference(4326);
            var newPoint_webMercator = await myEditor.RequestPointAsync();
            // 座標系変換 WebMercator to WGS84
            var newPoint_wgs84 = Esri.ArcGISRuntime.Geometry.GeometryEngine.Project(newPoint_webMercator, wgs84);

            var latlon_new = newPoint_wgs84 as MapPoint;
            var latlon = exifPoints.Geometry as MapPoint;
            latlon.X = latlon_new.X;
            latlon.Y = latlon_new.Y;
            var lon = latlon.X;
            var lat = latlon.Y;

            var filename = exifPoints.Attributes["Path"].ToString();
            int dir = int.Parse(exifPoints.Attributes["Direction"].ToString());
            var myPictureMarkerSymbol = ArrowSymbol(dir);
            var myGraphic = MainWindow.MappingPoints(lat, lon, dir, filename, myPictureMarkerSymbol);
        }





        // Clear
        public void Clear_Click(object sender, RoutedEventArgs e)
        {
            var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)this.MyMapView.Map.Layers["MyGraphicsLayer"];
            myGraphicsLayer.Graphics.Clear();

            // 2回目入力のShapefileを消すことができない。要検討。
            //var dynlayer = (Esri.ArcGISRuntime.Layers.ArcGISDynamicMapServiceLayer)this.MyMapView.Map.Layers["MyShapefileLayer"];
            //dynlayer.LayerDrawingOptions.Clear();
        }


        // Show Photo
        private async void ShowPhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the FeatureLayer from the Map.
                Esri.ArcGISRuntime.Controls.Map myMap = MyMapView.Map;
                Esri.ArcGISRuntime.Layers.LayerCollection myLayerCollection = myMap.Layers;
                Esri.ArcGISRuntime.Layers.GraphicsLayer myGraphicLayer 
                    = (Esri.ArcGISRuntime.Layers.GraphicsLayer)myLayerCollection[1];

                // Get the Editor associated with the MapView. The Editor enables drawing and editing graphic objects.
                Esri.ArcGISRuntime.Controls.Editor myEditor = MyMapView.Editor;

                // Get the MapPoint that the user tapped/clicked on the Map. Execution of the code stops here until the user is done drawing the MapPoint. 
                Esri.ArcGISRuntime.Geometry.MapPoint myPoint = await myEditor.RequestPointAsync();
                
                if (myPoint != null)
                {
                    // Translate the MapPoint into Microsoft Point object.
                    System.Windows.Point myWindowsPoint = MyMapView.LocationToScreen(myPoint);
                    //Esri.ArcGISRuntime.Data.FeatureTable myFeatureTable = myGraphicLayer.FeatureTable;
                    Graphic exifPoints = await myGraphicLayer.HitTestAsync(MyMapView, myWindowsPoint);

                    if (exifPoints != null)
                    {
                        var window = new ShowPhotoWindow(exifPoints);
                        var result = window.ShowDialog();
                    }
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                // This exception occurred because the user has already clicked the button but has not clicked/tapped a MapPoint on the Map yet.
                System.Windows.MessageBox.Show("Click or tap on over a feature in the map to select it.");
            }
            catch (System.Exception ex)
            {
                // We had some kind of issue. Display to the user so it can be corrected.
                System.Windows.MessageBox.Show(ex.Message);
            }
        }


        // Zoom Buton 
        private async void ZoomToEnvelopeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // use the MapView's Editor to request geometry (Envelope) from the user and await the result
                var newExtent = await MyMapView.Editor.RequestShapeAsync(Esri.ArcGISRuntime.Controls.DrawShape.Rectangle);
                // set the map view extent with the Envelope
                await MyMapView.SetViewAsync(newExtent);
            }
            catch
            {
                MessageBox.Show("Try again!");
            }
        }

        public async void AddShapefileButton_Click(object sender, RoutedEventArgs e)
        {
            var myShapefileHandler = new ShapefileHandler();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Shapefiles (*.shp)|*.shp";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    var fileNames = new List<string>();
                    foreach (var item in openFileDialog.SafeFileNames)
                    {
                        fileNames.Add(System.IO.Path.GetFileNameWithoutExtension(item));
                    }

                    // Call the add dataset method with workspace type, parent directory path and file names (without extensions)
                    var myAddFileDatasetToDynamicMapServiceLayer = new ShapefileHandler();
                    var dynLayer = await myAddFileDatasetToDynamicMapServiceLayer.AddFileDatasetToDynamicMapServiceLayer(WorkspaceFactoryType.Shapefile,
                        System.IO.Path.GetDirectoryName(openFileDialog.FileName), fileNames);

                    // Add the dynamic map service layer to the map
                    if (dynLayer != null)
                    {
                        MyMapView.Map.Layers.Add(dynLayer);

                    }

                }

            }
            catch
            {
                MessageBox.Show("This Shapefile cannot be read.");
            }
        }


        // Export CSV
        private void Click_ExportCSV(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            Encoding utf8 = Encoding.GetEncoding("UTF-8");

            Stream myStream;
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    myStream.Close();
                }

                var writer = new System.IO.StreamWriter(saveFileDialog1.FileName, true, utf8);

                var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)this.MyMapView.Map.Layers["MyGraphicsLayer"];

                foreach (var item in myGraphicsLayer.Graphics)
                {
                    Graphic point = item;
                    //string name = System.IO.Path.GetFileName(point.Attributes["Path"].ToString());
                    string name = point.Attributes["Path"].ToString();
                    string direction = point.Attributes["Direction"].ToString();
                    var latlon = point.Geometry as MapPoint;
                    var longitude = latlon.X;
                    var latitude = latlon.Y;
                    if (point.Attributes["Category"] == null)
                    {
                        writer.WriteLine("{0}, {1}, {2}, {3}, ", name, latitude, longitude, direction);
                    }
                    else
                    {
                        string category = point.Attributes["Category"].ToString();
                        writer.WriteLine("{0}, {1}, {2}, {3}, {4}", name, latitude, longitude, direction, category);
                    }
                }
                writer.Close();
            }
        }

        // Import CSV
        private void Click_InportCSV(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.Filter = "csv files (*.csv)|*.csv";
            openFileDialog1.RestoreDirectory = true;
            Encoding shiftjis = Encoding.GetEncoding("Shift_JIS");
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string text = System.IO.File.ReadAllText(openFileDialog1.FileName, shiftjis);
                    string[] rows = text.Trim().Replace("\r", "").Split('\n');
                    string directorypath = System.IO.Path.GetDirectoryName(openFileDialog1.FileName);

                    for (int i = 0; i < rows.Length; i++)
                    {
                        string[] columns = rows[i].Split(',');
                        string filename = columns[0];
                        string latitude = columns[1];
                        string longitude = columns[2];
                        string direction = columns[3];

                        // EXIFの緯度経度をポイントで表示
                        double lat = double.Parse(columns[1]);
                        double lon = double.Parse(columns[2]);
                        int dir = int.Parse(columns[3]);
                        var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)this.MyMapView.Map.Layers["MyGraphicsLayer"];
                        var myPictureMarkerSymbol = ArrowSymbol(dir);
                        if (columns.Length == 4)
                        {
                            var myGraphic = MainWindow.MappingPoints(lat, lon, dir, filename, myPictureMarkerSymbol);
                            myGraphicsLayer.Graphics.Add(myGraphic);
                        }
                        else if(columns.Length == 5)
                        {
                            string category = columns[4];
                            var myGraphic = MainWindow.MappingPoints(category, lat, lon, dir, filename, myPictureMarkerSymbol);
                            myGraphicsLayer.Graphics.Add(myGraphic);
                        }
                        else
                        {
                            MessageBox.Show(System.IO.Path.GetFileName(filename) + ":\n Number of column is not correct.\n Styles ONLY composed of 4 or 5 columns are supported.");
                            myGraphicsLayer.Graphics.Clear();
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Could not read file. It was written in the wrong form.", "Error");
                    var myGraphicsLayer = (Esri.ArcGISRuntime.Layers.GraphicsLayer)this.MyMapView.Map.Layers["MyGraphicsLayer"];
                    myGraphicsLayer.Graphics.Clear();
                }
            }
        }

        #endregion
        public static PictureMarkerSymbol ArrowSymbol(int dir)
        {
            System.Uri myPictureUri = new System.Uri("Resources/10_arrow.png", UriKind.Relative);
            var myPictureMarkerSymbol = new Esri.ArcGISRuntime.Symbology.PictureMarkerSymbol();
            myPictureMarkerSymbol.SetSourceAsync(myPictureUri);
            myPictureMarkerSymbol.Height = 40;
            myPictureMarkerSymbol.Width = 20;
            myPictureMarkerSymbol.Angle = dir;
            return myPictureMarkerSymbol;
        }


        public static Graphic MappingPoints(double lat, double lon, int direction, string filename, PictureMarkerSymbol marker)
        {

            var sref = Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84;
            var myGeometry = new Esri.ArcGISRuntime.Geometry.MapPoint(lon, lat, sref);
            var myGraphic = new Esri.ArcGISRuntime.Layers.Graphic(myGeometry, marker);
            myGraphic.Attributes["Path"] = filename;
            myGraphic.Attributes["Direction"] = direction;
            myGraphic.Attributes["Category"] = null;
            return myGraphic;
        }

        public static Graphic MappingPoints(string category, double lat, double lon, int direction, string filename, PictureMarkerSymbol marker)
        {

            var sref = Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84;
            var myGeometry = new Esri.ArcGISRuntime.Geometry.MapPoint(lon, lat, sref);
            var myGraphic = new Esri.ArcGISRuntime.Layers.Graphic(myGeometry, marker);
            myGraphic.Attributes["Path"] = filename;
            myGraphic.Attributes["Direction"] = direction;
            myGraphic.Attributes["Category"] = category;
            return myGraphic;
        }

    }

        
}
