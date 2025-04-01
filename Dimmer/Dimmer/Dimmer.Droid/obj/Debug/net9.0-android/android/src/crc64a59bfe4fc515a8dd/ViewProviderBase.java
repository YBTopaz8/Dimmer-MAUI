package crc64a59bfe4fc515a8dd;


public class ViewProviderBase
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.ViewProviderBase, DevExpress.Maui.DataGrid", ViewProviderBase.class, __md_methods);
	}

	public ViewProviderBase ()
	{
		super ();
		if (getClass () == ViewProviderBase.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.ViewProviderBase, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
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
