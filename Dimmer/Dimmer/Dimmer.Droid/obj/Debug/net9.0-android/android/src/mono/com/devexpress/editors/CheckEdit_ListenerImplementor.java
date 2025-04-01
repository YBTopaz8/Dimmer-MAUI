package mono.com.devexpress.editors;


public class CheckEdit_ListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.CheckEdit.Listener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onValueChanged:(Lcom/devexpress/editors/DXCheckEditValue;)V:GetOnValueChanged_Lcom_devexpress_editors_DXCheckEditValue_Handler:DevExpress.Android.Editors.CheckEdit/IListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.CheckEdit+IListenerImplementor, DXEditors.a", CheckEdit_ListenerImplementor.class, __md_methods);
	}

	public CheckEdit_ListenerImplementor ()
	{
		super ();
		if (getClass () == CheckEdit_ListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.CheckEdit+IListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onValueChanged (com.devexpress.editors.DXCheckEditValue p0)
	{
		n_onValueChanged (p0);
	}

	private native void n_onValueChanged (com.devexpress.editors.DXCheckEditValue p0);

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
