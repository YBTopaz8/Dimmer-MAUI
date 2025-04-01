package crc644741187cca50a741;


public class DateFormatter
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.DateFormatter
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_format:(III)Ljava/lang/CharSequence;:GetFormat_IIIHandler:DevExpress.Android.Editors.IDateFormatterInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Android.Internal.DateFormatter, DevExpress.Maui.Editors", DateFormatter.class, __md_methods);
	}

	public DateFormatter ()
	{
		super ();
		if (getClass () == DateFormatter.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Android.Internal.DateFormatter, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public java.lang.CharSequence format (int p0, int p1, int p2)
	{
		return n_format (p0, p1, p2);
	}

	private native java.lang.CharSequence n_format (int p0, int p1, int p2);

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
