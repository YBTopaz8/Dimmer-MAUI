package mono.com.devexpress.navigation.navigationdrawer;


public class IDrawerViewAdapter_ContentChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.navigationdrawer.IDrawerViewAdapter.ContentChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onContentChanged:()V:GetOnContentChangedHandler:DevExpress.Android.Navigation.Navigationdrawer.IDrawerViewAdapter/IContentChangedListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.Navigationdrawer.IDrawerViewAdapter+IContentChangedListenerImplementor, DXNavigation.a", IDrawerViewAdapter_ContentChangedListenerImplementor.class, __md_methods);
	}

	public IDrawerViewAdapter_ContentChangedListenerImplementor ()
	{
		super ();
		if (getClass () == IDrawerViewAdapter_ContentChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.Navigationdrawer.IDrawerViewAdapter+IContentChangedListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onContentChanged ()
	{
		n_onContentChanged ();
	}

	private native void n_onContentChanged ();

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
