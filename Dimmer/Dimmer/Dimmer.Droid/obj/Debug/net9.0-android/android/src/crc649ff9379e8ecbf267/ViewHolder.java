package crc649ff9379e8ecbf267;


public class ViewHolder
	extends androidx.recyclerview.widget.RecyclerView.ViewHolder
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Syncfusion.Maui.Toolkit.Carousel.ViewHolder, Syncfusion.Maui.Toolkit", ViewHolder.class, __md_methods);
	}

	public ViewHolder (android.view.View p0)
	{
		super (p0);
		if (getClass () == ViewHolder.class) {
			mono.android.TypeManager.Activate ("Syncfusion.Maui.Toolkit.Carousel.ViewHolder, Syncfusion.Maui.Toolkit", "Android.Views.View, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

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
