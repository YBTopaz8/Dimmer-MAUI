package mono.com.devexpress.navigation.navigationdrawer;


public class DrawerInnerContainer_IOnDrawListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.navigation.navigationdrawer.DrawerInnerContainer.IOnDrawListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_drawChild:(Landroid/graphics/Canvas;Landroid/view/View;J)V:GetDrawChild_Landroid_graphics_Canvas_Landroid_view_View_JHandler:DevExpress.Android.Navigation.Navigationdrawer.DrawerInnerContainer/IOnDrawListenerInvoker, DXNavigation.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Navigation.Navigationdrawer.DrawerInnerContainer+IOnDrawListenerImplementor, DXNavigation.a", DrawerInnerContainer_IOnDrawListenerImplementor.class, __md_methods);
	}

	public DrawerInnerContainer_IOnDrawListenerImplementor ()
	{
		super ();
		if (getClass () == DrawerInnerContainer_IOnDrawListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Navigation.Navigationdrawer.DrawerInnerContainer+IOnDrawListenerImplementor, DXNavigation.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void drawChild (android.graphics.Canvas p0, android.view.View p1, long p2)
	{
		n_drawChild (p0, p1, p2);
	}

	private native void n_drawChild (android.graphics.Canvas p0, android.view.View p1, long p2);

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
