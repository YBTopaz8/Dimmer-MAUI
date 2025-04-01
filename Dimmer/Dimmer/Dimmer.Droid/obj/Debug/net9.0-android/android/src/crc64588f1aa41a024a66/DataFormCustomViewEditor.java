package crc64588f1aa41a024a66;


public class DataFormCustomViewEditor
	extends crc64588f1aa41a024a66.DataFormAbstractEditorBase
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getView:()Landroid/view/View;:GetGetViewHandler\n" +
			"n_commitEditorValue:()Z:GetCommitEditorValueHandler\n" +
			"n_configure:()V:GetConfigureHandler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataForm.Internal.DataFormCustomViewEditor, DevExpress.Maui.Editors", DataFormCustomViewEditor.class, __md_methods);
	}

	public DataFormCustomViewEditor (android.view.View p0)
	{
		super (p0);
		if (getClass () == DataFormCustomViewEditor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormCustomViewEditor, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public DataFormCustomViewEditor (android.view.View p0, com.devexpress.editors.dataForm.protocols.DXDataFormEditorItemErrorProvider p1)
	{
		super (p0, p1);
		if (getClass () == DataFormCustomViewEditor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormCustomViewEditor, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android:DevExpress.Android.Editors.DataForm.Protocols.DXDataFormEditorItemErrorProvider, DXEditors.a", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public android.view.View getView ()
	{
		return n_getView ();
	}

	private native android.view.View n_getView ();

	public boolean commitEditorValue ()
	{
		return n_commitEditorValue ();
	}

	private native boolean n_commitEditorValue ();

	public void configure ()
	{
		n_configure ();
	}

	private native void n_configure ();

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
