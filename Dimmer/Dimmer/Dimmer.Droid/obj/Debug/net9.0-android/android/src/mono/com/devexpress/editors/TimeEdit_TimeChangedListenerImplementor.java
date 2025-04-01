package mono.com.devexpress.editors;


public class TimeEdit_TimeChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.TimeEdit.TimeChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTimeChanged:(Lcom/devexpress/editors/TimeEdit;II)V:GetOnTimeChanged_Lcom_devexpress_editors_TimeEdit_IIHandler:DevExpress.Android.Editors.TimeEdit/ITimeChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.TimeEdit+ITimeChangedListenerImplementor, DXEditors.a", TimeEdit_TimeChangedListenerImplementor.class, __md_methods);
	}

	public TimeEdit_TimeChangedListenerImplementor ()
	{
		super ();
		if (getClass () == TimeEdit_TimeChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.TimeEdit+ITimeChangedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onTimeChanged (com.devexpress.editors.TimeEdit p0, int p1, int p2)
	{
		n_onTimeChanged (p0, p1, p2);
	}

	private native void n_onTimeChanged (com.devexpress.editors.TimeEdit p0, int p1, int p2);

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
