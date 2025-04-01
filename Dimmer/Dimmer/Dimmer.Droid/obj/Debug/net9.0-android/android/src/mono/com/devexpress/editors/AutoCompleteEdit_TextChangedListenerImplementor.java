package mono.com.devexpress.editors;


public class AutoCompleteEdit_TextChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.AutoCompleteEdit.TextChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTextChanged:(Lcom/devexpress/editors/AutoCompleteEdit;Lcom/devexpress/editors/DXAutoCompleteTextChangeReason;)V:GetOnTextChanged_Lcom_devexpress_editors_AutoCompleteEdit_Lcom_devexpress_editors_DXAutoCompleteTextChangeReason_Handler:DevExpress.Android.Editors.AutoCompleteEdit/ITextChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.AutoCompleteEdit+ITextChangedListenerImplementor, DXEditors.a", AutoCompleteEdit_TextChangedListenerImplementor.class, __md_methods);
	}

	public AutoCompleteEdit_TextChangedListenerImplementor ()
	{
		super ();
		if (getClass () == AutoCompleteEdit_TextChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.AutoCompleteEdit+ITextChangedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onTextChanged (com.devexpress.editors.AutoCompleteEdit p0, com.devexpress.editors.DXAutoCompleteTextChangeReason p1)
	{
		n_onTextChanged (p0, p1);
	}

	private native void n_onTextChanged (com.devexpress.editors.AutoCompleteEdit p0, com.devexpress.editors.DXAutoCompleteTextChangeReason p1);

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
