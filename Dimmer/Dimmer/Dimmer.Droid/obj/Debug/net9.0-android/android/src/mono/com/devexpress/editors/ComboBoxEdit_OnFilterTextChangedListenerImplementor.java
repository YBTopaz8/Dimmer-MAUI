package mono.com.devexpress.editors;


public class ComboBoxEdit_OnFilterTextChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.ComboBoxEdit.OnFilterTextChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onFilterTextChanged:(Lcom/devexpress/editors/ComboBoxEdit;Ljava/lang/CharSequence;)V:GetOnFilterTextChanged_Lcom_devexpress_editors_ComboBoxEdit_Ljava_lang_CharSequence_Handler:DevExpress.Android.Editors.ComboBoxEdit/IOnFilterTextChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.ComboBoxEdit+IOnFilterTextChangedListenerImplementor, DXEditors.a", ComboBoxEdit_OnFilterTextChangedListenerImplementor.class, __md_methods);
	}

	public ComboBoxEdit_OnFilterTextChangedListenerImplementor ()
	{
		super ();
		if (getClass () == ComboBoxEdit_OnFilterTextChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.ComboBoxEdit+IOnFilterTextChangedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onFilterTextChanged (com.devexpress.editors.ComboBoxEdit p0, java.lang.CharSequence p1)
	{
		n_onFilterTextChanged (p0, p1);
	}

	private native void n_onFilterTextChanged (com.devexpress.editors.ComboBoxEdit p0, java.lang.CharSequence p1);

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
