package crc646b09977338b7360e;


public class EmptyView
	extends android.view.View
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getVisibility:()I:GetGetVisibilityHandler\n" +
			"n_setVisibility:(I)V:GetSetVisibility_IHandler\n" +
			"n_getAlpha:()F:GetGetAlphaHandler\n" +
			"n_setAlpha:(F)V:GetSetAlpha_FHandler\n" +
			"n_onMeasure:(II)V:GetOnMeasure_IIHandler\n" +
			"n_setBackgroundColor:(I)V:GetSetBackgroundColor_IHandler\n" +
			"n_onLayout:(ZIIII)V:GetOnLayout_ZIIIIHandler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Controls.Internal.EmptyView, DevExpress.Maui.Controls", EmptyView.class, __md_methods);
	}

	public EmptyView (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == EmptyView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.EmptyView, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public EmptyView (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == EmptyView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.EmptyView, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public EmptyView (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == EmptyView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.EmptyView, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public EmptyView (android.content.Context p0)
	{
		super (p0);
		if (getClass () == EmptyView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.EmptyView, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public int getVisibility ()
	{
		return n_getVisibility ();
	}

	private native int n_getVisibility ();

	public void setVisibility (int p0)
	{
		n_setVisibility (p0);
	}

	private native void n_setVisibility (int p0);

	public float getAlpha ()
	{
		return n_getAlpha ();
	}

	private native float n_getAlpha ();

	public void setAlpha (float p0)
	{
		n_setAlpha (p0);
	}

	private native void n_setAlpha (float p0);

	public void onMeasure (int p0, int p1)
	{
		n_onMeasure (p0, p1);
	}

	private native void n_onMeasure (int p0, int p1);

	public void setBackgroundColor (int p0)
	{
		n_setBackgroundColor (p0);
	}

	private native void n_setBackgroundColor (int p0);

	public void onLayout (boolean p0, int p1, int p2, int p3, int p4)
	{
		n_onLayout (p0, p1, p2, p3, p4);
	}

	private native void n_onLayout (boolean p0, int p1, int p2, int p3, int p4);

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
