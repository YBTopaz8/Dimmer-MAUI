using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using System.Diagnostics;

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class HoverCardControl : UserControl, IDisposable
{
    private readonly Compositor _compositor;
    private readonly Visual _rootVisual;
    private readonly DispatcherQueueTimer _hoverDelayTimer;
    private readonly AnimationOrchestrator _animationOrchestrator;
    private readonly HoverStateManager _stateManager;

    public HoverCardControl()
    {
        InitializeComponent();

        // Get composition objects
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        _rootVisual = ElementCompositionPreview.GetElementVisual(this);

        // Use DispatcherQueueTimer (runs on UI thread!)
        _hoverDelayTimer = DispatcherQueue.CreateTimer();
        _hoverDelayTimer.Interval = TimeSpan.FromMilliseconds(250);
        _hoverDelayTimer.Tick += OnHoverDelayTick;

        // State manager for thread-safe state handling
        _stateManager = new HoverStateManager();

        // Animation orchestrator for sequencing
        _animationOrchestrator = new AnimationOrchestrator(_compositor);
    }

    #region State Management (Thread-Safe)

    private class HoverStateManager
    {
        private readonly object _lock = new();
        private HoverState _currentState = HoverState.Idle;

        public enum HoverState { Idle, HoverPending, Expanding, Expanded, Collapsing }

        public bool TryTransitionTo(HoverState newState)
        {
            lock (_lock)
            {
                var oldState = _currentState;

                // Valid state transitions
                bool isValid = (oldState, newState) switch
                {
                    // Normal flow
                    (HoverState.Idle, HoverState.HoverPending) => true,
                    (HoverState.HoverPending, HoverState.Expanding) => true,
                    (HoverState.Expanding, HoverState.Expanded) => true,
                    (HoverState.Expanded, HoverState.Collapsing) => true,
                    (HoverState.Collapsing, HoverState.Idle) => true,

                    // Cancel hover before expansion
                    (HoverState.HoverPending, HoverState.Idle) => true,

                    // Immediate collapse from any state
                    (_, HoverState.Collapsing) => true,
                    (_, HoverState.Idle) when oldState == HoverState.Collapsing => true,

                    _ => false
                };

                if (isValid)
                {
                    _currentState = newState;
                    Debug.WriteLine($"State: {oldState} -> {newState}");
                    return true;
                }

                Debug.WriteLine($"Invalid state transition: {oldState} -> {newState}");
                return false;
            }
        }

        public HoverState CurrentState
        {
            get { lock (_lock) return _currentState; }
        }
    }
    #endregion

    #region Animation Orchestrator (Composition Layer)

    private class AnimationOrchestrator : IDisposable
    {
        private readonly Compositor _compositor;
        private readonly Dictionary<string, CompositionAnimation> _activeAnimations = new();
        private readonly object _animationLock = new();

        public AnimationOrchestrator(Compositor compositor)
        {
            _compositor = compositor;
        }

        public async Task<bool> RunAnimationSequenceAsync(
            string sequenceId,
            Func<CancellationToken, Task> animationTask,
            CancellationToken externalToken = default)
        {
            // Cancel previous animation with same ID
            CancelAnimation(sequenceId);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = linkedCts.Token;

            lock (_animationLock)
            {
                _activeAnimations[sequenceId] = null; // Mark as running
            }

            try
            {
                await animationTask(token);
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Animation {sequenceId} was cancelled");
                return false;
            }
            finally
            {
                lock (_animationLock)
                {
                    _activeAnimations.Remove(sequenceId);
                }
            }
        }

        public CompositionAnimation CreateSpringScaleAnimation(
            Vector3 startScale, Vector3 endScale, TimeSpan duration)
        {
            var springAnimation = _compositor.CreateSpringVector3Animation();
            springAnimation.InitialValue = startScale;
            springAnimation.FinalValue = endScale;
            springAnimation.DampingRatio = 0.7f; // Nice bounce
            springAnimation.Period = TimeSpan.FromMilliseconds(150);
            return springAnimation;
        }

        public CompositionAnimationGroup CreateFadeSlideAnimation(
            float startOpacity, float endOpacity,
            Vector3 startOffset, Vector3 endOffset, TimeSpan duration)
        {
            CompositionAnimationGroup? group = _compositor.CreateAnimationGroup();

            var fade = _compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(0f, startOpacity);
            fade.InsertKeyFrame(1f, endOpacity);
            fade.Duration = duration;
            fade.Target = "Opacity";

            var slide = _compositor.CreateVector3KeyFrameAnimation();
            slide.InsertKeyFrame(0f, startOffset);
            slide.InsertKeyFrame(1f, endOffset);
            slide.Duration = duration;
            slide.Target = "Offset";

            // Use cubic bezier for natural motion
            var easing = _compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0.0f),
                new Vector2(0.2f, 1.0f));

            fade.InsertKeyFrame(1f, endOpacity, easing);
            slide.InsertKeyFrame(1f, endOffset, easing);

            group.Add(fade);
            group.Add(slide);

            return group;
        }

        public void CancelAnimation(string sequenceId)
        {
            lock (_animationLock)
            {
                if (_activeAnimations.TryGetValue(sequenceId, out var animation))
                {
                    animation?.StopAnimationGroup(animation);
                }
            }
        }

        public void Dispose()
        {
            lock (_animationLock)
            {
                foreach (var animation in _activeAnimations.Values)
                {
                    animation?.Dispose();
                }
                _activeAnimations.Clear();
            }
        }
    }
    #endregion

    #region Event Handlers (Simplified & Robust)

    private void OnHoverDelayTick(DispatcherQueueTimer sender, object args)
    {
        _hoverDelayTimer.Stop();

        if (!_stateManager.TryTransitionTo(HoverStateManager.HoverState.Expanding))
            return;

        _ = ExpandAsync();
    }

    private async Task ExpandAsync()
    {
        bool success = await _animationOrchestrator.RunAnimationSequenceAsync(
            "expand",
            async (cancellationToken) =>
            {
                // Set center point for scaling
                _rootVisual.CenterPoint = new Vector3(
                    (float)ActualWidth / 2,
                    (float)ActualHeight / 2,
                    0);

                // Scale animation
                var scaleAnim = _animationOrchestrator.CreateSpringScaleAnimation(
                    new Vector3(1f),
                    new Vector3(1.05f),
                    TimeSpan.FromMilliseconds(250));

                _rootVisual.StartAnimation("Scale", scaleAnim);

                // Extra panel animation
                var extraVisual = ElementCompositionPreview.GetElementVisual(ExtraPanel);

                var fadeSlideAnim = _animationOrchestrator.CreateFadeSlideAnimation(
                    0f, 1f,
                    new Vector3(0, 20, 0), Vector3.Zero,
                    TimeSpan.FromMilliseconds(200));

                extraVisual.StartAnimationGroup(fadeSlideAnim);

                // Wait for animations (or cancellation)
                await Task.Delay(300, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    _stateManager.TryTransitionTo(HoverStateManager.HoverState.Expanded);
                }
            });

        if (!success)
        {
            _stateManager.TryTransitionTo(HoverStateManager.HoverState.Idle);
        }
    }

    private async Task CollapseAsync()
    {
        bool success = await _animationOrchestrator.RunAnimationSequenceAsync(
            "collapse",
            async (cancellationToken) =>
            {
                // Scale back
                var scaleAnim = _animationOrchestrator.CreateSpringScaleAnimation(
                    new Vector3(1.05f),
                    new Vector3(1f),
                    TimeSpan.FromMilliseconds(200));

                _rootVisual.StartAnimation("Scale", scaleAnim);

                // Extra panel hide
                var extraVisual = ElementCompositionPreview.GetElementVisual(ExtraPanel);

                var fadeSlideAnim = _animationOrchestrator.CreateFadeSlideAnimation(
                    1f, 0f,
                    Vector3.Zero, new Vector3(0, 20, 0),
                    TimeSpan.FromMilliseconds(200));

                extraVisual.StartAnimationGroup(fadeSlideAnim);

                await Task.Delay(250, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    _stateManager.TryTransitionTo(HoverStateManager.HoverState.Idle);
                }
            });

        if (!success)
        {
            _stateManager.TryTransitionTo(HoverStateManager.HoverState.Idle);
        }
    }

    private void CardBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_stateManager.CurrentState != HoverStateManager.HoverState.Idle)
            return;

        if (!_stateManager.TryTransitionTo(HoverStateManager.HoverState.HoverPending))
            return;

        _hoverDelayTimer.Start();
    }

    private void CardBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var currentState = _stateManager.CurrentState;

        if (currentState == HoverStateManager.HoverState.HoverPending)
        {
            _hoverDelayTimer.Stop();
            _stateManager.TryTransitionTo(HoverStateManager.HoverState.Idle);
        }
        else if (currentState == HoverStateManager.HoverState.Expanding ||
                 currentState == HoverStateManager.HoverState.Expanded)
        {
            _ = CollapseAsync();
        }
    }
    #endregion

    #region Cleanup

    public void Dispose()
    {
        _hoverDelayTimer.Stop();
        _animationOrchestrator?.Dispose();

        // Stop all running animations
        _rootVisual.StopAnimation("Scale");

        var extraVisual = ElementCompositionPreview.GetElementVisual(ExtraPanel);
        extraVisual.StopAnimation("Opacity");
        extraVisual.StopAnimation("Offset");
    }
    #endregion
}