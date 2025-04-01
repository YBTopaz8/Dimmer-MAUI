package crc64222d609bdd44b761;


public class IconClickedListener
	extends com.devexpress.editors.OnClickHandledListener
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onHandledClick:(Landroid/view/View;)Z:GetOnHandledClick_Landroid_view_View_Handler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.IconClickedListener, DevExpress.Maui.Editors", IconClickedListener.class, __md_methods);
	}

	public IconClickedListener ()
	{
		super ();
		if (getClass () == IconClickedListener.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.IconClickedListener, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onHandledClick (android.view.View p0)
	{
		return n_onHandledClick (p0);
	}

	private native boolean n_onHandledClick (android.view.View p0);

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
