package crc64cb71f025204412d4;


public class DXSafeKeyboardAreaView
	extends crc64cb71f025204412d4.PView
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_dispatchVisibilityChanged:(Landroid/view/View;I)V:GetDispatchVisibilityChanged_Landroid_view_View_IHandler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Android.Internal.DXSafeKeyboardAreaView, DevExpress.Maui.Core", DXSafeKeyboardAreaView.class, __md_methods);
	}

	public DXSafeKeyboardAreaView (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == DXSafeKeyboardAreaView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.DXSafeKeyboardAreaView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public DXSafeKeyboardAreaView (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == DXSafeKeyboardAreaView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.DXSafeKeyboardAreaView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public DXSafeKeyboardAreaView (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == DXSafeKeyboardAreaView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.DXSafeKeyboardAreaView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public DXSafeKeyboardAreaView (android.content.Context p0)
	{
		super (p0);
		if (getClass () == DXSafeKeyboardAreaView.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.DXSafeKeyboardAreaView, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public void dispatchVisibilityChanged (android.view.View p0, int p1)
	{
		n_dispatchVisibilityChanged (p0, p1);
	}

	private native void n_dispatchVisibilityChanged (android.view.View p0, int p1);

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
