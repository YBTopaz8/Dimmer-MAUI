package crc644741187cca50a741;


public class CSObjectWrapper
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_toString:()Ljava/lang/String;:GetToStringHandler\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Android.Internal.CSObjectWrapper, DevExpress.Maui.Editors", CSObjectWrapper.class, __md_methods);
	}

	public CSObjectWrapper ()
	{
		super ();
		if (getClass () == CSObjectWrapper.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Android.Internal.CSObjectWrapper, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public java.lang.String toString ()
	{
		return n_toString ();
	}

	private native java.lang.String n_toString ();

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
