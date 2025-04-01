package crc64d6358e7bf64fbac4;


public class ModelDownloadListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.speech.ModelDownloadListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onError:(I)V:GetOnError_IHandler:Android.Speech.IModelDownloadListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onProgress:(I)V:GetOnProgress_IHandler:Android.Speech.IModelDownloadListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onScheduled:()V:GetOnScheduledHandler:Android.Speech.IModelDownloadListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onSuccess:()V:GetOnSuccessHandler:Android.Speech.IModelDownloadListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Media.ModelDownloadListener, CommunityToolkit.Maui.Core", ModelDownloadListener.class, __md_methods);
	}

	public ModelDownloadListener ()
	{
		super ();
		if (getClass () == ModelDownloadListener.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Media.ModelDownloadListener, CommunityToolkit.Maui.Core", "", this, new java.lang.Object[] {  });
		}
	}

	public void onError (int p0)
	{
		n_onError (p0);
	}

	private native void n_onError (int p0);

	public void onProgress (int p0)
	{
		n_onProgress (p0);
	}

	private native void n_onProgress (int p0);

	public void onScheduled ()
	{
		n_onScheduled ();
	}

	private native void n_onScheduled ();

	public void onSuccess ()
	{
		n_onSuccess ();
	}

	private native void n_onSuccess ();

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
