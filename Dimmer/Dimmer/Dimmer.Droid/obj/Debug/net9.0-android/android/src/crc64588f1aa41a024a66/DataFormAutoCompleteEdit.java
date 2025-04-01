package crc64588f1aa41a024a66;


public class DataFormAutoCompleteEdit
	extends crc64588f1aa41a024a66.DataFormEditorBase_1
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataForm.Internal.DataFormAutoCompleteEdit, DevExpress.Maui.Editors", DataFormAutoCompleteEdit.class, __md_methods);
	}

	public DataFormAutoCompleteEdit (android.view.View p0)
	{
		super (p0);
		if (getClass () == DataFormAutoCompleteEdit.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormAutoCompleteEdit, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public DataFormAutoCompleteEdit (android.view.View p0, com.devexpress.editors.dataForm.protocols.DXDataFormEditorItemErrorProvider p1)
	{
		super (p0, p1);
		if (getClass () == DataFormAutoCompleteEdit.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormAutoCompleteEdit, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android:DevExpress.Android.Editors.DataForm.Protocols.DXDataFormEditorItemErrorProvider, DXEditors.a", this, new java.lang.Object[] { p0, p1 });
		}
	}

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
