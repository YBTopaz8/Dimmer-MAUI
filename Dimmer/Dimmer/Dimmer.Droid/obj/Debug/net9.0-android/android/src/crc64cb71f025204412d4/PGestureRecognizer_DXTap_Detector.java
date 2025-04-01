package crc64cb71f025204412d4;


public class PGestureRecognizer_DXTap_Detector
	extends android.view.GestureDetector
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXTap+Detector, DevExpress.Maui.Core", PGestureRecognizer_DXTap_Detector.class, __md_methods);
	}

	public PGestureRecognizer_DXTap_Detector (android.content.Context p0, android.view.GestureDetector.OnGestureListener p1, android.os.Handler p2, boolean p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == PGestureRecognizer_DXTap_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXTap+Detector, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Views.GestureDetector+IOnGestureListener, Mono.Android:Android.OS.Handler, Mono.Android:System.Boolean, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public PGestureRecognizer_DXTap_Detector (android.content.Context p0, android.view.GestureDetector.OnGestureListener p1, android.os.Handler p2)
	{
		super (p0, p1, p2);
		if (getClass () == PGestureRecognizer_DXTap_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXTap+Detector, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Views.GestureDetector+IOnGestureListener, Mono.Android:Android.OS.Handler, Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public PGestureRecognizer_DXTap_Detector (android.content.Context p0, android.view.GestureDetector.OnGestureListener p1)
	{
		super (p0, p1);
		if (getClass () == PGestureRecognizer_DXTap_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXTap+Detector, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Views.GestureDetector+IOnGestureListener, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public PGestureRecognizer_DXTap_Detector (android.view.GestureDetector.OnGestureListener p0, android.os.Handler p1)
	{
		super (p0, p1);
		if (getClass () == PGestureRecognizer_DXTap_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXTap+Detector, DevExpress.Maui.Core", "Android.Views.GestureDetector+IOnGestureListener, Mono.Android:Android.OS.Handler, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public PGestureRecognizer_DXTap_Detector (android.view.GestureDetector.OnGestureListener p0)
	{
		super (p0);
		if (getClass () == PGestureRecognizer_DXTap_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXTap+Detector, DevExpress.Maui.Core", "Android.Views.GestureDetector+IOnGestureListener, Mono.Android", this, new java.lang.Object[] { p0 });
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
