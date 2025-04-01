package crc64222d609bdd44b761;


public class EditBaseHandler_2_OnEditActionListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.OnEditActionListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onEditorAction:(I)Z:GetOnEditorAction_IHandler:DevExpress.Android.Editors.IOnEditActionListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.EditBaseHandler`2+OnEditActionListener, DevExpress.Maui.Editors", EditBaseHandler_2_OnEditActionListener.class, __md_methods);
	}

	public EditBaseHandler_2_OnEditActionListener ()
	{
		super ();
		if (getClass () == EditBaseHandler_2_OnEditActionListener.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.EditBaseHandler`2+OnEditActionListener, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onEditorAction (int p0)
	{
		return n_onEditorAction (p0);
	}

	private native boolean n_onEditorAction (int p0);

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
