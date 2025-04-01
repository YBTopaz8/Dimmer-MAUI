package mono.com.devexpress.dxlistview.swipes;


public class RecycleListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxlistview.swipes.RecycleListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_recycleItem:(Lcom/devexpress/dxlistview/layouts/LayoutElement;)V:GetRecycleItem_Lcom_devexpress_dxlistview_layouts_LayoutElement_Handler:DevExpress.Android.CollectionView.Swipes.IRecycleListenerInvoker, DXCollectionView.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.CollectionView.Swipes.IRecycleListenerImplementor, DXCollectionView.a", RecycleListenerImplementor.class, __md_methods);
	}

	public RecycleListenerImplementor ()
	{
		super ();
		if (getClass () == RecycleListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.CollectionView.Swipes.IRecycleListenerImplementor, DXCollectionView.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void recycleItem (com.devexpress.dxlistview.layouts.LayoutElement p0)
	{
		n_recycleItem (p0);
	}

	private native void n_recycleItem (com.devexpress.dxlistview.layouts.LayoutElement p0);

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
