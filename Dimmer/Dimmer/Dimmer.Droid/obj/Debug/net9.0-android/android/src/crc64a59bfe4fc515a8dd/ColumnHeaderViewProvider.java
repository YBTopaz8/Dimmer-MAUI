package crc64a59bfe4fc515a8dd;


public class ColumnHeaderViewProvider
	extends crc64a59bfe4fc515a8dd.ViewProviderBase
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.ColumnHeaderViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getColumnHeaderView:(Landroid/content/Context;I)Landroid/view/View;:GetGetColumnHeaderView_Landroid_content_Context_IHandler:DevExpress.Android.Grid.Providers.IColumnHeaderViewProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.ColumnHeaderViewProvider, DevExpress.Maui.DataGrid", ColumnHeaderViewProvider.class, __md_methods);
	}

	public ColumnHeaderViewProvider ()
	{
		super ();
		if (getClass () == ColumnHeaderViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.ColumnHeaderViewProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public android.view.View getColumnHeaderView (android.content.Context p0, int p1)
	{
		return n_getColumnHeaderView (p0, p1);
	}

	private native android.view.View n_getColumnHeaderView (android.content.Context p0, int p1);

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
