package mono.com.devexpress.navigation;


public class DrawerView_OnDrawerStateChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.DrawerView.OnDrawerStateChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_OnDrawerStateChanged:(Z)V:GetOnDrawerStateChanged_ZHandler:DevExpress.Android.Navigation.DrawerView/IOnDrawerStateChangedListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.DrawerView+IOnDrawerStateChangedListenerImplementor, DXNavigation.a", DrawerView_OnDrawerStateChangedListenerImplementor.class, __md_methods);
	}

	public DrawerView_OnDrawerStateChangedListenerImplementor ()
	{
		super ();
		if (getClass () == DrawerView_OnDrawerStateChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.DrawerView+IOnDrawerStateChangedListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void OnDrawerStateChanged (boolean p0)
	{
		n_OnDrawerStateChanged (p0);
	}

	private native void n_OnDrawerStateChanged (boolean p0);

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
