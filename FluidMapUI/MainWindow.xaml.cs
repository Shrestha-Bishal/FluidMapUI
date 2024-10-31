using ExCSS;
using HarfBuzzSharp;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.UI.Wpf;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.GeometriesGraph;
using NetTopologySuite.IO;
using NetTopologySuite.Triangulate;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        private List<PolygonCoordinate> _polygonCoordinates = [];
        private List<Coordinate> _vectorCoordinatesLonLat = [];
        private List<Coordinate> _sphericalMercatorCoordinatesLonLat = [];
        private System.Windows.Point? _mouseLeftDownPosition;

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;

            var mapControl = new MapControl();
            var map = mapControl.Map;

            //Adding layer of open street map 
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            MapControl.MouseLeftButtonDown += MapControl_MouseLeftButtonDown;
            MapControl.MouseLeftButtonUp += MapControl_MouseLeftButtonUp;
            MapControl.Map = map;

            NavigateToAustralia();
        }

        private void MapControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //getting the snapshot of the current mouse position
            _mouseLeftDownPosition = e.GetPosition(this);
        }

        private static bool IsMouseDrag(System.Windows.Point? startingPosition, System.Windows.Point? finishingPosition)
        {
            if(startingPosition != null && finishingPosition != null)
            {
                const double dragThreshold = 5.0;

                double x = finishingPosition.Value.X - startingPosition.Value.X;
                double y = finishingPosition.Value.Y - startingPosition.Value.Y;

                double distance = Math.Sqrt(x * x + y * y);

                return distance > dragThreshold;
            }

            return false;
        }

        private void MapControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var map = MapControl.Map;
            var mousePosition =  e.GetPosition(this); //Get the current mouse position

            var isMouseDrag = IsMouseDrag(_mouseLeftDownPosition, mousePosition);

            if (isMouseDrag)
                return;

            var navigator = map.Navigator;
           
            //conver the pixel position to the world coordinates (meters)
            var worldPosition = navigator.Viewport.ScreenToWorld(mousePosition.X, mousePosition.Y);

            // Project world coordinates to latitude and longitude
            var lonLat = SphericalMercator.ToLonLat(worldPosition);
            AddCoordinates(lonLat);

            DrawCoordinates();

            if (IsPolygonComplete)
            {
                var firstCoord = _vectorCoordinatesLonLat.First();
                AddCoordinates(firstCoord.ToMPoint());
                DrawPolygon();

                _polygonCoordinates.Add(
                    new(_sphericalMercatorCoordinatesLonLat.ToList(), _vectorCoordinatesLonLat.ToList()
                ));

                // clearing for the next polygon coordinates
                _sphericalMercatorCoordinatesLonLat.Clear(); 
                _vectorCoordinatesLonLat.Clear();
            }

            Console.WriteLine($"The mouse position is {mousePosition} with the longitude: {lonLat.X} and latitude {lonLat.Y}");
        }

        private bool IsPolygonComplete
        {
            get
            {
                int numCoords = _sphericalMercatorCoordinatesLonLat.Count;
                var resolution = MapControl.Map.Navigator.Viewport.Resolution;
                Coordinate? firstCoord = _sphericalMercatorCoordinatesLonLat.FirstOrDefault();
                Coordinate? lastCoord = _sphericalMercatorCoordinatesLonLat.LastOrDefault();
                double toleranceLevel = GetTolerableDistance();

                static bool IsNearStartingPoint(
                    Coordinate? firstCoord, 
                    Coordinate? lastCoord,
                    double tolerance)
                {
                    double distance = Math.Sqrt(Math.Pow(lastCoord.X - firstCoord.X, 2) + Math.Pow(lastCoord.Y - firstCoord.Y, 2));
                    return distance <= tolerance;
                }


                if (numCoords < 4) return false; //there should be at least 4 coordinates to close the ring
                if (!IsNearStartingPoint(firstCoord, lastCoord, toleranceLevel)) return false; // the last coordine should be within the tolerance level of the starting point
                //if (!firstCoord.Equals(lastCoord)) return false; //first coordinate should be equal to last to close the ring
                return true;
            }
        }

        private double GetTolerableDistance()
        {
            var resolution = MapControl.Map.Navigator.Viewport.Resolution;
            double pointSymbolScale = 0.4;
            double pixelResolution = 32;

            double radius = pointSymbolScale * pixelResolution * resolution;

            return radius;
        }

        private void AddCoordinates(MPoint lonLat)
        {
            _vectorCoordinatesLonLat.Add(new Coordinate(lonLat.X, lonLat.Y));

            /* Transforming the coordinates to the spherical mercator projection
             * Transforms the lat and long from spherical representation of the earth into a flat dimension plane
             */

            var (x, y) = SphericalMercator.FromLonLat(lonLat.X, lonLat.Y);
            _sphericalMercatorCoordinatesLonLat.Add(new Coordinate(x, y));
        }

        private void DrawPolygon()
        {
            try
            {
                //defining style
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

                var linearRing = new LinearRing([.. _sphericalMercatorCoordinatesLonLat]);
                var polygonFeature = new GeometryFeature()
                {
                    Geometry = new Polygon(linearRing),
                    Styles = { vectorStyle }
                };

                polygonFeature.Styles.Add(vectorStyle);

                var memoryLayer = new MemoryLayer()
                {
                    Features = new List<IFeature> { polygonFeature },
                    Name = "Polygon layer",
                    Style = null
                };

                //Add the layer 
                var map = MapControl.Map;
                map.Layers.Add(memoryLayer); //adding the new polygon layer
            }catch(Exception ex)
            {
                MessageBox.Show("An error occured while tracing a polygon. \n", ex.Message);
            }
        }

        private void DrawStartMarker()
        {
            var firstCoord = _sphericalMercatorCoordinatesLonLat.FirstOrDefault();
            
            if (firstCoord == default)
                return;

            var pointStyle = new SymbolStyle
            {
                SymbolScale = 0.4,
                Fill = new Brush(Color.Red),
                Outline = new Pen(Color.Red, 1)
            };

            var pointFeature = new GeometryFeature
            {
                Geometry = new NetTopologySuite.Geometries.Point(firstCoord.X, firstCoord.Y),
                Styles = { pointStyle }
            };

            var memoryLayer = new MemoryLayer
            {
                Features = new List<IFeature> { pointFeature },
                Name = "Starting point feature",
                Style = null,
            };

            MapControl.Map.Layers.Add(memoryLayer);
        }

        private void DrawCoordinates()
        {
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

            int numCoords = _sphericalMercatorCoordinatesLonLat.Count;

            if (numCoords < 2)
            {
                DrawStartMarker();
                return;
            }

            Coordinate vector1 = _sphericalMercatorCoordinatesLonLat[numCoords - 2];
            Coordinate vector2 = _sphericalMercatorCoordinatesLonLat[numCoords - 1];

            var lineFeature = new GeometryFeature()
            {
                Geometry = new LineString([vector1, vector2]),
            };

            lineFeature.Styles.Add(vectorStyle);

            //Add the feature to a memory layer
            var memoryLayer = new MemoryLayer
            {
                Features = new List<IFeature> { lineFeature },
                Name = "Layer",
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


        public class PolygonCoordinate
        {
            public IEnumerable<Coordinate> SphericalMercatorCoordinatesLonLat { get; }
            public IEnumerable<Coordinate> VectorCoordinatesLonLat { get; }
            public PolygonCoordinate(
                IEnumerable<Coordinate> sphericalMercatorCoordinatesLonLat,
                IEnumerable<Coordinate> vectorCoordinateLonLat)
            {
                SphericalMercatorCoordinatesLonLat = sphericalMercatorCoordinatesLonLat;
                VectorCoordinatesLonLat = vectorCoordinateLonLat;
            }
        };
    }
}


