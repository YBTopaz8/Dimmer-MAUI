package crc646b09977338b7360e;


public class DXBottomSheetDialog
	extends com.google.android.material.bottomsheet.BottomSheetDialog
	implements
		mono.android.IGCUserPeer,
		android.content.DialogInterface.OnShowListener,
		android.content.DialogInterface.OnCancelListener,
		android.content.DialogInterface.OnDismissListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreate:(Landroid/os/Bundle;)V:GetOnCreate_Landroid_os_Bundle_Handler\n" +
			"n_onShow:(Landroid/content/DialogInterface;)V:GetOnShow_Landroid_content_DialogInterface_Handler:Android.Content.IDialogInterfaceOnShowListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onCancel:(Landroid/content/DialogInterface;)V:GetOnCancel_Landroid_content_DialogInterface_Handler:Android.Content.IDialogInterfaceOnCancelListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"n_onDismiss:(Landroid/content/DialogInterface;)V:GetOnDismiss_Landroid_content_DialogInterface_Handler:Android.Content.IDialogInterfaceOnDismissListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Controls.Internal.DXBottomSheetDialog, DevExpress.Maui.Controls", DXBottomSheetDialog.class, __md_methods);
	}

	public DXBottomSheetDialog (android.content.Context p0)
	{
		super (p0);
		if (getClass () == DXBottomSheetDialog.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.DXBottomSheetDialog, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public DXBottomSheetDialog (android.content.Context p0, boolean p1, android.content.DialogInterface.OnCancelListener p2)
	{
		super (p0, p1, p2);
		if (getClass () == DXBottomSheetDialog.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.DXBottomSheetDialog, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android:System.Boolean, System.Private.CoreLib:Android.Content.IDialogInterfaceOnCancelListener, Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public DXBottomSheetDialog (android.content.Context p0, int p1)
	{
		super (p0, p1);
		if (getClass () == DXBottomSheetDialog.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Controls.Internal.DXBottomSheetDialog, DevExpress.Maui.Controls", "Android.Content.Context, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1 });
		}
	}

	public void onCreate (android.os.Bundle p0)
	{
		n_onCreate (p0);
	}

	private native void n_onCreate (android.os.Bundle p0);

	public void onShow (android.content.DialogInterface p0)
	{
		n_onShow (p0);
	}

	private native void n_onShow (android.content.DialogInterface p0);

	public void onCancel (android.content.DialogInterface p0)
	{
		n_onCancel (p0);
	}

	private native void n_onCancel (android.content.DialogInterface p0);

	public void onDismiss (android.content.DialogInterface p0)
	{
		n_onDismiss (p0);
	}

	private native void n_onDismiss (android.content.DialogInterface p0);

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
