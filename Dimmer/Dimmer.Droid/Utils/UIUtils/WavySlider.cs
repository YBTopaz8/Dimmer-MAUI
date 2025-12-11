using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;

namespace Dimmer.Utils.UIUtils;


public class WavySlider : View
{
    private Paint _activePaint;
    private Paint _inactivePaint;
    private Paint _thumbPaint;
    private Path _wavePath;
    private float _value = 0f; // 0.0 to 1.0
    private float _amplitude = 15f; // Height of wave
    private float _frequency = 30f; // Width of one wave cycle
    private bool _isDragging = false;

    public event EventHandler<float> ValueChanged;
    public event EventHandler DragStarted;
    public event EventHandler DragCompleted;

    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0f, 1f);
            Invalidate();
        }
    }

    public WavySlider(Context context) : base(context) => Init();
    public WavySlider(Context context, IAttributeSet attrs) : base(context, attrs) => Init();

    private void Init()
    {
        _activePaint = new Paint { Color = Color.ParseColor("#FF6200EE"), StrokeWidth = 8f, AntiAlias = true };
        _activePaint.SetStyle(Paint.Style.Stroke);
        _inactivePaint = new Paint { Color = Color.ParseColor("#FFCCCCCC"), StrokeWidth = 8f, AntiAlias = true };
        _inactivePaint.SetStyle(Paint.Style.Stroke);
        _thumbPaint = new Paint { Color = Color.ParseColor("#FF6200EE"), AntiAlias = true, };
        _thumbPaint.SetStyle(Paint.Style.Fill);
        _wavePath = new Path();
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        float w = Width;
        float h = Height;
        float cy = h / 2f;
        float activeW = w * _value;

        // 1. Draw Inactive Line (Straight)
        canvas.DrawLine(activeW, cy, w, cy, _inactivePaint);

        // 2. Draw Active Wave (Snake)
        _wavePath.Reset();
        _wavePath.MoveTo(0, cy);

        for (float x = 0; x <= activeW; x += 5)
        {
            // Sine wave formula: y = Amplitude * sin(Frequency * x)
            // We dampen amplitude near 0 so it starts straight
            float y = cy + _amplitude * (float)Math.Sin((x / _frequency) * Math.PI * 2);
            _wavePath.LineTo(x, y);
        }
        canvas.DrawPath(_wavePath, _activePaint);

        // 3. Draw Thumb
        float thumbX = activeW;
        float thumbY = cy + _amplitude * (float)Math.Sin((thumbX / _frequency) * Math.PI * 2);
        canvas.DrawCircle(thumbX, thumbY, 20f, _thumbPaint);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        switch (e.Action)
        {
            case MotionEventActions.Down:
                _isDragging = true;
                DragStarted?.Invoke(this, EventArgs.Empty);
                UpdateValueFromTouch(e.GetX());
                return true;
            case MotionEventActions.Move:
                UpdateValueFromTouch(e.GetX());
                return true;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                _isDragging = false;
                DragCompleted?.Invoke(this, EventArgs.Empty);
                return true;
        }
        return base.OnTouchEvent(e);
    }

    private void UpdateValueFromTouch(float x)
    {
        Value = x / Width;
        ValueChanged?.Invoke(this, _value);
    }
}