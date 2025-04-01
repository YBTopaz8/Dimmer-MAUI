package crc64588f1aa41a024a66;


public class DataFormMultilineEditor
	extends crc64588f1aa41a024a66.DataFormTextEditorBase_2
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataForm.Internal.DataFormMultilineEditor, DevExpress.Maui.Editors", DataFormMultilineEditor.class, __md_methods);
	}

	public DataFormMultilineEditor (android.view.View p0)
	{
		super (p0);
		if (getClass () == DataFormMultilineEditor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormMultilineEditor, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public DataFormMultilineEditor (android.view.View p0, com.devexpress.editors.dataForm.protocols.DXDataFormEditorItemErrorProvider p1)
	{
		super (p0, p1);
		if (getClass () == DataFormMultilineEditor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormMultilineEditor, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android:DevExpress.Android.Editors.DataForm.Protocols.DXDataFormEditorItemErrorProvider, DXEditors.a", this, new java.lang.Object[] { p0, p1 });
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
