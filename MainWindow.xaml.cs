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
using EXIFcoordinater;


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

        private async void GetParcelAddressButton_Click(object sender, RoutedEventArgs e)
        {
            var mapPoint = await MyMapView.Editor.RequestPointAsync();

            var poolPermitUrl = "http://sampleserver6.arcgisonline.com/arcgis/rest/services/PoolPermits/FeatureServer/0";
            var queryTask = new QueryTask(new System.Uri(poolPermitUrl));
            var queryFilter = new Query(mapPoint);
            queryFilter.OutFields.Add("apn");
            queryFilter.OutFields.Add("address");

            var queryResult = await queryTask.ExecuteAsync(queryFilter);
            if (queryResult.FeatureSet.Features.Count > 0)
            {
                var resultGraphic = queryResult.FeatureSet.Features[0] as Graphic;
                ApnTextBlock.Text = resultGraphic.Attributes["apn"].ToString();
                AddressTextBlock.Text = resultGraphic.Attributes["address"].ToString();
            }
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
        private void ButtonClicked_2(object sender, RoutedEventArgs e)
        {
            var window = new ImportEXIFWindow(MyMapView);
            var result = window.ShowDialog();
        }

        // Move points
        private void ButtonClicked_3(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Move points");
        }

        // Add atributes
        private void ButtonClicked_4(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add atributes");
        }

        // Clear
        private void ButtonClicked_5(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Clear");
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
                    Graphic exifPoints = 
                        await myGraphicLayer.HitTestAsync(MyMapView, myWindowsPoint);
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
            // use the MapView's Editor to request geometry (Envelope) from the user and await the result
            var newExtent = await MyMapView.Editor.RequestShapeAsync(Esri.ArcGISRuntime.Controls.DrawShape.Rectangle);
            // set the map view extent with the Envelope
            await MyMapView.SetViewAsync(newExtent);
        }

        public async void AddShapefileButton_Click(object sender, RoutedEventArgs e)
        {
            var myShapefileHandler = new ShapefileHandler();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Shapefiles (*.shp)|*.shp";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == true)
            {
                //try
                //{
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
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                //}
            }
        }


        // CSV Export
        private void Click_ExportCSV(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            Encoding encode = Encoding.GetEncoding("UTF-8");

            Stream myStream;
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    myStream.Close();
                }
            }

            var writer = new System.IO.StreamWriter(saveFileDialog1.FileName, true, encode);

            var x = new int[] { 3, 4, 5, 6, 7 };
            var y = new int[] { 2, 1, 3, 5, 6 };
            var z = new int[] { 3, 1, 0, -3, 4 };

            for (int i = 0; i < x.Length; ++i)
            {
                writer.WriteLine("{0}, {1}, {2}", x[i], y[i], z[i]);
            }
            writer.Close();
        }


        #endregion



    }
}
