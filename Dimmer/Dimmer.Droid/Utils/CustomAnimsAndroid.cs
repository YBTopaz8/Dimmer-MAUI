
namespace Dimmer.Utils.CustomAnimsAndroid;
public static class CustomAnimsAndroid
{

	public static void RunFocusModeAnimation(this DXBorder avatarView, Color strokeColor)
	{
		if (avatarView == null)
			return;

		// Set the stroke color based on pause/resume state
		avatarView.BorderColor = strokeColor;

		// Define a single animation to embiggen the stroke
		Animation expandAnimation = new Animation(v => avatarView.BorderThickness = v, // Only animating StrokeThickness now
			0,                                   // StartAsync with 0 thickness
			5,                                  // Expand to 10 thickness
			Easing.CubicInOut                    // Smooth easing
		);

		// Shrink the stroke back to zero after embiggen
		Animation shrinkAnimation = new Animation(
			v => avatarView.BorderThickness = v,
			5,                                   // StartAsync at 10 thickness
			0,                                    // Reduce to 0 thickness
			Easing.CubicInOut
		);

		// Combine expand and shrink animations into one sequence
		Animation animationSequence = new Animation
		{
			{ 0, 0.5, expandAnimation },   // Embiggen in the first half
            { 0.5, 1, shrinkAnimation }    // Shrink back in the second half
        };

		// Run the full animation sequence
		animationSequence.Commit(avatarView, "FocusModeAnimation", length: 500, easing: Easing.Linear);
	}

}