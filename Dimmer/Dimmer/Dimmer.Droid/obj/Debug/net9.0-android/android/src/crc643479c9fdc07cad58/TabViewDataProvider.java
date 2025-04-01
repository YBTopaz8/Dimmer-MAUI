package crc643479c9fdc07cad58;


public class TabViewDataProvider
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.tabcontrol.ITabControlAdapter
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getFragmentManager:()Ljava/lang/Object;:GetGetFragmentManagerHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_setFragmentManager:(Ljava/lang/Object;)V:GetSetFragmentManager_Ljava_lang_Object_Handler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_isFragmentAdapter:()Z:GetIsFragmentAdapterHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_getItemsCount:()I:GetGetItemsCountHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_addItemsSetChangedListener:(Lcom/devexpress/navigation/tabcontrol/ITabControlAdapter$ItemsChangedListener;)V:GetAddItemsSetChangedListener_Lcom_devexpress_navigation_tabcontrol_ITabControlAdapter_ItemsChangedListener_Handler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_clearItemsSetChangedListener:()V:GetClearItemsSetChangedListenerHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_getFragment:(I)Ljava/lang/Object;:GetGetFragment_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_getHeaderView:(Landroid/view/ViewGroup;I)Lcom/devexpress/navigation/tabs/models/HeaderItemModel;:GetGetHeaderView_Landroid_view_ViewGroup_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_getHeaderViews:(Landroid/view/ViewGroup;)Ljava/util/List;:GetGetHeaderViews_Landroid_view_ViewGroup_Handler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"n_getView:(Landroid/view/ViewGroup;I)Landroid/view/View;:GetGetView_Landroid_view_ViewGroup_IHandler:DevExpress.Android.Navigation.Tabcontrol.ITabControlAdapterInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Controls.Android.Internal.TabViewDataProvider, DevExpress.Maui.Controls", TabViewDataProvider.class, __md_methods);
	}

	public TabViewDataProvider ()
	{
		super ();
		if (getClass () == TabViewDataProvider.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Android.Internal.TabViewDataProvider, DevExpress.Maui.Controls", "", this, new java.lang.Object[] {  });
		}
	}

	public java.lang.Object getFragmentManager ()
	{
		return n_getFragmentManager ();
	}

	private native java.lang.Object n_getFragmentManager ();

	public void setFragmentManager (java.lang.Object p0)
	{
		n_setFragmentManager (p0);
	}

	private native void n_setFragmentManager (java.lang.Object p0);

	public boolean isFragmentAdapter ()
	{
		return n_isFragmentAdapter ();
	}

	private native boolean n_isFragmentAdapter ();

	public int getItemsCount ()
	{
		return n_getItemsCount ();
	}

	private native int n_getItemsCount ();

	public void addItemsSetChangedListener (com.devexpress.navigation.tabcontrol.ITabControlAdapter.ItemsChangedListener p0)
	{
		n_addItemsSetChangedListener (p0);
	}

	private native void n_addItemsSetChangedListener (com.devexpress.navigation.tabcontrol.ITabControlAdapter.ItemsChangedListener p0);

	public void clearItemsSetChangedListener ()
	{
		n_clearItemsSetChangedListener ();
	}

	private native void n_clearItemsSetChangedListener ();

	public java.lang.Object getFragment (int p0)
	{
		return n_getFragment (p0);
	}

	private native java.lang.Object n_getFragment (int p0);

	public com.devexpress.navigation.tabs.models.HeaderItemModel getHeaderView (android.view.ViewGroup p0, int p1)
	{
		return n_getHeaderView (p0, p1);
	}

	private native com.devexpress.navigation.tabs.models.HeaderItemModel n_getHeaderView (android.view.ViewGroup p0, int p1);

	public java.util.List getHeaderViews (android.view.ViewGroup p0)
	{
		return n_getHeaderViews (p0);
	}

	private native java.util.List n_getHeaderViews (android.view.ViewGroup p0);

	public android.view.View getView (android.view.ViewGroup p0, int p1)
	{
		return n_getView (p0, p1);
	}

	private native android.view.View n_getView (android.view.ViewGroup p0, int p1);

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
