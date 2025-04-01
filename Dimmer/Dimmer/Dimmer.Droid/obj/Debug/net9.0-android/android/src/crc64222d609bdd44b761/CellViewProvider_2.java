package crc64222d609bdd44b761;


public abstract class CellViewProvider_2
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.CellViewProvider`2, DevExpress.Maui.Editors", CellViewProvider_2.class, __md_methods);
	}

	public CellViewProvider_2 ()
	{
		super ();
		if (getClass () == CellViewProvider_2.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.CellViewProvider`2, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

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
