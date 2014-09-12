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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Tasks.Query;
using Esri.ArcGISRuntime.Geometry;
using System.Globalization;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using ShapefileHandler;
using Microsoft.Win32;
using Esri.ArcGISRuntime.LocalServices;


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

        // Export
        private void ButtonClicked_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export File");
        }

        // Import
        private void ButtonClicked_2(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import EXIF");
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
                try
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
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        #endregion

    }
}
