package crc64cb71f025204412d4;


public class PGestureRecognizer_DXPinch_Detector
	extends android.view.ScaleGestureDetector
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXPinch+Detector, DevExpress.Maui.Core", PGestureRecognizer_DXPinch_Detector.class, __md_methods);
	}

	public PGestureRecognizer_DXPinch_Detector (android.content.Context p0, android.view.ScaleGestureDetector.OnScaleGestureListener p1, android.os.Handler p2)
	{
		super (p0, p1, p2);
		if (getClass () == PGestureRecognizer_DXPinch_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXPinch+Detector, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Views.ScaleGestureDetector+IOnScaleGestureListener, Mono.Android:Android.OS.Handler, Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public PGestureRecognizer_DXPinch_Detector (android.content.Context p0, android.view.ScaleGestureDetector.OnScaleGestureListener p1)
	{
		super (p0, p1);
		if (getClass () == PGestureRecognizer_DXPinch_Detector.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Android.Internal.PGestureRecognizer+DXPinch+Detector, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Views.ScaleGestureDetector+IOnScaleGestureListener, Mono.Android", this, new java.lang.Object[] { p0, p1 });
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
