package crc64222d609bdd44b761;


public class DayOfWeekViewProvider
	extends crc64222d609bdd44b761.CellViewProvider_2
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.pickers.providers.DayOfWeekViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_check:(Landroid/view/View;I)Z:GetCheck_Landroid_view_View_IHandler:DevExpress.Android.Editors.Pickers.Providers.IDayOfWeekViewProviderInvoker, DXEditors.a\n" +
			"n_recycle:(Landroid/view/View;)V:GetRecycle_Landroid_view_View_Handler:DevExpress.Android.Editors.Pickers.Providers.IDayOfWeekViewProviderInvoker, DXEditors.a\n" +
			"n_request:(I)Landroid/view/View;:GetRequest_IHandler:DevExpress.Android.Editors.Pickers.Providers.IDayOfWeekViewProviderInvoker, DXEditors.a\n" +
			"n_update:(Landroid/view/View;I)V:GetUpdate_Landroid_view_View_IHandler:DevExpress.Android.Editors.Pickers.Providers.IDayOfWeekViewProviderInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.DayOfWeekViewProvider, DevExpress.Maui.Editors", DayOfWeekViewProvider.class, __md_methods);
	}

	public DayOfWeekViewProvider ()
	{
		super ();
		if (getClass () == DayOfWeekViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.DayOfWeekViewProvider, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean check (android.view.View p0, int p1)
	{
		return n_check (p0, p1);
	}

	private native boolean n_check (android.view.View p0, int p1);

	public void recycle (android.view.View p0)
	{
		n_recycle (p0);
	}

	private native void n_recycle (android.view.View p0);

	public android.view.View request (int p0)
	{
		return n_request (p0);
	}

	private native android.view.View n_request (int p0);

	public void update (android.view.View p0, int p1)
	{
		n_update (p0, p1);
	}

	private native void n_update (android.view.View p0, int p1);

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
