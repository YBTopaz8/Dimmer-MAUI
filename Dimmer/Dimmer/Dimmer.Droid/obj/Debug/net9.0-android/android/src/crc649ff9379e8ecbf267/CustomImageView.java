package crc649ff9379e8ecbf267;


public class CustomImageView
	extends android.widget.FrameLayout
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Syncfusion.Maui.Toolkit.Carousel.CustomImageView, Syncfusion.Maui.Toolkit", CustomImageView.class, __md_methods);
	}

	public CustomImageView (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == CustomImageView.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Carousel.CustomImageView, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public CustomImageView (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == CustomImageView.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Carousel.CustomImageView, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public CustomImageView (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == CustomImageView.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Carousel.CustomImageView, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public CustomImageView (android.content.Context p0)
	{
		super (p0);
		if (getClass () == CustomImageView.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Carousel.CustomImageView, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
