package mono.com.devexpress.editors;


public class AutoCompleteEdit_QuerySubmittedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.AutoCompleteEdit.QuerySubmittedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onQuerySubmitted:(Lcom/devexpress/editors/AutoCompleteEdit;Ljava/lang/CharSequence;)V:GetOnQuerySubmitted_Lcom_devexpress_editors_AutoCompleteEdit_Ljava_lang_CharSequence_Handler:DevExpress.Android.Editors.AutoCompleteEdit/IQuerySubmittedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.AutoCompleteEdit+IQuerySubmittedListenerImplementor, DXEditors.a", AutoCompleteEdit_QuerySubmittedListenerImplementor.class, __md_methods);
	}

	public AutoCompleteEdit_QuerySubmittedListenerImplementor ()
	{
		super ();
		if (getClass () == AutoCompleteEdit_QuerySubmittedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.AutoCompleteEdit+IQuerySubmittedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onQuerySubmitted (com.devexpress.editors.AutoCompleteEdit p0, java.lang.CharSequence p1)
	{
		n_onQuerySubmitted (p0, p1);
	}

	private native void n_onQuerySubmitted (com.devexpress.editors.AutoCompleteEdit p0, java.lang.CharSequence p1);

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
