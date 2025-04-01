package mono.com.devexpress.editors;


public class AutoCompleteEdit_ItemSelectedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.AutoCompleteEdit.ItemSelectedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onItemSelected:(Lcom/devexpress/editors/AutoCompleteEdit;Ljava/lang/Object;)V:GetOnItemSelected_Lcom_devexpress_editors_AutoCompleteEdit_Ljava_lang_Object_Handler:DevExpress.Android.Editors.AutoCompleteEdit/IItemSelectedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.AutoCompleteEdit+IItemSelectedListenerImplementor, DXEditors.a", AutoCompleteEdit_ItemSelectedListenerImplementor.class, __md_methods);
	}

	public AutoCompleteEdit_ItemSelectedListenerImplementor ()
	{
		super ();
		if (getClass () == AutoCompleteEdit_ItemSelectedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.AutoCompleteEdit+IItemSelectedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onItemSelected (com.devexpress.editors.AutoCompleteEdit p0, java.lang.Object p1)
	{
		n_onItemSelected (p0, p1);
	}

	private native void n_onItemSelected (com.devexpress.editors.AutoCompleteEdit p0, java.lang.Object p1);

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
