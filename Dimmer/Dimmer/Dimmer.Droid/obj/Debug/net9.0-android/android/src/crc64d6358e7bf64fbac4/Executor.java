package crc64d6358e7bf64fbac4;


public class Executor
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		java.util.concurrent.Executor
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_execute:(Ljava/lang/Runnable;)V:GetExecute_Ljava_lang_Runnable_Handler:Java.Util.Concurrent.IExecutorInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Media.Executor, CommunityToolkit.Maui.Core", Executor.class, __md_methods);
	}

	public Executor ()
	{
		super ();
		if (getClass () == Executor.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Media.Executor, CommunityToolkit.Maui.Core", "", this, new java.lang.Object[] {  });
		}
	}

	public void execute (java.lang.Runnable p0)
	{
		n_execute (p0);
	}

	private native void n_execute (java.lang.Runnable p0);

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
