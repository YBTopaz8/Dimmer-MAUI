package crc64222d609bdd44b761;


public class InCGViewProvider
	extends crc64222d609bdd44b761.CGViewProvider_1
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.InCGViewProvider, DevExpress.Maui.Editors", InCGViewProvider.class, __md_methods);
	}

	public InCGViewProvider ()
	{
		super ();
		if (getClass () == InCGViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.InCGViewProvider, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
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
