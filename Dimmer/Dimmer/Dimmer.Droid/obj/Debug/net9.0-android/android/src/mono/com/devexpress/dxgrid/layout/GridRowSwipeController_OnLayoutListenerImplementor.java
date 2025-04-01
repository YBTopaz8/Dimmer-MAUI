package mono.com.devexpress.dxgrid.layout;


public class GridRowSwipeController_OnLayoutListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.layout.GridRowSwipeController.OnLayoutListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_layoutViews:()V:GetLayoutViewsHandler:DevExpress.Android.Grid.Layout.GridRowSwipeController/IOnLayoutListenerInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Grid.Layout.GridRowSwipeController+IOnLayoutListenerImplementor, DXGrid.a", GridRowSwipeController_OnLayoutListenerImplementor.class, __md_methods);
	}

	public GridRowSwipeController_OnLayoutListenerImplementor ()
	{
		super ();
		if (getClass () == GridRowSwipeController_OnLayoutListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Grid.Layout.GridRowSwipeController+IOnLayoutListenerImplementor, DXGrid.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void layoutViews ()
	{
		n_layoutViews ();
	}

	private native void n_layoutViews ();

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
