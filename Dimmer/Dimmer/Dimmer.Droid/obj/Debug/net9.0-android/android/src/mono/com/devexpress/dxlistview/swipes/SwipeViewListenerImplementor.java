package mono.com.devexpress.dxlistview.swipes;


public class SwipeViewListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxlistview.swipes.SwipeViewListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_swipeItemTap:(IILcom/devexpress/dxlistview/swipes/DXSwipeGroup;)V:GetSwipeItemTap_IILcom_devexpress_dxlistview_swipes_DXSwipeGroup_Handler:DevExpress.Android.CollectionView.Swipes.ISwipeViewListenerInvoker, DXCollectionView.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.CollectionView.Swipes.ISwipeViewListenerImplementor, DXCollectionView.a", SwipeViewListenerImplementor.class, __md_methods);
	}

	public SwipeViewListenerImplementor ()
	{
		super ();
		if (getClass () == SwipeViewListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.CollectionView.Swipes.ISwipeViewListenerImplementor, DXCollectionView.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void swipeItemTap (int p0, int p1, com.devexpress.dxlistview.swipes.DXSwipeGroup p2)
	{
		n_swipeItemTap (p0, p1, p2);
	}

	private native void n_swipeItemTap (int p0, int p1, com.devexpress.dxlistview.swipes.DXSwipeGroup p2);

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
