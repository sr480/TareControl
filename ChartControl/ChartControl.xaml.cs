using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;

namespace ChartControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ChartControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ChartControl), new UIPropertyMetadata("Title"));
        public static readonly DependencyProperty YLableProperty =
            DependencyProperty.Register("YLable", typeof(string), typeof(ChartControl), new UIPropertyMetadata("Y Lable"));
        public static readonly DependencyProperty XLableProperty =
            DependencyProperty.Register("XLable", typeof(string), typeof(ChartControl), new UIPropertyMetadata("X Lable"));
        public static readonly DependencyProperty XMaximumProperty =
            DependencyProperty.Register("XMaximum", typeof(double), typeof(ChartControl), new UIPropertyMetadata(100.0));
        public static readonly DependencyProperty XMinimumProperty =
            DependencyProperty.Register("XMinimum", typeof(double), typeof(ChartControl), new UIPropertyMetadata(0.0));
        public static readonly DependencyProperty YMaximumProperty =
            DependencyProperty.Register("YMaximum", typeof(double), typeof(ChartControl), new UIPropertyMetadata(100.0));
        public static readonly DependencyProperty YMinimumProperty =
            DependencyProperty.Register("YMinimum", typeof(double), typeof(ChartControl), new UIPropertyMetadata(0.0));
        public static readonly DependencyProperty XGridStepProperty =
            DependencyProperty.Register("XGridStep", typeof(double), typeof(ChartControl), new UIPropertyMetadata(10.0));
        public static readonly DependencyProperty YGridStepProperty =
            DependencyProperty.Register("YGridStep", typeof(double), typeof(ChartControl), new UIPropertyMetadata(10.0));
        public static readonly DependencyProperty DataMemberProperty =
            DependencyProperty.Register("DataMember", typeof(string), typeof(ChartControl));
        public static readonly DependencyProperty ValueMembersProperty =
            DependencyProperty.Register("ValueMembers", typeof(ObservableCollection<ValueMemberDefinition>), typeof(ChartControl));
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(object), typeof(ChartControl));
        public static readonly DependencyProperty AutoCalculateAxisLimitsProperty =
            DependencyProperty.Register("AutoCalculateAxisLimits", typeof(bool), typeof(ChartControl), new UIPropertyMetadata(true));

        public bool AutoCalculateAxisLimits
        {
            get { return (bool)GetValue(AutoCalculateAxisLimitsProperty); }
            set { SetValue(AutoCalculateAxisLimitsProperty, value); }
        }
        public double XMaximum
        {
            get { return (double)GetValue(XMaximumProperty); }
            set { SetValue(XMaximumProperty, value); }
        }
        public double XMinimum
        {
            get { return (double)GetValue(XMinimumProperty); }
            set { SetValue(XMinimumProperty, value); }
        }
        public double YMaximum
        {
            get { return (double)GetValue(YMaximumProperty); }
            set { SetValue(YMaximumProperty, value); }
        }
        public double YMinimum
        {
            get { return (double)GetValue(YMinimumProperty); }
            set { SetValue(YMinimumProperty, value); }
        }
        public double XGridStep
        {
            get { return (double)GetValue(XGridStepProperty); }
            set { SetValue(XGridStepProperty, value); }
        }
        public double YGridStep
        {
            get { return (double)GetValue(YGridStepProperty); }
            set { SetValue(YGridStepProperty, value); }
        }
        public string YLable
        {
            get { return (string)GetValue(YLableProperty); }
            set { SetValue(YLableProperty, value); }
        }
        public string XLable
        {
            get { return (string)GetValue(XLableProperty); }
            set { SetValue(XLableProperty, value); }
        }
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public string DataMember
        {
            get { return (string)GetValue(DataMemberProperty); }
            set { SetValue(DataMemberProperty, value); }
        }
        public ObservableCollection<ValueMemberDefinition> ValueMembers
        {
            get { return (ObservableCollection<ValueMemberDefinition>)GetValue(ValueMembersProperty); }
            set { SetValue(ValueMembersProperty, value); }
        }
        public object DataSource
        {
            get { return GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }


        private List<RawValueInfo> rawData;

        public ChartControl()
        {
            ValueMembers = new ObservableCollection<ValueMemberDefinition>();
            InitializeComponent();
            plotGrid.SizeChanged += new SizeChangedEventHandler(plotGrid_SizeChanged);
            plot.SizeChanged += new SizeChangedEventHandler(plot_SizeChanged);

            xAxisValues.SizeChanged += new SizeChangedEventHandler(xAxisValues_SizeChanged);
            yAxisValues.SizeChanged += new SizeChangedEventHandler(yAxisValues_SizeChanged);
        }

        private void ReadRawData()
        {
            rawData = new List<RawValueInfo>();
            if (DataSource == null)
                return;
            foreach (var item in (IEnumerable)DataSource)
            {
                try
                {
                    double data = Convert.ToDouble(item.GetType().GetProperty(DataMember).GetValue(item, null));
                    foreach (var valMember in ValueMembers)
                    {
                        try
                        {
                            double value = Convert.ToDouble(item.GetType().GetProperty(valMember.Member).GetValue(item, null));
                            rawData.Add(new RawValueInfo(data, value, valMember));
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == DataSourceProperty)
            {
                if (e.OldValue is INotifyCollectionChanged)
                    (e.OldValue as INotifyCollectionChanged).CollectionChanged -= DataSourceChanged;
                if (e.NewValue is INotifyCollectionChanged)
                    (e.NewValue as INotifyCollectionChanged).CollectionChanged += DataSourceChanged;
                ReadData();
            }
        }

        void DataSourceChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ReadData();
        }
        private void ReadData()
        {
            ReadRawData();
            if (AutoCalculateAxisLimits)
                CalculateAxisLimits();
            if (IsVisible)
                RedrawAll();
        }
        public void CalculateAxisLimits()
        {
            if (rawData == null | rawData.Count == 0) return;

            double maxValue = rawData.Max(r => r.Value);
            double minValue = rawData.Min(r => r.Value);
            double maxData = rawData.Max(r => r.Data);
            double minData = rawData.Min(r => r.Data);

            double xLength = Math.Abs(maxData - minData);
            double yLength = Math.Abs(maxValue - minValue);
            XMinimum = minData;
            XMaximum = maxData;
            YMinimum = minValue;
            YMaximum = maxValue;

            XGridStep = xLength / 10;
            YGridStep = yLength / 10;
        }
        public void RedrawAll()
        {
            RedrawPlot(plot.ActualWidth, plot.ActualHeight);
            RedrawYAxis(yAxisValues.ActualWidth);
            RedrawXAxis(xAxisValues.ActualWidth);
            RedrawGrid(plotGrid.ActualWidth, plotGrid.ActualHeight);
        }

        //Redraw plot
        private void RedrawPlot(double width, double height)
        {
            plot.Children.Clear();
            if (rawData == null || rawData.Count == 0)
                return;

            double yTransform = height / (YMaximum - YMinimum);
            double xTransform = width / (XMaximum - XMinimum);

            var groupedData = rawData.GroupBy(g => g.ValueMember);
            foreach (var grp in groupedData)
            {
                var sortedData = grp.OrderBy(o => o.Data).ToList();
                for (int i = 1; i < sortedData.Count(); i++)
                {
                    plot.Children.Add(new Line()
                        {
                            Stroke = grp.Key.Color,
                            X1 = xTransform * (sortedData[i - 1].Data - XMinimum),
                            Y1 = height - yTransform * (sortedData[i - 1].Value - YMinimum),
                            X2 = xTransform * (sortedData[i].Data - XMinimum),
                            Y2 = height - yTransform * (sortedData[i].Value - YMinimum)
                        });
                }
            }
        }
        void plot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawPlot(e.NewSize.Width, e.NewSize.Height);
        }

        //Redraw vertical axis
        private void RedrawYAxis(double width)
        {
            yAxisValues.Children.Clear();
            double yTransform = width / (YMaximum - YMinimum);
            int hCount = (int)((YMaximum - YMinimum) / YGridStep);
            for (int h = 0; h <= hCount; h++)
            {
                yAxisValues.Children.Add(new TextBlock()
                {
                    Text = (YMinimum + YGridStep * h).ToString("F1"),
                    Margin = new Thickness(h * YGridStep * yTransform, 1, 0, 1)
                });
            }

            //Remove overlapping values
            for (int i = 0; i < yAxisValues.Children.Count; i++)
            {
                for (int j = i + 1; j < yAxisValues.Children.Count; )
                {
                    var iCh = (TextBlock)yAxisValues.Children[i];
                    var jCh = (TextBlock)yAxisValues.Children[j];
                    iCh.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    if (iCh.DesiredSize.Width > jCh.Margin.Left)
                        yAxisValues.Children.RemoveAt(j);
                    else
                        break;
                }
            }
        }
        void yAxisValues_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawYAxis(e.NewSize.Width);
        }

        //Redraw horisontal axis
        private void RedrawXAxis(double width)
        {
            xAxisValues.Children.Clear();
            double xTransform = width / (XMaximum - XMinimum);
            int vCount = (int)((XMaximum - XMinimum) / XGridStep);
            for (int v = 0; v <= vCount; v++)
            {
                xAxisValues.Children.Add(new TextBlock()
                {
                    Text = (XMinimum + XGridStep * v).ToString("F1"),
                    Margin = new Thickness(v * XGridStep * xTransform, 1, 0, 1)
                });
            }

            //Remove overlapping values
            for (int i = 0; i < xAxisValues.Children.Count; i++)
            {
                for (int j = i + 1; j < xAxisValues.Children.Count; )
                {
                    var iCh = (TextBlock)xAxisValues.Children[i];
                    var jCh = (TextBlock)xAxisValues.Children[j];
                    iCh.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    if (iCh.DesiredSize.Width > jCh.Margin.Left)
                        xAxisValues.Children.RemoveAt(j);
                    else
                        break;
                }
            }
        }
        void xAxisValues_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawXAxis(e.NewSize.Width);
        }

        //Redraw grid
        private void RedrawGrid(double width, double height)
        {
            plotGrid.Children.Clear();

            var gridStroke = Brushes.LightGray;
            var axisStroke = Brushes.Black;

            double xTransform = width / (XMaximum - XMinimum);
            double yTransform = height / (YMaximum - YMinimum);
            //Draw vertical grid
            int vCount = (int)((XMaximum - XMinimum) / XGridStep);
            for (int v = 1; v <= vCount; v++)
            {
                var x = v * XGridStep * xTransform;
                plotGrid.Children.Add(new Line() { X1 = x, Y1 = 0, X2 = x, Y2 = height, Stroke = gridStroke });
            }
            //Draw horisontal grid
            int hCount = (int)((YMaximum - YMinimum) / YGridStep);
            for (int h = 0; h < hCount; h++)
            {
                var y = h * YGridStep * yTransform;
                plotGrid.Children.Add(new Line() { X1 = 0, Y1 = y, X2 = width, Y2 = y, Stroke = gridStroke });
            }
            //Horisontal Axis arrow
            plotGrid.Children.Add(new Line() { X1 = 0, Y1 = height, X2 = width, Y2 = height, Stroke = axisStroke });
            plotGrid.Children.Add(new Line() { X1 = width - 9, Y1 = height - 3, X2 = width, Y2 = height, Stroke = axisStroke });
            plotGrid.Children.Add(new Line() { X1 = width - 9, Y1 = height + 3, X2 = width, Y2 = height, Stroke = axisStroke });
            //Vertical Axis arrow
            plotGrid.Children.Add(new Line() { X1 = 0, Y1 = height, X2 = 0, Y2 = 0, Stroke = axisStroke });
            plotGrid.Children.Add(new Line() { X1 = -3, Y1 = 9, X2 = 0, Y2 = 0, Stroke = axisStroke });
            plotGrid.Children.Add(new Line() { X1 = +3, Y1 = 9, X2 = 0, Y2 = 0, Stroke = axisStroke });
        }
        void plotGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawGrid(e.NewSize.Width, e.NewSize.Height);
        }

        private void plotTarget_MouseMove(object sender, MouseEventArgs e)
        {
            plotTarget.Children.Clear();
            values.Children.Clear();

            var axisStroke = Brushes.Gray;

            var x = e.GetPosition(plotTarget).X;
            var y = e.GetPosition(plotTarget).Y;
            //Draw vertical line
            plotTarget.Children.Add(new Line() { X1 = x, Y1 = 0, X2 = x, Y2 = plotTarget.ActualHeight, Stroke = axisStroke });

            //Draw horisontal line
            //plotTarget.Children.Add(new Line() { X1 = 0, Y1 = y, X2 = plotTarget.ActualWidth, Y2 = y, Stroke = axisStroke });

            var xVal = (XMaximum - XMinimum) / plot.ActualWidth * x + XMinimum;

            for (int i = 0; i < ValueMembers.Count; i++)
            {
                double? valueY = GetValueY(ValueMembers[i], xVal);
                if (valueY != null)
                {
                    TextBlock textBlock = new TextBlock()
                    {
                        Margin = new Thickness(2, 2, 2, 2), //new Thickness(x + 2, y - 16 * (ValueMembers.Count - i + 1), 0, 0),
                        Text = valueY.Value.ToString("F"),
                        Foreground = ValueMembers[i].Color,
                        FontWeight = FontWeights.Bold
                    };
                    values.Children.Add(textBlock);
                }
            }
            values.Children.Add(new TextBlock()
            {
                Margin = new Thickness(2, 2, 2, 2), //new Thickness(x + 2, y - 16, 0, 0),
                Text = xVal.ToString("F"),
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            });
        }

        double? GetValueY(ValueMemberDefinition vm, double x)
        {
            if (rawData == null)
                return null;
            var leftVal = rawData.Where(v => v.ValueMember == vm & v.Data >= x).OrderBy(v => v.Data).FirstOrDefault();
            var rightVal = rawData.Where(v => v.ValueMember == vm & v.Data <= x).OrderByDescending(v => v.Data).FirstOrDefault();
            if (leftVal == null | rightVal == null)
                return null;
            return (leftVal.Value - rightVal.Value) / (leftVal.Data - rightVal.Data) * (x - leftVal.Data) + leftVal.Value;
        }
    }
}
