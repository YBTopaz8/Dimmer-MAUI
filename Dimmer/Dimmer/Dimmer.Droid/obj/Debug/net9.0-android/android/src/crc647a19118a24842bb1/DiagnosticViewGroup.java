package crc647a19118a24842bb1;


public abstract class DiagnosticViewGroup
	extends android.view.ViewGroup
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Core.Internal.DiagnosticViewGroup, DevExpress.Maui.Core", DiagnosticViewGroup.class, __md_methods);
	}

	public DiagnosticViewGroup (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == DiagnosticViewGroup.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Internal.DiagnosticViewGroup, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}

	public DiagnosticViewGroup (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == DiagnosticViewGroup.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Internal.DiagnosticViewGroup, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public DiagnosticViewGroup (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == DiagnosticViewGroup.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Internal.DiagnosticViewGroup, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public DiagnosticViewGroup (android.content.Context p0)
	{
		super (p0);
		if (getClass () == DiagnosticViewGroup.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Core.Internal.DiagnosticViewGroup, DevExpress.Maui.Core", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
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
