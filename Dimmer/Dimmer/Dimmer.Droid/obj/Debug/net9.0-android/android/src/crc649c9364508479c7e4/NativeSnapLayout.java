package crc649c9364508479c7e4;


public class NativeSnapLayout
	extends crc6452ffdc5b34af3a0f.LayoutViewGroup
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onInterceptTouchEvent:(Landroid/view/MotionEvent;)Z:GetOnInterceptTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"";
		mono.android.Runtime.register ("Syncfusion.Maui.Toolkit.Calendar.NativeSnapLayout, Syncfusion.Maui.Toolkit", NativeSnapLayout.class, __md_methods);
	}

	public NativeSnapLayout (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == NativeSnapLayout.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Calendar.NativeSnapLayout, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public NativeSnapLayout (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == NativeSnapLayout.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Calendar.NativeSnapLayout, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public NativeSnapLayout (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == NativeSnapLayout.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Calendar.NativeSnapLayout, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public NativeSnapLayout (android.content.Context p0)
	{
		super (p0);
		if (getClass () == NativeSnapLayout.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Calendar.NativeSnapLayout, Syncfusion.Maui.Toolkit", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public boolean onInterceptTouchEvent (android.view.MotionEvent p0)
	{
		return n_onInterceptTouchEvent (p0);
	}

	private native boolean n_onInterceptTouchEvent (android.view.MotionEvent p0);

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
