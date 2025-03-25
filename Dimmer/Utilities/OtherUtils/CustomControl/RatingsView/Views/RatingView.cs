using Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;
using static Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Views.ScaleToAnimation;
using LineSegment = Microsoft.Maui.Controls.Shapes.LineSegment;
using Path = Microsoft.Maui.Controls.Shapes.Path;


namespace Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Views;

public partial class RatingControl : ContentView
{
    #region Bindable Properties

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(double),
            typeof(RatingControl),
            0.0,
            BindingMode.TwoWay,
            propertyChanged: OnValueChanged);

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(
            nameof(Maximum),
            typeof(int),
            typeof(RatingControl),
            5,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(
            nameof(Size),
            typeof(double),
            typeof(RatingControl),
            24.0,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(RatingControl),
            Colors.Gold,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty EmptyColorProperty =
        BindableProperty.Create(
            nameof(EmptyColor),
            typeof(Color),
            typeof(RatingControl),
            Colors.Gray,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(
            nameof(StrokeColor),
            typeof(Color),
            typeof(RatingControl),
            Colors.Transparent,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty StrokeThicknessProperty =
        BindableProperty.Create(
            nameof(StrokeThickness),
            typeof(double),
            typeof(RatingControl),
            0.0,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty SpacingProperty =
        BindableProperty.Create(
            nameof(Spacing),
            typeof(double),
            typeof(RatingControl),
            4.0,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty AllowHalfStarsProperty =
        BindableProperty.Create(
            nameof(AllowHalfStars),
            typeof(bool),
            typeof(RatingControl),
            false,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty ShapeProperty =
        BindableProperty.Create(
            nameof(Shape),
            typeof(RatingShapes),
            typeof(RatingControl),
            RatingShape.Star,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty CustomShapeProperty =
        BindableProperty.Create(
            nameof(CustomShape),
            typeof(string),
            typeof(RatingControl),
            string.Empty,
            propertyChanged: OnRatingChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(RatingControl),
            null);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(RatingControl),
            null);

    #endregion

    #region Public Properties

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public Color EmptyColor
    {
        get => (Color)GetValue(EmptyColorProperty);
        set => SetValue(EmptyColorProperty, value);
    }

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool AllowHalfStars
    {
        get => (bool)GetValue(AllowHalfStarsProperty);
        set => SetValue(AllowHalfStarsProperty, value);
    }

    public RatingShape Shape
    {
        get => (RatingShape)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public string CustomShape
    {
        get => (string)GetValue(CustomShapeProperty);
        set => SetValue(CustomShapeProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    private readonly Grid _layout;

    public RatingControl()
    {
        _layout = new Grid
        {
            RowDefinitions = { new RowDefinition { Height = GridLength.Auto } },
            ColumnSpacing = Spacing,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        Content = _layout;
        UpdateRating();
    }

    #region Property Changed Handlers

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is RatingControl control)
        {
            control.UpdateRating();
            control.Command?.Execute(new Rating
            {
                Value = control.Value,
                Parameter = control.CommandParameter
            });
        }
    }

    private static void OnRatingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is RatingControl control)
        {
            control.UpdateRating();
        }
    }

    #endregion

    #region Rating Logic

    private void UpdateRating()
    {
        _layout.ColumnSpacing = Spacing;
        _layout.Children.Clear();
        _layout.ColumnDefinitions.Clear();

        for (int i = 0; i < Maximum; i++)
        {
            _layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Geometry shapePath = GetShapePath();

            Path shape = new Path
            {
                Data = shapePath,
                Fill = (i < Math.Floor(Value)) ? FillColor : EmptyColor,
                Stroke = StrokeColor,
                StrokeThickness = StrokeThickness,
                HeightRequest = Size,
                WidthRequest = Size,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            // Handle half-star ratings
            if (AllowHalfStars && i + 0.5 == Value)
            {
                shape.Clip = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, Size / 2, Size)
                };
                shape.Fill = FillColor;
            }

            // Add Tap Gesture
            TapGestureRecognizer tapGesture = new TapGestureRecognizer
            {
                Command = new Command(() => OnStarTapped(i + 1))
            };
            shape.GestureRecognizers.Add(tapGesture);

            // Add animations
            AddAnimations(shape);

            _layout.Children.Add(shape);
            Grid.SetColumn(shape, i);
        }
    }

    private void OnStarTapped(double tappedValue)
    {
        double newValue = tappedValue;

        // If half stars are allowed and the tap was on the left half, set to half
        // This requires more precise tap handling, which can be implemented as needed
        // For simplicity, we assume full stars here

        Value = newValue;

        Command?.Execute(new Rating
        {
            Value = Value,
            Parameter = CommandParameter
        });
    }

    private Geometry GetShapePath()
    {
        return Shape switch
        {
            RatingShape.Star => new PathGeometry
            {
                Figures =
                    [
                        new PathFigure
                        {
                            StartPoint = new Point(12, 0),
                            Segments =
                            [
                                new LineSegment { Point = new Point(15, 8) },
                                new LineSegment { Point = new Point(24, 9) },
                                new LineSegment { Point = new Point(17, 14) },
                                new LineSegment { Point = new Point(19, 23) },
                                new LineSegment { Point = new Point(12, 18) },
                                new LineSegment { Point = new Point(5, 23) },
                                new LineSegment { Point = new Point(7, 14) },
                                new LineSegment { Point = new Point(0, 9) },
                                new LineSegment { Point = new Point(9, 8) },
                                new LineSegment { Point = new Point(12, 0) }
                            ],
                            IsClosed = true
                        }
                    ]
            },
            RatingShape.Heart => new PathGeometry
            {
                Figures =
                    [
                        new PathFigure
                        {
                            StartPoint = new Point(12, 21),
                            Segments =
                            [
                                new BezierSegment
                                {
                                    Point1 = new Point(12, 21),
                                    Point2 = new Point(4, 13.5),
                                    Point3 = new Point(4, 8)
                                },
                                new BezierSegment
                                {
                                    Point1 = new Point(4, 3),
                                    Point2 = new Point(7.5, 0),
                                    Point3 = new Point(12, 5)
                                },
                                new BezierSegment
                                {
                                    Point1 = new Point(16.5, 0),
                                    Point2 = new Point(20, 3),
                                    Point3 = new Point(20, 8)
                                },
                                new BezierSegment
                                {
                                    Point1 = new Point(20, 13.5),
                                    Point2 = new Point(12, 21),
                                    Point3 = new Point(12, 21)
                                }
                            ],
                            IsClosed = true
                        }
                    ]
            },
            RatingShape.ThumbUp => new PathGeometry
            {
                Figures =
                    [
                        new PathFigure
                        {
                            StartPoint = new Point(2, 12),
                            Segments =
                            [
                                new LineSegment { Point = new Point(2, 22) },
                                new LineSegment { Point = new Point(10, 22) },
                                new LineSegment { Point = new Point(10, 14) },
                                new LineSegment { Point = new Point(18, 14) },
                                new LineSegment { Point = new Point(18, 12) },
                                new LineSegment { Point = new Point(10, 12) },
                                new LineSegment { Point = new Point(10, 4) },
                                new LineSegment { Point = new Point(2, 12) }
                            ],
                            IsClosed = true
                        }
                    ]
            },
            RatingShape.ThumbDown => new PathGeometry
            {
                Figures =
                    [
                        new PathFigure
                        {
                            StartPoint = new Point(2, 12),
                            Segments =
                            [
                                new LineSegment { Point = new Point(2, 2) },
                                new LineSegment { Point = new Point(10, 2) },
                                new LineSegment { Point = new Point(10, 10) },
                                new LineSegment { Point = new Point(18, 10) },
                                new LineSegment { Point = new Point(18, 12) },
                                new LineSegment { Point = new Point(10, 12) },
                                new LineSegment { Point = new Point(10, 20) },
                                new LineSegment { Point = new Point(2, 12) }
                            ],
                            IsClosed = true
                        }
                    ]
            },
            
            _ => GetDefaultStarPath(),
        };
    }

    private Geometry GetDefaultStarPath()
    {
        return new PathGeometry
        {
            Figures =
                [
                    new PathFigure
                    {
                        StartPoint = new Point(12, 0),
                        Segments =
                        [
                            new LineSegment { Point = new Point(15, 8) },
                            new LineSegment { Point = new Point(24, 9) },
                            new LineSegment { Point = new Point(17, 14) },
                            new LineSegment { Point = new Point(19, 23) },
                            new LineSegment { Point = new Point(12, 18) },
                            new LineSegment { Point = new Point(5, 23) },
                            new LineSegment { Point = new Point(7, 14) },
                            new LineSegment { Point = new Point(0, 9) },
                            new LineSegment { Point = new Point(9, 8) },
                            new LineSegment { Point = new Point(12, 0) }
                        ],
                        IsClosed = true
                    }
                ]
        };
    }

    private void AddAnimations(View view)
    {
        // Scale Animation on Tap
        ScaleToAnimation scaleUp = new ScaleToAnimation(view, 1.2, 100);
        ScaleToAnimation scaleDown = new ScaleToAnimation(view, 1.0, 100);

        view.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                await scaleUp.StartAsync();
                await scaleDown.StartAsync();
            })
        });
    }

    #endregion
}

// Helper class for scaling animations
public class ScaleToAnimation
{
    private readonly View _view;
    private readonly double _scale;
    private readonly uint _length;

    public ScaleToAnimation(View view, double scale, uint length)
    {
        _view = view;
        _scale = scale;
        _length = length;
    }

    public async Task StartAsync()
    {
        await _view.ScaleTo(_scale, _length, Easing.CubicInOut);
    }

    public enum RatingShape
    {
        Star,
        Heart,
        ThumbUp,
        ThumbDown,
        Custom
    }

    public class Rating
    {
        public double Value { get; set; }
        public object Parameter { get; set; }
    }
}