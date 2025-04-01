package crc64a59bfe4fc515a8dd;


public class SwipeItemViewProvider
	extends crc64a59bfe4fc515a8dd.ViewProviderBase
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.SwipeButtonViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getView:(Landroid/content/Context;I)Landroid/view/View;:GetGetView_Landroid_content_Context_IHandler:DevExpress.Android.Grid.Providers.ISwipeButtonViewProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.SwipeItemViewProvider, DevExpress.Maui.DataGrid", SwipeItemViewProvider.class, __md_methods);
	}

	public SwipeItemViewProvider ()
	{
		super ();
		if (getClass () == SwipeItemViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.SwipeItemViewProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public android.view.View getView (android.content.Context p0, int p1)
	{
		return n_getView (p0, p1);
	}

	private native android.view.View n_getView (android.content.Context p0, int p1);

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
