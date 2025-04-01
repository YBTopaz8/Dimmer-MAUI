package mono.com.devexpress.navigation;


public class TabsView_OnTabSelectedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.TabsView.OnTabSelectedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTabSelected:(Lcom/devexpress/navigation/tabs/views/TabItemView;II)V:GetOnTabSelected_Lcom_devexpress_navigation_tabs_views_TabItemView_IIHandler:DevExpress.Android.Navigation.TabsView/IOnTabSelectedListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.TabsView+IOnTabSelectedListenerImplementor, DXNavigation.a", TabsView_OnTabSelectedListenerImplementor.class, __md_methods);
	}

	public TabsView_OnTabSelectedListenerImplementor ()
	{
		super ();
		if (getClass () == TabsView_OnTabSelectedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.TabsView+IOnTabSelectedListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onTabSelected (com.devexpress.navigation.tabs.views.TabItemView p0, int p1, int p2)
	{
		n_onTabSelected (p0, p1, p2);
	}

	private native void n_onTabSelected (com.devexpress.navigation.tabs.views.TabItemView p0, int p1, int p2);

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
