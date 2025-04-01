package crc64222d609bdd44b761;


public class ComboBoxContentContainerController
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.ContentContainerControllerListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_isOpened:()Z:GetIsOpenedHandler:DevExpress.Android.Editors.IContentContainerControllerListenerInvoker, DXEditors.a\n" +
			"n_close:(Z)Z:GetClose_ZHandler:DevExpress.Android.Editors.IContentContainerControllerListenerInvoker, DXEditors.a\n" +
			"n_show:()V:GetShowHandler:DevExpress.Android.Editors.IContentContainerControllerListenerInvoker, DXEditors.a\n" +
			"n_update:()V:GetUpdateHandler:DevExpress.Android.Editors.IContentContainerControllerListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.ComboBoxContentContainerController, DevExpress.Maui.Editors", ComboBoxContentContainerController.class, __md_methods);
	}

	public ComboBoxContentContainerController ()
	{
		super ();
		if (getClass () == ComboBoxContentContainerController.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.ComboBoxContentContainerController, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean isOpened ()
	{
		return n_isOpened ();
	}

	private native boolean n_isOpened ();

	public boolean close (boolean p0)
	{
		return n_close (p0);
	}

	private native boolean n_close (boolean p0);

	public void show ()
	{
		n_show ();
	}

	private native void n_show ();

	public void update ()
	{
		n_update ();
	}

	private native void n_update ();

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
