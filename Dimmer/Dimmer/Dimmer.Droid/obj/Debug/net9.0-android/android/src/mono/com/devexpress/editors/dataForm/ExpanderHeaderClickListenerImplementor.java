package mono.com.devexpress.editors.dataForm;


public class ExpanderHeaderClickListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.dataForm.ExpanderHeaderClickListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onHeaderClicked:()V:GetOnHeaderClickedHandler:DevExpress.Android.Editors.DataForm.IExpanderHeaderClickListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.DataForm.IExpanderHeaderClickListenerImplementor, DXEditors.a", ExpanderHeaderClickListenerImplementor.class, __md_methods);
	}

	public ExpanderHeaderClickListenerImplementor ()
	{
		super ();
		if (getClass () == ExpanderHeaderClickListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.DataForm.IExpanderHeaderClickListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onHeaderClicked ()
	{
		n_onHeaderClicked ();
	}

	private native void n_onHeaderClicked ();

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
