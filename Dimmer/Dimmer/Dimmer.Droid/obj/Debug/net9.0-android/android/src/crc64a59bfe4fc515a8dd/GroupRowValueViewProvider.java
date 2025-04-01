package crc64a59bfe4fc515a8dd;


public class GroupRowValueViewProvider
	extends crc64a59bfe4fc515a8dd.GroupRowValueViewProviderBase
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.GroupRowValueViewProvider, DevExpress.Maui.DataGrid", GroupRowValueViewProvider.class, __md_methods);
	}

	public GroupRowValueViewProvider ()
	{
		super ();
		if (getClass () == GroupRowValueViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.GroupRowValueViewProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
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
