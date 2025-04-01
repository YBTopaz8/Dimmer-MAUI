package mono.com.devexpress.editors;


public class NumericEdit_UpDownClickListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.NumericEdit.UpDownClickListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_clearClicked:()V:GetClearClickedHandler:DevExpress.Android.Editors.NumericEdit/IUpDownClickListenerInvoker, DXEditors.a\n" +
			"n_downClicked:()V:GetDownClickedHandler:DevExpress.Android.Editors.NumericEdit/IUpDownClickListenerInvoker, DXEditors.a\n" +
			"n_upClicked:()V:GetUpClickedHandler:DevExpress.Android.Editors.NumericEdit/IUpDownClickListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.NumericEdit+IUpDownClickListenerImplementor, DXEditors.a", NumericEdit_UpDownClickListenerImplementor.class, __md_methods);
	}

	public NumericEdit_UpDownClickListenerImplementor ()
	{
		super ();
		if (getClass () == NumericEdit_UpDownClickListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.NumericEdit+IUpDownClickListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void clearClicked ()
	{
		n_clearClicked ();
	}

	private native void n_clearClicked ();

	public void downClicked ()
	{
		n_downClicked ();
	}

	private native void n_downClicked ();

	public void upClicked ()
	{
		n_upClicked ();
	}

	private native void n_upClicked ();

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
