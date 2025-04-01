package mono.com.devexpress.dxgrid.views;


public class SwipeActionButton_OnTapListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.views.SwipeActionButton.OnTapListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onSwipeButtonTap:(Lcom/devexpress/dxgrid/views/SwipeActionButton;)V:GetOnSwipeButtonTap_Lcom_devexpress_dxgrid_views_SwipeActionButton_Handler:DevExpress.Android.Grid.Views.SwipeActionButton/IOnTapListenerInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Grid.Views.SwipeActionButton+IOnTapListenerImplementor, DXGrid.a", SwipeActionButton_OnTapListenerImplementor.class, __md_methods);
	}

	public SwipeActionButton_OnTapListenerImplementor ()
	{
		super ();
		if (getClass () == SwipeActionButton_OnTapListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Grid.Views.SwipeActionButton+IOnTapListenerImplementor, DXGrid.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onSwipeButtonTap (com.devexpress.dxgrid.views.SwipeActionButton p0)
	{
		n_onSwipeButtonTap (p0);
	}

	private native void n_onSwipeButtonTap (com.devexpress.dxgrid.views.SwipeActionButton p0);

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
