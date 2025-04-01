package crc64a59bfe4fc515a8dd;


public class DragPreviewTemplateProvider
	extends crc64a59bfe4fc515a8dd.ViewProviderBase
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.DragPreviewTemplateProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getDragPreview:(Landroid/content/Context;I)Landroid/view/View;:GetGetDragPreview_Landroid_content_Context_IHandler:DevExpress.Android.Grid.Providers.IDragPreviewTemplateProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.DragPreviewTemplateProvider, DevExpress.Maui.DataGrid", DragPreviewTemplateProvider.class, __md_methods);
	}

	public DragPreviewTemplateProvider ()
	{
		super ();
		if (getClass () == DragPreviewTemplateProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.DragPreviewTemplateProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public android.view.View getDragPreview (android.content.Context p0, int p1)
	{
		return n_getDragPreview (p0, p1);
	}

	private native android.view.View n_getDragPreview (android.content.Context p0, int p1);

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
