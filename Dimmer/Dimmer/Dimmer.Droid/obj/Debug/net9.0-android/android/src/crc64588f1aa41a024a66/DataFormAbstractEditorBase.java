package crc64588f1aa41a024a66;


public abstract class DataFormAbstractEditorBase
	extends com.devexpress.editors.dataForm.protocols.DXDataFormEditorItem
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_commitEditorValue:()Z:GetCommitEditorValueHandler\n" +
			"n_validateEditorValue:()Z:GetValidateEditorValueHandler\n" +
			"n_resetEditorValue:()V:GetResetEditorValueHandler\n" +
			"n_getEditorWrappedValue:()Ljava/lang/Object;:GetGetEditorWrappedValueHandler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.DataForm.Internal.DataFormAbstractEditorBase, DevExpress.Maui.Editors", DataFormAbstractEditorBase.class, __md_methods);
	}

	public DataFormAbstractEditorBase (android.view.View p0)
	{
		super (p0);
		if (getClass () == DataFormAbstractEditorBase.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormAbstractEditorBase, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public DataFormAbstractEditorBase (android.view.View p0, com.devexpress.editors.dataForm.protocols.DXDataFormEditorItemErrorProvider p1)
	{
		super (p0, p1);
		if (getClass () == DataFormAbstractEditorBase.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.DataForm.Internal.DataFormAbstractEditorBase, DevExpress.Maui.Editors", "Android.Views.View, Mono.Android:DevExpress.Android.Editors.DataForm.Protocols.DXDataFormEditorItemErrorProvider, DXEditors.a", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public boolean commitEditorValue ()
	{
		return n_commitEditorValue ();
	}

	private native boolean n_commitEditorValue ();

	public boolean validateEditorValue ()
	{
		return n_validateEditorValue ();
	}

	private native boolean n_validateEditorValue ();

	public void resetEditorValue ()
	{
		n_resetEditorValue ();
	}

	private native void n_resetEditorValue ();

	public java.lang.Object getEditorWrappedValue ()
	{
		return n_getEditorWrappedValue ();
	}

	private native java.lang.Object n_getEditorWrappedValue ();

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
