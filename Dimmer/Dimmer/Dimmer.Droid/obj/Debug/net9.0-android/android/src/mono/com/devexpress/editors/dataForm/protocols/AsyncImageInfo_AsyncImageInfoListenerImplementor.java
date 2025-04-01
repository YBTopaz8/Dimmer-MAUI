package mono.com.devexpress.editors.dataForm.protocols;


public class AsyncImageInfo_AsyncImageInfoListenerImplementor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.dataForm.protocols.AsyncImageInfo.AsyncImageInfoListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onImageLoaded:()V:GetOnImageLoadedHandler:DevExpress.Android.Editors.DataForm.Protocols.AsyncImageInfo/IAsyncImageInfoListenerInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Android.Editors.DataForm.Protocols.AsyncImageInfo+IAsyncImageInfoListenerImplementor, DXEditors.a", AsyncImageInfo_AsyncImageInfoListenerImplementor.class, __md_methods);
	}

	public AsyncImageInfo_AsyncImageInfoListenerImplementor ()
	{
		super ();
		if (getClass () == AsyncImageInfo_AsyncImageInfoListenerImplementor.class) {
			mono.android.TypeManager.Activate ("DevExpress.Android.Editors.DataForm.Protocols.AsyncImageInfo+IAsyncImageInfoListenerImplementor, DXEditors.a", "", this, new java.lang.Object[] {  });
		}
	}

	public void onImageLoaded ()
	{
		n_onImageLoaded ();
	}

	private native void n_onImageLoaded ();

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
