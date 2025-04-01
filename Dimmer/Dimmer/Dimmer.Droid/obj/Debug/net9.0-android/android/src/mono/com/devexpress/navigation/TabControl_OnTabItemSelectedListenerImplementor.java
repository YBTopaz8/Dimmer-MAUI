package mono.com.devexpress.navigation;


public class TabControl_OnTabItemSelectedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.TabControl.OnTabItemSelectedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTabItemSelected:(II)V:GetOnTabItemSelected_IIHandler:DevExpress.Android.Navigation.TabControl/IOnTabItemSelectedListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.TabControl+IOnTabItemSelectedListenerImplementor, DXNavigation.a", TabControl_OnTabItemSelectedListenerImplementor.class, __md_methods);
	}

	public TabControl_OnTabItemSelectedListenerImplementor ()
	{
		super ();
		if (getClass () == TabControl_OnTabItemSelectedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.TabControl+IOnTabItemSelectedListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onTabItemSelected (int p0, int p1)
	{
		n_onTabItemSelected (p0, p1);
	}

	private native void n_onTabItemSelected (int p0, int p1);

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
