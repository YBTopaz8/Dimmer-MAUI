package crc64a59bfe4fc515a8dd;


public class GroupRowViewProvider
	extends crc64a59bfe4fc515a8dd.ViewProviderBase
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.GroupRowViewProvider,
		com.devexpress.dxgrid.providers.GroupRowViewProviderBase
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_canUpdate:(I)Z:GetCanUpdate_IHandler:DevExpress.Android.Grid.Providers.IGroupRowViewProviderInvoker, DXGrid.a\n" +
			"n_getView:(Landroid/content/Context;I)Landroid/view/View;:GetGetView_Landroid_content_Context_IHandler:DevExpress.Android.Grid.Providers.IGroupRowViewProviderBaseInvoker, DXGrid.a\n" +
			"n_updateView:(Landroid/content/Context;Landroid/view/View;I)V:GetUpdateView_Landroid_content_Context_Landroid_view_View_IHandler:DevExpress.Android.Grid.Providers.IGroupRowViewProviderBaseInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.GroupRowViewProvider, DevExpress.Maui.DataGrid", GroupRowViewProvider.class, __md_methods);
	}

	public GroupRowViewProvider ()
	{
		super ();
		if (getClass () == GroupRowViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.GroupRowViewProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean canUpdate (int p0)
	{
		return n_canUpdate (p0);
	}

	private native boolean n_canUpdate (int p0);

	public android.view.View getView (android.content.Context p0, int p1)
	{
		return n_getView (p0, p1);
	}

	private native android.view.View n_getView (android.content.Context p0, int p1);

	public void updateView (android.content.Context p0, android.view.View p1, int p2)
	{
		n_updateView (p0, p1, p2);
	}

	private native void n_updateView (android.content.Context p0, android.view.View p1, int p2);

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
