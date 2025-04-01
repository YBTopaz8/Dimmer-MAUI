package crc64a59bfe4fc515a8dd;


public class CustomAppearanceProvider
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.models.columns.CustomAppearanceProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getCustomAppearance:(I)Lcom/devexpress/dxgrid/models/appearance/AppearanceBase;:GetGetCustomAppearance_IHandler:DevExpress.Android.Grid.Models.Columns.ICustomAppearanceProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.CustomAppearanceProvider, DevExpress.Maui.DataGrid", CustomAppearanceProvider.class, __md_methods);
	}

	public CustomAppearanceProvider ()
	{
		super ();
		if (getClass () == CustomAppearanceProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.CustomAppearanceProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public com.devexpress.dxgrid.models.appearance.AppearanceBase getCustomAppearance (int p0)
	{
		return n_getCustomAppearance (p0);
	}

	private native com.devexpress.dxgrid.models.appearance.AppearanceBase n_getCustomAppearance (int p0);

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
