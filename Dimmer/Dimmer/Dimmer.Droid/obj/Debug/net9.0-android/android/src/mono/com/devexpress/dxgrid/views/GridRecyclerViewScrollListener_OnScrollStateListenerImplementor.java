package mono.com.devexpress.dxgrid.views;


public class GridRecyclerViewScrollListener_OnScrollStateListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.views.GridRecyclerViewScrollListener.OnScrollStateListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onScrolled:(II)V:GetOnScrolled_IIHandler:DevExpress.Android.Grid.Views.GridRecyclerViewScrollListener/IOnScrollStateListenerInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Grid.Views.GridRecyclerViewScrollListener+IOnScrollStateListenerImplementor, DXGrid.a", GridRecyclerViewScrollListener_OnScrollStateListenerImplementor.class, __md_methods);
	}

	public GridRecyclerViewScrollListener_OnScrollStateListenerImplementor ()
	{
		super ();
		if (getClass () == GridRecyclerViewScrollListener_OnScrollStateListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Grid.Views.GridRecyclerViewScrollListener+IOnScrollStateListenerImplementor, DXGrid.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onScrolled (int p0, int p1)
	{
		n_onScrolled (p0, p1);
	}

	private native void n_onScrolled (int p0, int p1);

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
