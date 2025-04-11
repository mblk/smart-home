using ScottPlot;
using SmartHome.Infrastructure.Devices;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartHome.PlotUtil.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly PlotData _plotData = new PlotData();

    public MainWindow()
    {
        InitializeComponent();

        base.Loaded += MainWindow_Loaded;

        // TODO make nice config ui, hot reloading, etc
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Loaded");

        var plot1 = WpfPlot1.Plot;
        var multiplot = new Multiplot(plot1);
        var plot2 = multiplot.AddPlot();
        var plot3 = multiplot.AddPlot();

        multiplot.SharedAxes.ShareX([plot1, plot2, plot3]);

        {
            var f = plot1.Add.Function(x => _plotData.GetSunState(x).AltDeg);
            f.LegendText = "alt deg";
        }
        {
            //var f = plot1.Add.Function(x => _plotData.GetSunState(x).DirDeg);
            //f.LegendText = "dir deg";
        }

        {
            var f = plot2.Add.Function(x => _plotData.GetRoomLightEstimate(x).DirectLightFactor);
            f.LegendText = "direct factor";
        }
        {
            var f = plot2.Add.Function(x => _plotData.GetRoomLightEstimate(x).DiffuseLightFactor);
            f.LegendText = "diffuse factor";
        }
        {
            var f = plot2.Add.Function(x => _plotData.GetRoomLightEstimate(x).TotalLightFactor);
            f.LegendText = "total factor";
        }

        {
            var f = plot3.Add.Function(x => _plotData.GetRoomLightEstimate(x).Illuminance);
            f.LegendText = "illuminance Lx";
        }
        {
            var f = plot3.Add.Function(x => _plotData.GetRoomLightEstimate(x).Irradiance);
            f.LegendText = "irradiance W/m^2";
        }

        WpfPlot1.Multiplot = multiplot;

        plot1.Axes.SetLimits(0, 24, -90, 90);
        plot2.Axes.SetLimitsY(-0.1, 1.1);
        plot3.Axes.SetLimitsY(-100, 10_000);

        WpfPlot1.Refresh();
    }
}

public class PlotData
{
    private readonly DateTime _startDateTime = new DateTime(2025, 4, 10, 0, 0, 0);

    private readonly SunSensorConfig _sunSensorConfig = new SunSensorConfig(49.5979564034886, 10.958495656572843);

    private readonly RoomLightConfig _roomLightConfig = new RoomLightConfig(90.0, 2.0, 0.6);

    public PlotData()
    {
    }

    private DateTime GetDateTime(double x)
    {
        return _startDateTime.AddHours(x);
    }

    public SunState GetSunState(double x)
    {
        var dt = GetDateTime(x);

        var sunState = SunSensor.GetSunState(_sunSensorConfig, dt);

        return sunState;
    }

    public RoomLightEstimate GetRoomLightEstimate(double x)
    {
        var dt0 = new DateTime(2025, 4, 10, 0, 0, 0);
        var dt = dt0.AddHours(x);

        var sunState = SunSensor.GetSunState(_sunSensorConfig, dt);

        var roomLightEstimate = RoomLightEstimator.EstimateRoomLight(sunState, _roomLightConfig);

        return roomLightEstimate;
    }
}