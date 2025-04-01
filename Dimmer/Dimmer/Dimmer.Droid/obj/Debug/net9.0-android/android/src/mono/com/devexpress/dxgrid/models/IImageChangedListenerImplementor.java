package mono.com.devexpress.dxgrid.models;


public class IImageChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.dxgrid.models.IImageChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onImageChanged:()V:GetOnImageChangedHandler:DevExpress.Android.Grid.Models.IImageChangedListenerInvoker, DXGrid.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Grid.Models.IImageChangedListenerImplementor, DXGrid.a", IImageChangedListenerImplementor.class, __md_methods);
	}

	public IImageChangedListenerImplementor ()
	{
		super ();
		if (getClass () == IImageChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Grid.Models.IImageChangedListenerImplementor, DXGrid.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onImageChanged ()
	{
		n_onImageChanged ();
	}

	private native void n_onImageChanged ();

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
