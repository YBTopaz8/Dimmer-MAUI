package mono.com.devexpress.editors;


public class DateEdit_DateChangedListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.DateEdit.DateChangedListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onDateChanged:(Lcom/devexpress/editors/DateEdit;III)V:GetOnDateChanged_Lcom_devexpress_editors_DateEdit_IIIHandler:DevExpress.Android.Editors.DateEdit/IDateChangedListenerInvoker, DXEditors.a\n" +
			"n_onMaxDateChanged:(Lcom/devexpress/editors/DateEdit;III)V:GetOnMaxDateChanged_Lcom_devexpress_editors_DateEdit_IIIHandler:DevExpress.Android.Editors.DateEdit/IDateChangedListenerInvoker, DXEditors.a\n" +
			"n_onMinDateChanged:(Lcom/devexpress/editors/DateEdit;III)V:GetOnMinDateChanged_Lcom_devexpress_editors_DateEdit_IIIHandler:DevExpress.Android.Editors.DateEdit/IDateChangedListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.DateEdit+IDateChangedListenerImplementor, DXEditors.a", DateEdit_DateChangedListenerImplementor.class, __md_methods);
	}

	public DateEdit_DateChangedListenerImplementor ()
	{
		super ();
		if (getClass () == DateEdit_DateChangedListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.DateEdit+IDateChangedListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onDateChanged (com.devexpress.editors.DateEdit p0, int p1, int p2, int p3)
	{
		n_onDateChanged (p0, p1, p2, p3);
	}

	private native void n_onDateChanged (com.devexpress.editors.DateEdit p0, int p1, int p2, int p3);

	public void onMaxDateChanged (com.devexpress.editors.DateEdit p0, int p1, int p2, int p3)
	{
		n_onMaxDateChanged (p0, p1, p2, p3);
	}

	private native void n_onMaxDateChanged (com.devexpress.editors.DateEdit p0, int p1, int p2, int p3);

	public void onMinDateChanged (com.devexpress.editors.DateEdit p0, int p1, int p2, int p3)
	{
		n_onMinDateChanged (p0, p1, p2, p3);
	}

	private native void n_onMinDateChanged (com.devexpress.editors.DateEdit p0, int p1, int p2, int p3);

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
