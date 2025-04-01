package crc64a59bfe4fc515a8dd;


public class TotalSummaryViewProvider
	extends crc64a59bfe4fc515a8dd.ViewProviderBase
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.TotalSummaryViewProvider,
		com.devexpress.dxgrid.providers.TotalSummaryViewProviderBase
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_canGet:(I)Z:GetCanGet_IHandler:DevExpress.Android.Grid.Providers.ITotalSummaryViewProviderInvoker, DXGrid.a\n" +
			"n_getTotalSummaryView:(Landroid/content/Context;I)Landroid/view/View;:GetGetTotalSummaryView_Landroid_content_Context_IHandler:DevExpress.Android.Grid.Providers.ITotalSummaryViewProviderBaseInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.TotalSummaryViewProvider, DevExpress.Maui.DataGrid", TotalSummaryViewProvider.class, __md_methods);
	}

	public TotalSummaryViewProvider ()
	{
		super ();
		if (getClass () == TotalSummaryViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.TotalSummaryViewProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean canGet (int p0)
	{
		return n_canGet (p0);
	}

	private native boolean n_canGet (int p0);

	public android.view.View getTotalSummaryView (android.content.Context p0, int p1)
	{
		return n_getTotalSummaryView (p0, p1);
	}

	private native android.view.View n_getTotalSummaryView (android.content.Context p0, int p1);

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
