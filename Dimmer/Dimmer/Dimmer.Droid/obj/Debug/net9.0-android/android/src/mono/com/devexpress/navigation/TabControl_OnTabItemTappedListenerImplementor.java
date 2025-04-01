package mono.com.devexpress.navigation;


public class TabControl_OnTabItemTappedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.TabControl.OnTabItemTappedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTabItemTapped:(ILcom/devexpress/navigation/tabs/models/CancelEventArgs;)V:GetOnTabItemTapped_ILcom_devexpress_navigation_tabs_models_CancelEventArgs_Handler:DevExpress.Android.Navigation.TabControl/IOnTabItemTappedListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.TabControl+IOnTabItemTappedListenerImplementor, DXNavigation.a", TabControl_OnTabItemTappedListenerImplementor.class, __md_methods);
	}

	public TabControl_OnTabItemTappedListenerImplementor ()
	{
		super ();
		if (getClass () == TabControl_OnTabItemTappedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.TabControl+IOnTabItemTappedListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onTabItemTapped (int p0, com.devexpress.navigation.tabs.models.CancelEventArgs p1)
	{
		n_onTabItemTapped (p0, p1);
	}

	private native void n_onTabItemTapped (int p0, com.devexpress.navigation.tabs.models.CancelEventArgs p1);

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
