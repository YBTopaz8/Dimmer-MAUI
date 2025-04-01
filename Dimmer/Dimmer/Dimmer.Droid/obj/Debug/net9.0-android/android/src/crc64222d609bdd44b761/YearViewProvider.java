package crc64222d609bdd44b761;


public class YearViewProvider
	extends crc64222d609bdd44b761.CellViewProvider_2
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.pickers.providers.YearViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_check:(Landroid/view/View;II)Z:GetCheck_Landroid_view_View_IIHandler:DevExpress.Android.Editors.Pickers.Providers.IYearViewProviderInvoker, DXEditors.a\n" +
			"n_recycle:(Landroid/view/View;)V:GetRecycle_Landroid_view_View_Handler:DevExpress.Android.Editors.Pickers.Providers.IYearViewProviderInvoker, DXEditors.a\n" +
			"n_request:(II)Landroid/view/View;:GetRequest_IIHandler:DevExpress.Android.Editors.Pickers.Providers.IYearViewProviderInvoker, DXEditors.a\n" +
			"n_update:(Landroid/view/View;II)V:GetUpdate_Landroid_view_View_IIHandler:DevExpress.Android.Editors.Pickers.Providers.IYearViewProviderInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.YearViewProvider, DevExpress.Maui.Editors", YearViewProvider.class, __md_methods);
	}

	public YearViewProvider ()
	{
		super ();
		if (getClass () == YearViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.YearViewProvider, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean check (android.view.View p0, int p1, int p2)
	{
		return n_check (p0, p1, p2);
	}

	private native boolean n_check (android.view.View p0, int p1, int p2);

	public void recycle (android.view.View p0)
	{
		n_recycle (p0);
	}

	private native void n_recycle (android.view.View p0);

	public android.view.View request (int p0, int p1)
	{
		return n_request (p0, p1);
	}

	private native android.view.View n_request (int p0, int p1);

	public void update (android.view.View p0, int p1, int p2)
	{
		n_update (p0, p1, p2);
	}

	private native void n_update (android.view.View p0, int p1, int p2);

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
