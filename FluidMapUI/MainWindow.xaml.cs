using ExCSS;
using HarfBuzzSharp;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Wpf;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate;
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
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Feature = NetTopologySuite.Features.Feature;
using IFeature = Mapsui.IFeature;
using Pen = Mapsui.Styles.Pen;
using Polygon = NetTopologySuite.Geometries.Polygon;

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

            DrawPolygon();
        } 

        private void DrawPolygon()
        {
            //defining polygon
            var vectorStyle = new VectorStyle
            {
                Fill = new Brush
                {
                    FillStyle = FillStyle.Solid,
                    Color = Color.FromArgb(18, 255, 0, 0)
                },
                Outline = new Pen
                {
                    Color = Color.Red,
                    Width = 2.0,
                    PenStyle = PenStyle.Solid
                }
            };

            //static australian coordinates
            var polygonCoordinates = new List<Coordinate>
            {
                new Coordinate(113.338953, -43.634597), // Southwest near Augusta, WA
                new Coordinate(115.339774, -34.350983), // Near Perth, WA
                new Coordinate(123.579131, -30.221319), // Northwest near Broome, WA
                new Coordinate(130.856739, -12.425845), // Northern tip near Darwin, NT
                new Coordinate(137.854808, -15.996023), // Northern Gulf near Nhulunbuy, NT
                new Coordinate(141.001484, -16.333675), // Northeast near the Gulf of Carpentaria, QLD
                new Coordinate(144.676123, -14.503222), // Eastern Cape York Peninsula, QLD
                new Coordinate(153.638464, -28.176644), // East coast near Brisbane, QLD
                new Coordinate(153.028093, -32.012175), // Southeast near Newcastle, NSW
                new Coordinate(150.794713, -35.358225), // South coast near Batemans Bay, NSW
                new Coordinate(146.912724, -39.233228), // South coast near Wilsons Promontory, VIC
                new Coordinate(141.153835, -38.473678), // Southeast near Mount Gambier, SA
                new Coordinate(129.459159, -32.186216), // Southern coast near Eucla, WA
                new Coordinate(113.338953, -43.634597), // Closing the polygon back to the start point
            };

            //Transform the coordinates to the spherical mercator projection

            for (int i = 0; i < polygonCoordinates.Count; i++)
            {
                var (x, y) = SphericalMercator.FromLonLat(polygonCoordinates[i].X, polygonCoordinates[i].Y);
                polygonCoordinates[i] = new Coordinate(x, y);
            }

            //Create the linear ring for the polygons outer boundry
            LinearRing vertices = new LinearRing(polygonCoordinates.ToArray());

            var polygonFeature = new GeometryFeature()
            {
                Geometry = new Polygon(vertices, null, GeometryFactory.Default),
                //Styles = { vectorStyle },
            };

            polygonFeature.Styles.Add(vectorStyle);

            //Add the feature to a memory layer
            var memoryLayer = new MemoryLayer
            {
                Features = new List<IFeature> { polygonFeature },
                Name = "Polygon Layer",
                Style = null
            };

            //Add the layer to the map
            MapControl.Map.Layers.Add(memoryLayer);
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
