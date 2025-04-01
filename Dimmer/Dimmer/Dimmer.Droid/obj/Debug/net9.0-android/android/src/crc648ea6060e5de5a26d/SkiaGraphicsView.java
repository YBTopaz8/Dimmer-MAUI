package crc648ea6060e5de5a26d;


public class SkiaGraphicsView
	extends crc648e35430423bd4943.SKCanvasView
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onSizeChanged:(IIII)V:GetOnSizeChanged_IIIIHandler\n" +
			"";
		mono.android.Runtime.register ("Microsoft.Maui.Graphics.Skia.Views.SkiaGraphicsView, Microsoft.Maui.Graphics.Skia", SkiaGraphicsView.class, __md_methods);
	}

	public SkiaGraphicsView (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == SkiaGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Graphics.Skia.Views.SkiaGraphicsView, Microsoft.Maui.Graphics.Skia", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public SkiaGraphicsView (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == SkiaGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Graphics.Skia.Views.SkiaGraphicsView, Microsoft.Maui.Graphics.Skia", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public SkiaGraphicsView (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == SkiaGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Graphics.Skia.Views.SkiaGraphicsView, Microsoft.Maui.Graphics.Skia", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public SkiaGraphicsView (android.content.Context p0)
	{
		super (p0);
		if (getClass () == SkiaGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Graphics.Skia.Views.SkiaGraphicsView, Microsoft.Maui.Graphics.Skia", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void onSizeChanged (int p0, int p1, int p2, int p3)
	{
		n_onSizeChanged (p0, p1, p2, p3);
	}

	private native void n_onSizeChanged (int p0, int p1, int p2, int p3);

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
