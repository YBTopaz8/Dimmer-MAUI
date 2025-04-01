package mono.com.devexpress.editors.pickers;


public class DateEditPicker_DateEditPickerListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.pickers.DateEditPicker.DateEditPickerListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onActiveViewChanged:(Lcom/devexpress/editors/pickers/DateEditPicker;I)V:GetOnActiveViewChanged_Lcom_devexpress_editors_pickers_DateEditPicker_IHandler:DevExpress.Android.Editors.Pickers.DateEditPicker/IDateEditPickerListenerInvoker, DXEditors.a\n" +
			"n_onDayCellTap:(Lcom/devexpress/editors/pickers/DateEditPicker;III)V:GetOnDayCellTap_Lcom_devexpress_editors_pickers_DateEditPicker_IIIHandler:DevExpress.Android.Editors.Pickers.DateEditPicker/IDateEditPickerListenerInvoker, DXEditors.a\n" +
			"n_onDisplayYearMonthChanged:(Lcom/devexpress/editors/pickers/DateEditPicker;II)V:GetOnDisplayYearMonthChanged_Lcom_devexpress_editors_pickers_DateEditPicker_IIHandler:DevExpress.Android.Editors.Pickers.DateEditPicker/IDateEditPickerListenerInvoker, DXEditors.a\n" +
			"n_onSizeChanged:()V:GetOnSizeChangedHandler:DevExpress.Android.Editors.Pickers.DateEditPicker/IDateEditPickerListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.Pickers.DateEditPicker+IDateEditPickerListenerImplementor, DXEditors.a", DateEditPicker_DateEditPickerListenerImplementor.class, __md_methods);
	}

	public DateEditPicker_DateEditPickerListenerImplementor ()
	{
		super ();
		if (getClass () == DateEditPicker_DateEditPickerListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.Pickers.DateEditPicker+IDateEditPickerListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onActiveViewChanged (com.devexpress.editors.pickers.DateEditPicker p0, int p1)
	{
		n_onActiveViewChanged (p0, p1);
	}

	private native void n_onActiveViewChanged (com.devexpress.editors.pickers.DateEditPicker p0, int p1);

	public void onDayCellTap (com.devexpress.editors.pickers.DateEditPicker p0, int p1, int p2, int p3)
	{
		n_onDayCellTap (p0, p1, p2, p3);
	}

	private native void n_onDayCellTap (com.devexpress.editors.pickers.DateEditPicker p0, int p1, int p2, int p3);

	public void onDisplayYearMonthChanged (com.devexpress.editors.pickers.DateEditPicker p0, int p1, int p2)
	{
		n_onDisplayYearMonthChanged (p0, p1, p2);
	}

	private native void n_onDisplayYearMonthChanged (com.devexpress.editors.pickers.DateEditPicker p0, int p1, int p2);

	public void onSizeChanged ()
	{
		n_onSizeChanged ();
	}

	private native void n_onSizeChanged ();

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
