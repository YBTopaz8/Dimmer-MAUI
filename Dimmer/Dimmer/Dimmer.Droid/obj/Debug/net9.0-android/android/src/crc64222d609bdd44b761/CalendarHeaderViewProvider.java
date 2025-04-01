package crc64222d609bdd44b761;


public class CalendarHeaderViewProvider
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.pickers.providers.HeaderViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_check:(Landroid/view/View;IIILcom/devexpress/editors/pickers/CalendarViewStates;)Z:GetCheck_Landroid_view_View_IIILcom_devexpress_editors_pickers_CalendarViewStates_Handler:DevExpress.Android.Editors.Pickers.Providers.IHeaderViewProviderInvoker, DXEditors.a\n" +
			"n_recycle:(Landroid/view/View;Lcom/devexpress/editors/pickers/CalendarViewStates;)V:GetRecycle_Landroid_view_View_Lcom_devexpress_editors_pickers_CalendarViewStates_Handler:DevExpress.Android.Editors.Pickers.Providers.IHeaderViewProviderInvoker, DXEditors.a\n" +
			"n_request:(IIILcom/devexpress/editors/pickers/CalendarViewStates;)Landroid/view/View;:GetRequest_IIILcom_devexpress_editors_pickers_CalendarViewStates_Handler:DevExpress.Android.Editors.Pickers.Providers.IHeaderViewProviderInvoker, DXEditors.a\n" +
			"n_update:(Landroid/view/View;IIILcom/devexpress/editors/pickers/CalendarViewStates;)V:GetUpdate_Landroid_view_View_IIILcom_devexpress_editors_pickers_CalendarViewStates_Handler:DevExpress.Android.Editors.Pickers.Providers.IHeaderViewProviderInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.CalendarHeaderViewProvider, DevExpress.Maui.Editors", CalendarHeaderViewProvider.class, __md_methods);
	}

	public CalendarHeaderViewProvider ()
	{
		super ();
		if (getClass () == CalendarHeaderViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.CalendarHeaderViewProvider, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean check (android.view.View p0, int p1, int p2, int p3, com.devexpress.editors.pickers.CalendarViewStates p4)
	{
		return n_check (p0, p1, p2, p3, p4);
	}

	private native boolean n_check (android.view.View p0, int p1, int p2, int p3, com.devexpress.editors.pickers.CalendarViewStates p4);

	public void recycle (android.view.View p0, com.devexpress.editors.pickers.CalendarViewStates p1)
	{
		n_recycle (p0, p1);
	}

	private native void n_recycle (android.view.View p0, com.devexpress.editors.pickers.CalendarViewStates p1);

	public android.view.View request (int p0, int p1, int p2, com.devexpress.editors.pickers.CalendarViewStates p3)
	{
		return n_request (p0, p1, p2, p3);
	}

	private native android.view.View n_request (int p0, int p1, int p2, com.devexpress.editors.pickers.CalendarViewStates p3);

	public void update (android.view.View p0, int p1, int p2, int p3, com.devexpress.editors.pickers.CalendarViewStates p4)
	{
		n_update (p0, p1, p2, p3, p4);
	}

	private native void n_update (android.view.View p0, int p1, int p2, int p3, com.devexpress.editors.pickers.CalendarViewStates p4);

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
