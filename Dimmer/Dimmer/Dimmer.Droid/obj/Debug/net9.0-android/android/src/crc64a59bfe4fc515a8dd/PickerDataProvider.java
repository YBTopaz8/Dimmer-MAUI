package crc64a59bfe4fc515a8dd;


public class PickerDataProvider
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.PickerDataProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getItemCount:(I)I:GetGetItemCount_IHandler:DevExpress.Android.Grid.Providers.IPickerDataProviderInvoker, DXGrid.a\n" +
			"n_getItemIndex:(I)I:GetGetItemIndex_IHandler:DevExpress.Android.Grid.Providers.IPickerDataProviderInvoker, DXGrid.a\n" +
			"n_getText:(II)Ljava/lang/String;:GetGetText_IIHandler:DevExpress.Android.Grid.Providers.IPickerDataProviderInvoker, DXGrid.a\n" +
			"n_setItemIndex:(II)Ljava/lang/String;:GetSetItemIndex_IIHandler:DevExpress.Android.Grid.Providers.IPickerDataProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.PickerDataProvider, DevExpress.Maui.DataGrid", PickerDataProvider.class, __md_methods);
	}

	public PickerDataProvider ()
	{
		super ();
		if (getClass () == PickerDataProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.PickerDataProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public int getItemCount (int p0)
	{
		return n_getItemCount (p0);
	}

	private native int n_getItemCount (int p0);

	public int getItemIndex (int p0)
	{
		return n_getItemIndex (p0);
	}

	private native int n_getItemIndex (int p0);

	public java.lang.String getText (int p0, int p1)
	{
		return n_getText (p0, p1);
	}

	private native java.lang.String n_getText (int p0, int p1);

	public java.lang.String setItemIndex (int p0, int p1)
	{
		return n_setItemIndex (p0, p1);
	}

	private native java.lang.String n_setItemIndex (int p0, int p1);

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
