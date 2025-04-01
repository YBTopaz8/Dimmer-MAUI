package mono.com.devexpress.editors;


public class SelectionChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.SelectionChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onSelectionChanged:(II)V:GetOnSelectionChanged_IIHandler:DevExpress.Android.Editors.ISelectionChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.ISelectionChangedListenerImplementor, DXEditors.a", SelectionChangedListenerImplementor.class, __md_methods);
	}

	public SelectionChangedListenerImplementor ()
	{
		super ();
		if (getClass () == SelectionChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.ISelectionChangedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onSelectionChanged (int p0, int p1)
	{
		n_onSelectionChanged (p0, p1);
	}

	private native void n_onSelectionChanged (int p0, int p1);

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
