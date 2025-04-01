package crc64222d609bdd44b761;


public class TextEditBaseHandler_2_OnTextChangedListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.OnTextChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTextChanged:(Lcom/devexpress/editors/TextEditBase;Ljava/lang/CharSequence;)V:GetOnTextChanged_Lcom_devexpress_editors_TextEditBase_Ljava_lang_CharSequence_Handler:DevExpress.Android.Editors.IOnTextChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.TextEditBaseHandler`2+OnTextChangedListener, DevExpress.Maui.Editors", TextEditBaseHandler_2_OnTextChangedListener.class, __md_methods);
	}

	public TextEditBaseHandler_2_OnTextChangedListener ()
	{
		super ();
		if (getClass () == TextEditBaseHandler_2_OnTextChangedListener.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.TextEditBaseHandler`2+OnTextChangedListener, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public void onTextChanged (com.devexpress.editors.TextEditBase p0, java.lang.CharSequence p1)
	{
		n_onTextChanged (p0, p1);
	}

	private native void n_onTextChanged (com.devexpress.editors.TextEditBase p0, java.lang.CharSequence p1);

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
