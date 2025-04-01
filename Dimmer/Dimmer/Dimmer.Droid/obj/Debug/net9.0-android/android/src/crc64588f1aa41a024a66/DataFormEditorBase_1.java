package crc64588f1aa41a024a66;


public abstract class DataFormEditorBase_1
	extends crc64588f1aa41a024a66.DataFormAbstractEditorBase
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getView:()Landroid/view/View;:GetGetViewHandler\n" +
			"n_getEdit:()Lcom/devexpress/editors/EditBase;:GetGetEditHandler\n" +
			"n_configure:()V:GetConfigureHandler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataForm.Internal.DataFormEditorBase`1, DevExpress.Maui.Editors", DataFormEditorBase_1.class, __md_methods);
	}

	public DataFormEditorBase_1 (android.view.View p0)
	{
		super (p0);
		if (getClass () == DataFormEditorBase_1.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormEditorBase`1, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public DataFormEditorBase_1 (android.view.View p0, com.devexpress.editors.dataForm.protocols.DXDataFormEditorItemErrorProvider p1)
	{
		super (p0, p1);
		if (getClass () == DataFormEditorBase_1.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormEditorBase`1, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android:DevExpress.Android.Editors.DataForm.Protocols.DXDataFormEditorItemErrorProvider, DXEditors.a", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public android.view.View getView ()
	{
		return n_getView ();
	}

	private native android.view.View n_getView ();

	public com.devexpress.editors.EditBase getEdit ()
	{
		return n_getEdit ();
	}

	private native com.devexpress.editors.EditBase n_getEdit ();

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
