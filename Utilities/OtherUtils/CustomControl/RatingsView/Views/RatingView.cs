using Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;
using Colors = Microsoft.Maui.Graphics.Colors;

namespace Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Views;


public class RatingControl : BaseTemplateView<Grid>
{
    #region Private Properties

    private Microsoft.Maui.Controls.Shapes.Path[] shapes;

    private string shape;

    private PathFigureCollection converted;

    private int touchedTime = 0;

    #endregion

    #region Bindable Properties
    //Rating value bindable property
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(double),
            typeof(RatingControl),
            defaultValue: 0.0,
            propertyChanged: OnBindablePropertyChanged);


    //Maximum rating value Bindable property
    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(
            nameof(Maximum),
            typeof(int),
            typeof(RatingControl),
            defaultValue: 5,
            propertyChanged: OnBindablePropertyChanged);


    //Star size Bindable property
    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(
            nameof(Size),
            typeof(double),
            typeof(RatingControl),
            defaultValue: 20.0,
            propertyChanged: OnBindablePropertyChanged);


    //Star Color Bindable property
    public static readonly BindableProperty FillProperty =
        BindableProperty.Create(
            nameof(Fill),
            typeof(Color),
            typeof(RatingControl),
            defaultValue: Colors.DarkSlateBlue,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty EmptyColorProperty =
        BindableProperty.Create(
            nameof(EmptyColor),
            typeof(Color),
            typeof(RatingControl),
            defaultValue: Colors.Transparent,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(
            nameof(StrokeColor),
            typeof(Color),
            typeof(RatingControl),
            defaultValue: Colors.Blue,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty StrokeThicknessProperty =
        BindableProperty.Create(
            nameof(StrokeThickness),
            typeof(double),
            typeof(RatingControl),
            defaultValue: 0.0,
            propertyChanged: OnBindablePropertyChanged);

    //Star Spacing Between Bindable Property
    public static readonly BindableProperty SpacingProperty =
        BindableProperty.Create(
            nameof(Spacing),
            typeof(int),
            typeof(RatingControl),
            defaultValue: 5,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty AllowRatingProperty =
        BindableProperty.Create(
            nameof(AllowRating),
            typeof(bool),
            typeof(RatingControl),
            defaultValue: true,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(RatingControl),
            defaultValue: null,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(RatingControl),
            defaultValue: null,
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty BindControlProperty =
        BindableProperty.Create(
            nameof(BindControl),
            typeof(object),
            typeof(RatingControl),
            propertyChanged: OnBindablePropertyChanged);

    public static readonly BindableProperty AnimateProperty =
        BindableProperty.Create(
            nameof(Animate),
            typeof(bool),
            typeof(RatingControl),
            defaultValue: true,
            propertyChanged: OnBindablePropertyChanged);

    public readonly BindableProperty ShapeProperty =
       BindableProperty.Create(
           nameof(Shape),
           typeof(RatingShapes),
           typeof(RatingControl),
           
           propertyChanged: OnShapePropertyChanged);

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

    public Color Fill
    {
        get => (Color)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
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

    public int Spacing
    {
        get => (int)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool AllowRating
    {
        get => (bool)GetValue(AllowRatingProperty);
        set => SetValue(AllowRatingProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandProperty, value);
    }

    public object BindControl
    {
        get => GetValue(BindControlProperty);
        set => SetValue(BindControlProperty, value);
    }

    public bool Animate
    {
        get => (bool)GetValue(AnimateProperty);
        set => SetValue(AnimateProperty, value);
    }

    public RatingShapes Shape
    {
        get => (RatingShapes)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }
    #endregion

    public RatingControl()
    {
        shapes = new Microsoft.Maui.Controls.Shapes.Path[Maximum];

        HorizontalOptions = LayoutOptions.Center;

        this.Control.ColumnSpacing = Spacing;

        InitializeShape();

        DrawBase();
    }

    #region Events

    private static void OnBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        //re-draw forms.
        ((RatingControl)bindable).ReDraw();
    }

    private static void OnShapePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((RatingControl)bindable).InitializeShape();
        ((RatingControl)bindable).ReDraw();
    }

    int GetHoverValue(object sender, EventArgs e)
    {
        var tappedShape = sender as Microsoft.Maui.Controls.Shapes.Path;

        if (tappedShape is null)
            return 99;
        var value = Control!.GetColumn(tappedShape);
        Value = value;
        return value;
        
    }
    private void OnShapeTapped(object sender, TappedEventArgs e)
    {
        var tappedShape = sender as Microsoft.Maui.Controls.Shapes.Path;

        if (tappedShape is null)
            return;
        
        var columnIndex = Control.GetColumn(tappedShape);


        if (Maximum > 1)
        {
            Value = columnIndex + 1;
        }
        else if (Maximum is 1 or 0)
        {
            if (BindControl is null)
            {
                Value = Value is 1 ? 0 : 1;
            }
            else if (BindControl is RatingControl)
            {
                touchedTime++;
                if (touchedTime >= 1)
                {
                    Value = Value == 1 ? 0 : 1;
                    ((RatingControl)BindControl).Value = 0;
                    touchedTime = 0;
                }
                else
                {
                    Value = Value == 1 ? 0 : 1;
                    ((RatingControl)BindControl).Value = Value == 1 ? 0 : 1;
                }
            }
        }

        var data = new Rating
        {
            Value = Value,
            Parameter = CommandParameter
        };

        if (Command is not null && Command.CanExecute(data))
        {
            Command.Execute(data);
        }
    }

    #endregion

    #region Methods
    private void DrawBase()
    {
        for (int i = 0; i < Maximum; i++)
        {

            Control.ColumnDefinitions.Add(new ColumnDefinition { Width = Size });
            Microsoft.Maui.Controls.Shapes.Path image = new();
            if (i <= Value)
            {
                image.Data = new PathGeometry(converted);

                image.Fill = Fill;
                image.Stroke = Fill;
                image.Aspect = Stretch.Uniform;
                image.HeightRequest = Size;
                image.WidthRequest = Size;

            }
            else if (i > Value)
            {
                image.Data = new PathGeometry(converted);

                image.Fill = Colors.White;
                ;
                image.Stroke = Colors.Yellow;
                ;

                image.StrokeLineJoin = PenLineJoin.Round;
                image.StrokeThickness = StrokeThickness;

                image.Aspect = Stretch.Uniform;
                image.HeightRequest = Size;
                image.WidthRequest = Size;
            }


            if (AllowRating)
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += OnShapeTapped;

                var pointerGestureRecognizer = new PointerGestureRecognizer();
                pointerGestureRecognizer.PointerEntered += PointerGestureRecognizer_PointerEntered;
                pointerGestureRecognizer.PointerExited += PointerGestureRecognizer_PointerExited;
                image.GestureRecognizers.Add(tapGestureRecognizer);
                image.GestureRecognizers.Add(pointerGestureRecognizer);
            }

            Control.Children.Add(image);
            this.Control.SetColumn(image, i);

            if (Animate)
                shapes[i] = ApplyCustomStyle(image);
            else
                shapes[i] = image;

        }

        UpdateDraw();
    }

    private async void PointerGestureRecognizer_PointerExited(object? sender, PointerEventArgs e)
    {

        var tappedShape = sender as Microsoft.Maui.Controls.Shapes.Path;

        if (tappedShape is null)
            return;
        
        await tappedShape.AnimateFocusModePointerExited(endScale:1, endOpacity:1);
    }

    private async void PointerGestureRecognizer_PointerEntered(object? sender, PointerEventArgs e)
    {

        var tappedShape = sender as Microsoft.Maui.Controls.Shapes.Path;

        if (tappedShape is null)
            return;
        
        await tappedShape.AnimateFocusModePointerEnter(endScale:1.2);
        GetHoverValue(sender, e);
        //UpdateDraw();
        
    }

    private void ReDraw()
    {
        Control.Children.Clear();

        Control.ColumnDefinitions.Clear();

        shapes = new Microsoft.Maui.Controls.Shapes.Path[Maximum];

        for (int i = 0; i < Maximum; i++)
        {
            Control.ColumnDefinitions.Add(new ColumnDefinition { Width = Size });

            Microsoft.Maui.Controls.Shapes.Path image = new();
            if (i <= Value)
            {
                var c = PathConverter.ConvertStringPathToGeo(shape);
                image.Data = new PathGeometry((PathFigureCollection)c);

                image.Fill = Colors.Transparent;
                image.Stroke = Colors.Turquoise;
                image.Aspect = Stretch.Uniform;
                image.HeightRequest = Size;
                image.WidthRequest = Size;

            }
            else if (i > Value)
            {
                var c = PathConverter.ConvertStringPathToGeo(shape);
                image.Data = new PathGeometry((PathFigureCollection)c);

                image.Fill = Colors.Green;
                image.Stroke = Colors.Green;


                image.StrokeLineJoin = PenLineJoin.Round;
                image.StrokeLineCap = PenLineCap.Round;
                image.StrokeThickness = StrokeThickness;

                image.Aspect = Stretch.Uniform;
                image.HeightRequest = Size;
                image.WidthRequest = Size;
            }


            if (AllowRating)
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += OnShapeTapped;
                var pointerGestureRecognizer = new PointerGestureRecognizer();
                pointerGestureRecognizer.PointerEntered += PointerGestureRecognizer_PointerEntered;
                pointerGestureRecognizer.PointerExited += PointerGestureRecognizer_PointerExited;
                image.GestureRecognizers.Add(tapGestureRecognizer);
                image.GestureRecognizers.Add(pointerGestureRecognizer);
                
            }

            Control.Children.Add(image);
            this.Control.SetColumn(image, i);

            if (Animate)
                shapes[i] = ApplyCustomStyle(image);
            else
                shapes[i] = image;
        }

        UpdateDraw();
    }

    private void UpdateDraw(double tempVal = 9)
    {
        for (int i = 0; i < Maximum; i++)
        {
            var image = shapes[i];

            if (Value > Maximum)
                return;
            if (Value >= i + 1)
            {
                image.HeightRequest = Size;
                image.WidthRequest = Size;
                image.StrokeLineJoin = PenLineJoin.Round;
                image.StrokeThickness = 1;
                image.Stroke = Colors.DarkSlateBlue;
                image.Fill = Fill;
            }
            else
            {
                if (Value % 1 == 0)
                {
                    
                    image.Fill = Colors.Transparent;
                    //image.Fill = Colors.Green;
                    image.BackgroundColor = Colors.Transparent;
                    image.Stroke = Colors.DarkSlateBlue;
                    image.StrokeThickness = 3;
                    image.StrokeLineJoin = PenLineJoin.Round;
                }
                else
                {
                    
                    var fraction = Value - Math.Floor(Value);
                    var element = shapes[(int)(Value - fraction)];
                    if (element != null)
                    {
                        var colors = new GradientStopCollection
                        {
                            new GradientStop(Colors.Green, (float)fraction),
                            new GradientStop(EmptyColor, (float)fraction)
                        };

                        element.Fill = new LinearGradientBrush(colors, new Point(0, 0), new Point(1, 0));
                        element.StrokeThickness = 1;
                        element.StrokeLineJoin = PenLineJoin.Round;
                        element.Stroke = Colors.Pink;
                    }
                }
            }
        }
    }

    private PathFigureCollection InitializeShape()
    {
        switch (Shape)
        {
            case RatingShapes.Star:
                shape = PathShapes.Star;
                return (PathConverter.ConvertStringPathToGeo(PathShapes.Star) as PathFigureCollection)!;
            case RatingShapes.Heart:
                shape = PathShapes.Heart;
                return (PathConverter.ConvertStringPathToGeo(PathShapes.Heart) as PathFigureCollection)!;
            case RatingShapes.Like:
                shape = PathShapes.Like;
                return (PathConverter.ConvertStringPathToGeo(PathShapes.Like) as PathFigureCollection)!;
            case RatingShapes.Dislike:
                shape = PathShapes.Dislike;
                return (PathConverter.ConvertStringPathToGeo(PathShapes.Dislike) as PathFigureCollection)!;
            default:
                shape = PathShapes.Star;
                return (PathConverter.ConvertStringPathToGeo(PathShapes.Star) as PathFigureCollection)!;
        }

    }

    private Microsoft.Maui.Controls.Shapes.Path ApplyCustomStyle(Microsoft.Maui.Controls.Shapes.Path image)
    {
        Style imageStyle = new(typeof(Microsoft.Maui.Controls.Shapes.Path));

        VisualStateGroup commonStatesGroup = new() { Name = "CommonStates" };

        VisualState normalState = new() { Name = "Normal" };

        VisualState pointerOverState = new() { Name = "PointerOver" };
        Setter pointerOverSetter = new()
        {
            Property = Microsoft.Maui.Controls.Shapes.Path.ScaleProperty,
            Value = 1.01,
        };

        var pointerOverSetter2 = new Setter()
        {
            Property = Microsoft.Maui.Controls.Shapes.Path.FillProperty,
            Value = Colors.DarkRed
        };
        pointerOverState.Setters.Add(pointerOverSetter);
        pointerOverState.Setters.Add(pointerOverSetter2);
        VisualState clickedState = new() { Name = "Touched" };
        Setter scaleState = new()
        {
            Property = Microsoft.Maui.Controls.Shapes.Path.ScaleProperty,
            Value = 0.5
        };
        Setter colorState = new()
        {
            Property = Microsoft.Maui.Controls.Shapes.Path.ScaleProperty,
            Value = 0.5
        };
        clickedState.Setters.Add(scaleState);
        clickedState.Setters.Add(colorState);
        commonStatesGroup.States.Add(normalState);
        commonStatesGroup.States.Add(clickedState);
        commonStatesGroup.States.Add(pointerOverState);

        VisualStateManager.GetVisualStateGroups(image).Add(commonStatesGroup);

        image.Style = imageStyle;

        return image;
    }

    #endregion

    protected override void OnControlInitialized(Grid control)
    {
        shapes = new Microsoft.Maui.Controls.Shapes.Path[Maximum];

        converted = InitializeShape();

        HorizontalOptions = LayoutOptions.Center;

        this.Control.ColumnSpacing = Spacing;

        DrawBase();
    }
}

public abstract class BaseTemplateView<TControl> : TemplatedView where TControl : View, new()
{
    protected TControl? Control { get; private set; }

    public BaseTemplateView()
        => ControlTemplate = new ControlTemplate(typeof(TControl));

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (Control != null)
            Control.BindingContext = BindingContext;
    }

    protected override void OnChildAdded(Element child)
    {
        if (Control is null && child is TControl control)
        {
            Control = control;
            OnControlInitialized(Control);
        }

        base.OnChildAdded(child);
    }

    protected abstract void OnControlInitialized(TControl control);
}


internal class PathConverter
{
    const bool AllowSign = true;
    const bool AllowComma = true;

    static bool _figureStarted;
    static string _pathString;
    static int _pathLength;
    static int _curIndex;
    static Point _lastStart;
    static Point _lastPoint;
    static Point _secondLastPoint;
    static char _token;

    public static object ConvertStringPathToGeo(string value)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(value, "value");

        PathFigureCollection pathFigureCollection = new PathFigureCollection();

        ParseStringToPathFigureCollection(pathFigureCollection, value);

        return pathFigureCollection;
    }

    static void ParseStringToPathFigureCollection(PathFigureCollection pathFigureCollection, string pathString)
    {
        if (pathString != null)
        {
            int curIndex = 0;

            while ((curIndex < pathString.Length) && char.IsWhiteSpace(pathString, curIndex))
            {
                curIndex++;
            }

            if (curIndex < pathString.Length)
            {
                if (pathString[curIndex] == 'F')
                {
                    curIndex++;

                    while ((curIndex < pathString.Length) && char.IsWhiteSpace(pathString, curIndex))
                    {
                        curIndex++;
                    }

                    // If we ran out of text, this is an error, because 'F' cannot be specified without 0 or 1
                    // Also, if the next token isn't 0 or 1, this too is illegal
                    if ((curIndex == pathString.Length) ||
                        ((pathString[curIndex] != '0') &&
                         (pathString[curIndex] != '1')))
                    {
                        throw new FormatException("IllegalToken");
                    }

                    // Increment curIndex to point to the next char
                    curIndex++;
                }
            }

            ParseToPathFigureCollection(pathFigureCollection, pathString, curIndex);
        }
    }

    static void ParseToPathFigureCollection(PathFigureCollection pathFigureCollection, string pathString, int startIndex)
    {
        PathFigure pathFigure = null;

        _pathString = pathString;
        _pathLength = pathString.Length;
        _curIndex = startIndex;

        _secondLastPoint = new Point(0, 0);
        _lastPoint = new Point(0, 0);
        _lastStart = new Point(0, 0);

        _figureStarted = false;

        bool first = true;

        char last_cmd = ' ';

        while (ReadToken()) // Empty path is allowed in XAML
        {
            char cmd = _token;

            if (first)
            {
                if ((cmd != 'M') && (cmd != 'm'))  // Path starts with M|m
                {
                    ThrowBadToken();
                }

                first = false;
            }

            switch (cmd)
            {
                case 'm':
                case 'M':
                    // XAML allows multiple points after M/m
                    _lastPoint = ReadPoint(cmd, !AllowComma);

                    pathFigure = new PathFigure
                    {
                        StartPoint = _lastPoint
                    };
                    pathFigureCollection.Add(pathFigure);

                    _figureStarted = true;
                    _lastStart = _lastPoint;
                    last_cmd = 'M';

                    while (IsNumber(AllowComma))
                    {
                        _lastPoint = ReadPoint(cmd, !AllowComma);

                        LineSegment lineSegment = new LineSegment
                        {
                            Point = _lastPoint
                        };
                        pathFigure.Segments.Add(lineSegment);

                        last_cmd = 'L';
                    }
                    break;

                case 'l':
                case 'L':
                case 'h':
                case 'H':
                case 'v':
                case 'V':
                    EnsureFigure();

                    do
                    {
                        switch (cmd)
                        {
                            case 'l':
                                _lastPoint = ReadPoint(cmd, !AllowComma);
                                break;
                            case 'L':
                                _lastPoint = ReadPoint(cmd, !AllowComma);
                                break;
                            case 'h':
                                _lastPoint.X += ReadNumber(!AllowComma);
                                break;
                            case 'H':
                                _lastPoint.X = ReadNumber(!AllowComma);
                                break;
                            case 'v':
                                _lastPoint.Y += ReadNumber(!AllowComma);
                                break;
                            case 'V':
                                _lastPoint.Y = ReadNumber(!AllowComma);
                                break;
                        }

                        pathFigure.Segments.Add(new LineSegment
                        {
                            Point = _lastPoint
                        });
                    }
                    while (IsNumber(AllowComma));

                    last_cmd = 'L';
                    break;

                case 'c':
                case 'C': // Cubic Bezier
                case 's':
                case 'S': // Smooth cublic Bezier
                    EnsureFigure();

                    do
                    {
                        Point p;

                        if ((cmd == 's') || (cmd == 'S'))
                        {
                            if (last_cmd == 'C')
                            {
                                p = Reflect();
                            }
                            else
                            {
                                p = _lastPoint;
                            }

                            _secondLastPoint = ReadPoint(cmd, !AllowComma);
                        }
                        else
                        {
                            p = ReadPoint(cmd, !AllowComma);

                            _secondLastPoint = ReadPoint(cmd, AllowComma);
                        }

                        _lastPoint = ReadPoint(cmd, AllowComma);

                        BezierSegment bezierSegment = new BezierSegment
                        {
                            Point1 = p,
                            Point2 = _secondLastPoint,
                            Point3 = _lastPoint
                        };

                        pathFigure.Segments.Add(bezierSegment);

                        last_cmd = 'C';
                    }
                    while (IsNumber(AllowComma));

                    break;

                case 'q':
                case 'Q': // Quadratic Bezier
                case 't':
                case 'T': // Smooth quadratic Bezier
                    EnsureFigure();

                    do
                    {
                        if ((cmd == 't') || (cmd == 'T'))
                        {
                            if (last_cmd == 'Q')
                            {
                                _secondLastPoint = Reflect();
                            }
                            else
                            {
                                _secondLastPoint = _lastPoint;
                            }

                            _lastPoint = ReadPoint(cmd, !AllowComma);
                        }
                        else
                        {
                            _secondLastPoint = ReadPoint(cmd, !AllowComma);
                            _lastPoint = ReadPoint(cmd, AllowComma);
                        }

                        QuadraticBezierSegment quadraticBezierSegment = new QuadraticBezierSegment
                        {
                            Point1 = _secondLastPoint,
                            Point2 = _lastPoint
                        };

                        pathFigure.Segments.Add(quadraticBezierSegment);

                        last_cmd = 'Q';
                    }
                    while (IsNumber(AllowComma));

                    break;

                case 'a':
                case 'A':
                    EnsureFigure();

                    do
                    {
                        // A 3,4 5, 0, 0, 6,7
                        double w = ReadNumber(!AllowComma);
                        double h = ReadNumber(AllowComma);
                        double rotation = ReadNumber(AllowComma);
                        bool large = ReadBool();
                        bool sweep = ReadBool();

                        _lastPoint = ReadPoint(cmd, AllowComma);

                        ArcSegment arcSegment = new ArcSegment
                        {
                            Size = new Size(w, h),
                            RotationAngle = rotation,
                            IsLargeArc = large,
                            SweepDirection = sweep ? SweepDirection.Clockwise : SweepDirection.CounterClockwise,
                            Point = _lastPoint
                        };

                        pathFigure.Segments.Add(arcSegment);
                    }
                    while (IsNumber(AllowComma));

                    last_cmd = 'A';
                    break;

                case 'z':
                case 'Z':
                    EnsureFigure();
                    pathFigure.IsClosed = true;
                    _figureStarted = false;
                    last_cmd = 'Z';

                    _lastPoint = _lastStart; // Set reference point to be first point of current figure
                    break;

                default:
                    ThrowBadToken();
                    break;
            }
        }
    }

    static void EnsureFigure()
    {
        if (!_figureStarted)
            _figureStarted = true;
    }

    static Point Reflect()
    {
        return new Point(
            2 * _lastPoint.X - _secondLastPoint.X,
            2 * _lastPoint.Y - _secondLastPoint.Y);
    }

    static bool More()
    {
        return _curIndex < _pathLength;
    }

    static bool SkipWhiteSpace(bool allowComma)
    {
        bool commaMet = false;

        while (More())
        {
            char ch = _pathString[_curIndex];

            switch (ch)
            {
                case ' ':
                case '\n':
                case '\r':
                case '\t':
                    break;

                case ',':
                    if (allowComma)
                    {
                        commaMet = true;
                        allowComma = false; // One comma only
                    }
                    else
                    {
                        ThrowBadToken();
                    }
                    break;

                default:
                    // Avoid calling IsWhiteSpace for ch in (' ' .. 'z']
                    if (((ch > ' ') && (ch <= 'z')) || !char.IsWhiteSpace(ch))
                    {
                        return commaMet;
                    }
                    break;
            }

            _curIndex++;
        }

        return commaMet;
    }

    static bool ReadBool()
    {
        SkipWhiteSpace(AllowComma);

        if (More())
        {
            _token = _pathString[_curIndex++];

            if (_token == '0')
            {
                return false;
            }
            else if (_token == '1')
            {
                return true;
            }
        }

        ThrowBadToken();

        return false;
    }

    static bool ReadToken()
    {
        SkipWhiteSpace(!AllowComma);

        // Check for end of string
        if (More())
        {
            _token = _pathString[_curIndex++];

            return true;
        }
        else
        {
            return false;
        }
    }

    static void ThrowBadToken()
    {
        throw new FormatException(string.Format("UnexpectedToken \"{0}\" into {1}", _pathString, _curIndex - 1));
    }

    static Point ReadPoint(char cmd, bool allowcomma)
    {
        double x = ReadNumber(allowcomma);
        double y = ReadNumber(AllowComma);

        if (cmd >= 'a') // 'A' < 'a'. lower case for relative
        {
            x += _lastPoint.X;
            y += _lastPoint.Y;
        }

        return new Point(x, y);
    }

    static bool IsNumber(bool allowComma)
    {
        bool commaMet = SkipWhiteSpace(allowComma);

        if (More())
        {
            _token = _pathString[_curIndex];

            // Valid start of a number
            if ((_token == '.') || (_token == '-') || (_token == '+') || ((_token >= '0') && (_token <= '9'))
                || (_token == 'I')  // Infinity
                || (_token == 'N')) // NaN
            {
                return true;
            }
        }

        if (commaMet) // Only allowed between numbers
        {
            ThrowBadToken();
        }

        return false;
    }

    static double ReadNumber(bool allowComma)
    {
        if (!IsNumber(allowComma))
        {
            ThrowBadToken();
        }

        bool simple = true;
        int start = _curIndex;

        // Allow for a sign
        // 
        // There are numbers that cannot be preceded with a sign, for instance, -NaN, but it's
        // fine to ignore that at this point, since the CLR parser will catch this later.
        if (More() && ((_pathString[_curIndex] == '-') || _pathString[_curIndex] == '+'))
        {
            _curIndex++;
        }

        // Check for Infinity (or -Infinity).
        if (More() && (_pathString[_curIndex] == 'I'))
        {
            // Don't bother reading the characters, as the CLR parser will
            // do this for us later.
            _curIndex = Math.Min(_curIndex + 8, _pathLength); // "Infinity" has 8 characters
            simple = false;
        }
        // Check for NaN
        else if (More() && (_pathString[_curIndex] == 'N'))
        {
            //
            // Don't bother reading the characters, as the CLR parser will
            // do this for us later.
            //
            _curIndex = Math.Min(_curIndex + 3, _pathLength); // "NaN" has 3 characters
            simple = false;
        }
        else
        {
            SkipDigits(!AllowSign);

            // Optional period, followed by more digits
            if (More() && (_pathString[_curIndex] == '.'))
            {
                simple = false;
                _curIndex++;
                SkipDigits(!AllowSign);
            }

            // Exponent
            if (More() && ((_pathString[_curIndex] == 'E') || (_pathString[_curIndex] == 'e')))
            {
                simple = false;
                _curIndex++;
                SkipDigits(AllowSign);
            }
        }

        if (simple && (_curIndex <= (start + 8))) // 32-bit integer
        {
            int sign = 1;

            if (_pathString[start] == '+')
            {
                start++;
            }
            else if (_pathString[start] == '-')
            {
                start++;
                sign = -1;
            }

            int value = 0;

            while (start < _curIndex)
            {
                value = value * 10 + (_pathString[start] - '0');
                start++;
            }

            return value * sign;
        }
        else
        {
            string subString = _pathString.Substring(start, _curIndex - start);

            try
            {
                return Convert.ToDouble(subString, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                throw new FormatException(string.Format("UnexpectedToken \"{0}\" into {1}", start, _pathString));
            }
        }
    }

    static void SkipDigits(bool signAllowed)
    {
        // Allow for a sign
        if (signAllowed && More() && ((_pathString[_curIndex] == '-') || _pathString[_curIndex] == '+'))
        {
            _curIndex++;
        }

        while (More() && (_pathString[_curIndex] >= '0') && (_pathString[_curIndex] <= '9'))
        {
            _curIndex++;
        }
    }
}