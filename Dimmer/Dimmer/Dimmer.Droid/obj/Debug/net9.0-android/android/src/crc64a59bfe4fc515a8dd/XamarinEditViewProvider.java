package crc64a59bfe4fc515a8dd;


public class XamarinEditViewProvider
	extends crc64a59bfe4fc515a8dd.ViewProviderBase
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.providers.EditViewProvider
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_isPaddingInEditor:()Z:GetIsPaddingInEditorHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_getEditableView:(Landroid/content/Context;I)Landroid/view/View;:GetGetEditableView_Landroid_content_Context_IHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_open:(ZI)V:GetOpen_ZIHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_setRequestUpdateRunnable:(Ljava/lang/Runnable;I)V:GetSetRequestUpdateRunnable_Ljava_lang_Runnable_IHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_submitEditValue:(Landroid/view/View;I)Ljava/lang/String;:GetSubmitEditValue_Landroid_view_View_IHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_updateAppearance:(Lcom/devexpress/dxgrid/models/appearance/AppearanceBase;I)V:GetUpdateAppearance_Lcom_devexpress_dxgrid_models_appearance_AppearanceBase_IHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_updateAppearance:(I)V:GetUpdateAppearance_IHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"n_updateContent:(I)V:GetUpdateContent_IHandler:DevExpress.Android.Grid.Providers.IEditViewProviderInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataGrid.Android.Internal.XamarinEditViewProvider, DevExpress.Maui.DataGrid", XamarinEditViewProvider.class, __md_methods);
	}

	public XamarinEditViewProvider ()
	{
		super ();
		if (getClass () == XamarinEditViewProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataGrid.Android.Internal.XamarinEditViewProvider, DevExpress.Maui.DataGrid", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean isPaddingInEditor ()
	{
		return n_isPaddingInEditor ();
	}

	private native boolean n_isPaddingInEditor ();

	public android.view.View getEditableView (android.content.Context p0, int p1)
	{
		return n_getEditableView (p0, p1);
	}

	private native android.view.View n_getEditableView (android.content.Context p0, int p1);

	public void open (boolean p0, int p1)
	{
		n_open (p0, p1);
	}

	private native void n_open (boolean p0, int p1);

	public void setRequestUpdateRunnable (java.lang.Runnable p0, int p1)
	{
		n_setRequestUpdateRunnable (p0, p1);
	}

	private native void n_setRequestUpdateRunnable (java.lang.Runnable p0, int p1);

	public java.lang.String submitEditValue (android.view.View p0, int p1)
	{
		return n_submitEditValue (p0, p1);
	}

	private native java.lang.String n_submitEditValue (android.view.View p0, int p1);

	public void updateAppearance (com.devexpress.dxgrid.models.appearance.AppearanceBase p0, int p1)
	{
		n_updateAppearance (p0, p1);
	}

	private native void n_updateAppearance (com.devexpress.dxgrid.models.appearance.AppearanceBase p0, int p1);

	public void updateAppearance (int p0)
	{
		n_updateAppearance (p0);
	}

	private native void n_updateAppearance (int p0);

	public void updateContent (int p0)
	{
		n_updateContent (p0);
	}

	private native void n_updateContent (int p0);

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
