using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Wpf;
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

namespace FluidMapUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;

            var mapControl = new MapControl();
            var map = mapControl.Map;

            //Adding layer of open street map 
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            MapControl.Map = map;

            NavigateToAustralia();
        }

        private async void NavigateToAustralia()
        {
            await Task.Delay(500);

            var target = new MPoint(133.7751, -25.2744); //Australia

            //OSM uses spherical mercator coordinates. So transform the long lat coordinates to the spherical mercator
            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(target.X, target.Y).ToMPoint();

            var map = MapControl.Map;
            var navigator = map.Navigator;

            navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, navigator.Resolutions[5], duration: 2000);
        }
    }
}
