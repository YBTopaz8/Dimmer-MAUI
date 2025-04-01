package mono.com.devexpress.navigation.tabs.models;


public class Padding_OnPaddingChangeListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.tabs.models.Padding.OnPaddingChangeListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPaddingChanged:()V:GetOnPaddingChangedHandler:DevExpress.Android.Navigation.Tabs.Models.Padding/IOnPaddingChangeListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.Tabs.Models.Padding+IOnPaddingChangeListenerImplementor, DXNavigation.a", Padding_OnPaddingChangeListenerImplementor.class, __md_methods);
	}

	public Padding_OnPaddingChangeListenerImplementor ()
	{
		super ();
		if (getClass () == Padding_OnPaddingChangeListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.Tabs.Models.Padding+IOnPaddingChangeListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onPaddingChanged ()
	{
		n_onPaddingChanged ();
	}

	private native void n_onPaddingChanged ();

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
