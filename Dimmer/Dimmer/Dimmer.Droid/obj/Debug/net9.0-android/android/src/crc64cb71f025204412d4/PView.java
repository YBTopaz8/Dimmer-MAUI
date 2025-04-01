package crc64cb71f025204412d4;


public class PView
	extends crc647a19118a24842bb1.DiagnosticViewGroup
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onAttachedToWindow:()V:GetOnAttachedToWindowHandler\n" +
			"n_onDetachedFromWindow:()V:GetOnDetachedFromWindowHandler\n" +
			"n_onMeasure:(II)V:GetOnMeasure_IIHandler\n" +
			"n_onLayout:(ZIIII)V:GetOnLayout_ZIIIIHandler\n" +
			"n_dispatchDraw:(Landroid/graphics/Canvas;)V:GetDispatchDraw_Landroid_graphics_Canvas_Handler\n" +
			"n_requestDisallowInterceptTouchEvent:(Z)V:GetRequestDisallowInterceptTouchEvent_ZHandler\n" +
			"n_dispatchTouchEvent:(Landroid/view/MotionEvent;)Z:GetDispatchTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_onInterceptTouchEvent:(Landroid/view/MotionEvent;)Z:GetOnInterceptTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_onTouchEvent:(Landroid/view/MotionEvent;)Z:GetOnTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_invalidateDrawable:(Landroid/graphics/drawable/Drawable;)V:GetInvalidateDrawable_Landroid_graphics_drawable_Drawable_Handler\n" +
			"n_onDrawForeground:(Landroid/graphics/Canvas;)V:GetOnDrawForeground_Landroid_graphics_Canvas_Handler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Android.Internal.PView, DevExpress.Maui.Core", PView.class, __md_methods);
	}

	public PView (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == PView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public PView (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == PView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public PView (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == PView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public PView (android.content.Context p0)
	{
		super (p0);
		if (getClass () == PView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void onAttachedToWindow ()
	{
		n_onAttachedToWindow ();
	}

	private native void n_onAttachedToWindow ();

	public void onDetachedFromWindow ()
	{
		n_onDetachedFromWindow ();
	}

	private native void n_onDetachedFromWindow ();

	public void onMeasure (int p0, int p1)
	{
		n_onMeasure (p0, p1);
	}

	private native void n_onMeasure (int p0, int p1);

	public void onLayout (boolean p0, int p1, int p2, int p3, int p4)
	{
		n_onLayout (p0, p1, p2, p3, p4);
	}

	private native void n_onLayout (boolean p0, int p1, int p2, int p3, int p4);

	public void dispatchDraw (android.graphics.Canvas p0)
	{
		n_dispatchDraw (p0);
	}

	private native void n_dispatchDraw (android.graphics.Canvas p0);

	public void requestDisallowInterceptTouchEvent (boolean p0)
	{
		n_requestDisallowInterceptTouchEvent (p0);
	}

	private native void n_requestDisallowInterceptTouchEvent (boolean p0);

	public boolean dispatchTouchEvent (android.view.MotionEvent p0)
	{
		return n_dispatchTouchEvent (p0);
	}

	private native boolean n_dispatchTouchEvent (android.view.MotionEvent p0);

	public boolean onInterceptTouchEvent (android.view.MotionEvent p0)
	{
		return n_onInterceptTouchEvent (p0);
	}

	private native boolean n_onInterceptTouchEvent (android.view.MotionEvent p0);

	public boolean onTouchEvent (android.view.MotionEvent p0)
	{
		return n_onTouchEvent (p0);
	}

	private native boolean n_onTouchEvent (android.view.MotionEvent p0);

	public void invalidateDrawable (android.graphics.drawable.Drawable p0)
	{
		n_invalidateDrawable (p0);
	}

	private native void n_invalidateDrawable (android.graphics.drawable.Drawable p0);

	public void onDrawForeground (android.graphics.Canvas p0)
	{
		n_onDrawForeground (p0);
	}

	private native void n_onDrawForeground (android.graphics.Canvas p0);

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
