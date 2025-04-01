package mono.com.devexpress.editors;


public class ExpanderListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.ExpanderListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_contentSizeChanged:(Landroid/view/View;)V:GetContentSizeChanged_Landroid_view_View_Handler:DevExpress.Android.Editors.IExpanderListenerInvoker, DXEditors.a\n" +
			"n_isExpanderCollapsed:(Lcom/devexpress/editors/dataForm/ExpanderView;Z)V:GetIsExpanderCollapsed_Lcom_devexpress_editors_dataForm_ExpanderView_ZHandler:DevExpress.Android.Editors.IExpanderListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.IExpanderListenerImplementor, DXEditors.a", ExpanderListenerImplementor.class, __md_methods);
	}

	public ExpanderListenerImplementor ()
	{
		super ();
		if (getClass () == ExpanderListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.IExpanderListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void contentSizeChanged (android.view.View p0)
	{
		n_contentSizeChanged (p0);
	}

	private native void n_contentSizeChanged (android.view.View p0);

	public void isExpanderCollapsed (com.devexpress.editors.dataForm.ExpanderView p0, boolean p1)
	{
		n_isExpanderCollapsed (p0, p1);
	}

	private native void n_isExpanderCollapsed (com.devexpress.editors.dataForm.ExpanderView p0, boolean p1);

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
