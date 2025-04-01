package crc64222d609bdd44b761;


public class AdvancedDatePickerListener
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
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.AdvancedDatePickerListener, DevExpress.Maui.Editors", AdvancedDatePickerListener.class, __md_methods);
	}

	public AdvancedDatePickerListener ()
	{
		super ();
		if (getClass () == AdvancedDatePickerListener.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.AdvancedDatePickerListener, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
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
