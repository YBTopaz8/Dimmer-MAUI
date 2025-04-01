package mono.com.devexpress.editors;


public class DateEditPickerListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.DateEditPickerListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_dismiss:()V:GetDismissHandler:DevExpress.Android.Editors.IDateEditPickerListenerInvoker, DXEditors.a\n" +
			"n_show:()V:GetShowHandler:DevExpress.Android.Editors.IDateEditPickerListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.IDateEditPickerListenerImplementor, DXEditors.a", DateEditPickerListenerImplementor.class, __md_methods);
	}

	public DateEditPickerListenerImplementor ()
	{
		super ();
		if (getClass () == DateEditPickerListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.IDateEditPickerListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void dismiss ()
	{
		n_dismiss ();
	}

	private native void n_dismiss ();

	public void show ()
	{
		n_show ();
	}

	private native void n_show ();

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
