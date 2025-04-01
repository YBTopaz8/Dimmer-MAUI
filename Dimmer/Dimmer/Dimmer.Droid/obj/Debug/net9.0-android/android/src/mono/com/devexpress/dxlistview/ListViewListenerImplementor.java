package mono.com.devexpress.dxlistview;


public class ListViewListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxlistview.ListViewListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_canDrop:(II)Z:GetCanDrop_IIHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_canLoadMore:()Z:GetCanLoadMoreHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_canPullToRefresh:()Z:GetCanPullToRefreshHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_canStartDrag:(I)Z:GetCanStartDrag_IHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_drop:(II)V:GetDrop_IIHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_itemDoubleTap:(I)V:GetItemDoubleTap_IHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_itemLongPress:(I)V:GetItemLongPress_IHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_itemPressed:(Landroid/view/View;FF)V:GetItemPressed_Landroid_view_View_FFHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_itemReleased:(Landroid/view/View;FF)V:GetItemReleased_Landroid_view_View_FFHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_itemTap:(I)V:GetItemTap_IHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_itemTapConfirmed:(I)V:GetItemTapConfirmed_IHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_loadMore:()V:GetLoadMoreHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_pullToRefresh:()V:GetPullToRefreshHandler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"n_scrolled:(Lcom/devexpress/dxlistview/ListViewScrolledEventArgs;)V:GetScrolled_Lcom_devexpress_dxlistview_ListViewScrolledEventArgs_Handler:DevExpress.Android.CollectionView.IListViewListenerInvoker, DXCollectionView.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.CollectionView.IListViewListenerImplementor, DXCollectionView.a", ListViewListenerImplementor.class, __md_methods);
	}

	public ListViewListenerImplementor ()
	{
		super ();
		if (getClass () == ListViewListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.CollectionView.IListViewListenerImplementor, DXCollectionView.a", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean canDrop (int p0, int p1)
	{
		return n_canDrop (p0, p1);
	}

	private native boolean n_canDrop (int p0, int p1);

	public boolean canLoadMore ()
	{
		return n_canLoadMore ();
	}

	private native boolean n_canLoadMore ();

	public boolean canPullToRefresh ()
	{
		return n_canPullToRefresh ();
	}

	private native boolean n_canPullToRefresh ();

	public boolean canStartDrag (int p0)
	{
		return n_canStartDrag (p0);
	}

	private native boolean n_canStartDrag (int p0);

	public void drop (int p0, int p1)
	{
		n_drop (p0, p1);
	}

	private native void n_drop (int p0, int p1);

	public void itemDoubleTap (int p0)
	{
		n_itemDoubleTap (p0);
	}

	private native void n_itemDoubleTap (int p0);

	public void itemLongPress (int p0)
	{
		n_itemLongPress (p0);
	}

	private native void n_itemLongPress (int p0);

	public void itemPressed (android.view.View p0, float p1, float p2)
	{
		n_itemPressed (p0, p1, p2);
	}

	private native void n_itemPressed (android.view.View p0, float p1, float p2);

	public void itemReleased (android.view.View p0, float p1, float p2)
	{
		n_itemReleased (p0, p1, p2);
	}

	private native void n_itemReleased (android.view.View p0, float p1, float p2);

	public void itemTap (int p0)
	{
		n_itemTap (p0);
	}

	private native void n_itemTap (int p0);

	public void itemTapConfirmed (int p0)
	{
		n_itemTapConfirmed (p0);
	}

	private native void n_itemTapConfirmed (int p0);

	public void loadMore ()
	{
		n_loadMore ();
	}

	private native void n_loadMore ();

	public void pullToRefresh ()
	{
		n_pullToRefresh ();
	}

	private native void n_pullToRefresh ();

	public void scrolled (com.devexpress.dxlistview.ListViewScrolledEventArgs p0)
	{
		n_scrolled (p0);
	}

	private native void n_scrolled (com.devexpress.dxlistview.ListViewScrolledEventArgs p0);

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
