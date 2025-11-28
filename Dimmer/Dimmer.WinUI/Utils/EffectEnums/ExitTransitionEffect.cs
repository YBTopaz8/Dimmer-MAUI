using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils;

public enum ExitTransitionEffect
{
    FadeSlideDown,    // 1. The requested effect
    ZoomOut,          // 2. Shrink into the center
    SlideRight,       // 3. Move off-screen to the right
    SlideLeft,        // 4. Move off-screen to the left
    FlyUp,            // 5. Rise up and fade out
    SpinAndShrink,    // 6. Rotate 360 while disappearing
    FlipHorizontal,   // 7. Card flip effect (Y-axis)
    FoldVertical,     // 8. Squashes vertically like a closing eye
    Explode           // 9. Scales up large while fading out
}