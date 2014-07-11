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


namespace WpfApplication3
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
    }
}
