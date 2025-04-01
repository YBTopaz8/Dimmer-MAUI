package crc6436e425876cb621d9;


public class FragmentLifecycleManager
	extends androidx.fragment.app.FragmentManager.FragmentLifecycleCallbacks
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onFragmentAttached:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;Landroid/content/Context;)V:GetOnFragmentAttached_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Landroid_content_Context_Handler\n" +
			"n_onFragmentCreated:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;Landroid/os/Bundle;)V:GetOnFragmentCreated_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Landroid_os_Bundle_Handler\n" +
			"n_onFragmentDestroyed:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentDestroyed_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"n_onFragmentDetached:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentDetached_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"n_onFragmentPaused:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentPaused_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"n_onFragmentPreAttached:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;Landroid/content/Context;)V:GetOnFragmentPreAttached_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Landroid_content_Context_Handler\n" +
			"n_onFragmentPreCreated:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;Landroid/os/Bundle;)V:GetOnFragmentPreCreated_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Landroid_os_Bundle_Handler\n" +
			"n_onFragmentResumed:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentResumed_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"n_onFragmentSaveInstanceState:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;Landroid/os/Bundle;)V:GetOnFragmentSaveInstanceState_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Landroid_os_Bundle_Handler\n" +
			"n_onFragmentStarted:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentStarted_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"n_onFragmentStopped:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentStopped_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"n_onFragmentViewCreated:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;Landroid/view/View;Landroid/os/Bundle;)V:GetOnFragmentViewCreated_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Landroid_view_View_Landroid_os_Bundle_Handler\n" +
			"n_onFragmentViewDestroyed:(Landroidx/fragment/app/FragmentManager;Landroidx/fragment/app/Fragment;)V:GetOnFragmentViewDestroyed_Landroidx_fragment_app_FragmentManager_Landroidx_fragment_app_Fragment_Handler\n" +
			"";
		mono.android.Runtime.register ("CommunityToolkit.Maui.Core.Services.FragmentLifecycleManager, CommunityToolkit.Maui.Core", FragmentLifecycleManager.class, __md_methods);
	}

	public FragmentLifecycleManager ()
	{
		super ();
		if (getClass () == FragmentLifecycleManager.class) {
			mono.android.TypeManager.Activate ("CommunityToolkit.Maui.Core.Services.FragmentLifecycleManager, CommunityToolkit.Maui.Core", "", this, new java.lang.Object[] {  });
		}
	}

	public void onFragmentAttached (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.content.Context p2)
	{
		n_onFragmentAttached (p0, p1, p2);
	}

	private native void n_onFragmentAttached (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.content.Context p2);

	public void onFragmentCreated (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.os.Bundle p2)
	{
		n_onFragmentCreated (p0, p1, p2);
	}

	private native void n_onFragmentCreated (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.os.Bundle p2);

	public void onFragmentDestroyed (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentDestroyed (p0, p1);
	}

	private native void n_onFragmentDestroyed (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

	public void onFragmentDetached (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentDetached (p0, p1);
	}

	private native void n_onFragmentDetached (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

	public void onFragmentPaused (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentPaused (p0, p1);
	}

	private native void n_onFragmentPaused (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

	public void onFragmentPreAttached (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.content.Context p2)
	{
		n_onFragmentPreAttached (p0, p1, p2);
	}

	private native void n_onFragmentPreAttached (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.content.Context p2);

	public void onFragmentPreCreated (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.os.Bundle p2)
	{
		n_onFragmentPreCreated (p0, p1, p2);
	}

	private native void n_onFragmentPreCreated (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.os.Bundle p2);

	public void onFragmentResumed (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentResumed (p0, p1);
	}

	private native void n_onFragmentResumed (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

	public void onFragmentSaveInstanceState (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.os.Bundle p2)
	{
		n_onFragmentSaveInstanceState (p0, p1, p2);
	}

	private native void n_onFragmentSaveInstanceState (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.os.Bundle p2);

	public void onFragmentStarted (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentStarted (p0, p1);
	}

	private native void n_onFragmentStarted (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

	public void onFragmentStopped (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentStopped (p0, p1);
	}

	private native void n_onFragmentStopped (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

	public void onFragmentViewCreated (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.view.View p2, android.os.Bundle p3)
	{
		n_onFragmentViewCreated (p0, p1, p2, p3);
	}

	private native void n_onFragmentViewCreated (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1, android.view.View p2, android.os.Bundle p3);

	public void onFragmentViewDestroyed (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1)
	{
		n_onFragmentViewDestroyed (p0, p1);
	}

	private native void n_onFragmentViewDestroyed (androidx.fragment.app.FragmentManager p0, androidx.fragment.app.Fragment p1);

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
