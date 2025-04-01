package mono.com.devexpress.editors;


public class DropDownStateChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.DropDownStateChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onDropDownStateChanged:(Lcom/devexpress/editors/EditBase;Z)V:GetOnDropDownStateChanged_Lcom_devexpress_editors_EditBase_ZHandler:DevExpress.Android.Editors.IDropDownStateChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.IDropDownStateChangedListenerImplementor, DXEditors.a", DropDownStateChangedListenerImplementor.class, __md_methods);
	}

	public DropDownStateChangedListenerImplementor ()
	{
		super ();
		if (getClass () == DropDownStateChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.IDropDownStateChangedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onDropDownStateChanged (com.devexpress.editors.EditBase p0, boolean p1)
	{
		n_onDropDownStateChanged (p0, p1);
	}

	private native void n_onDropDownStateChanged (com.devexpress.editors.EditBase p0, boolean p1);

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
