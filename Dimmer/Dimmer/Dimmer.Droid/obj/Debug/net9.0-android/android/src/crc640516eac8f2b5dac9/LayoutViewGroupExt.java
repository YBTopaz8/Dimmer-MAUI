package crc640516eac8f2b5dac9;


public class LayoutViewGroupExt
	extends crc6452ffdc5b34af3a0f.LayoutViewGroup
	implements
		mono.android.IGCUserPeer,
		android.view.accessibility.AccessibilityManager.AccessibilityStateChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_dispatchDraw:(Landroid/graphics/Canvas;)V:GetDispatchDraw_Landroid_graphics_Canvas_Handler\n" +
			"n_dispatchHoverEvent:(Landroid/view/MotionEvent;)Z:GetDispatchHoverEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_onSizeChanged:(IIII)V:GetOnSizeChanged_IIIIHandler\n" +
			"n_onMeasure:(II)V:GetOnMeasure_IIHandler\n" +
			"n_onLayout:(ZIIII)V:GetOnLayout_ZIIIIHandler\n" +
			"n_onInterceptTouchEvent:(Landroid/view/MotionEvent;)Z:GetOnInterceptTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_onDetachedFromWindow:()V:GetOnDetachedFromWindowHandler\n" +
			"n_requestLayout:()V:GetRequestLayoutHandler\n" +
			"n_onAccessibilityStateChanged:(Z)V:GetOnAccessibilityStateChanged_ZHandler:Android.Views.Accessibility.AccessibilityManager/IAccessibilityStateChangeListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("Syncfusion.Maui.Toolkit.Platform.LayoutViewGroupExt, Syncfusion.Maui.Toolkit", LayoutViewGroupExt.class, __md_methods);
	}

	public LayoutViewGroupExt (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == LayoutViewGroupExt.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Platform.LayoutViewGroupExt, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public LayoutViewGroupExt (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == LayoutViewGroupExt.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Platform.LayoutViewGroupExt, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public LayoutViewGroupExt (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == LayoutViewGroupExt.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Platform.LayoutViewGroupExt, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public LayoutViewGroupExt (android.content.Context p0)
	{
		super (p0);
		if (getClass () == LayoutViewGroupExt.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Platform.LayoutViewGroupExt, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void dispatchDraw (android.graphics.Canvas p0)
	{
		n_dispatchDraw (p0);
	}

	private native void n_dispatchDraw (android.graphics.Canvas p0);

	public boolean dispatchHoverEvent (android.view.MotionEvent p0)
	{
		return n_dispatchHoverEvent (p0);
	}

	private native boolean n_dispatchHoverEvent (android.view.MotionEvent p0);

	public void onSizeChanged (int p0, int p1, int p2, int p3)
	{
		n_onSizeChanged (p0, p1, p2, p3);
	}

	private native void n_onSizeChanged (int p0, int p1, int p2, int p3);

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

	public boolean onInterceptTouchEvent (android.view.MotionEvent p0)
	{
		return n_onInterceptTouchEvent (p0);
	}

	private native boolean n_onInterceptTouchEvent (android.view.MotionEvent p0);

	public void onDetachedFromWindow ()
	{
		n_onDetachedFromWindow ();
	}

	private native void n_onDetachedFromWindow ();

	public void requestLayout ()
	{
		n_requestLayout ();
	}

	private native void n_requestLayout ();

	public void onAccessibilityStateChanged (boolean p0)
	{
		n_onAccessibilityStateChanged (p0);
	}

	private native void n_onAccessibilityStateChanged (boolean p0);

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
