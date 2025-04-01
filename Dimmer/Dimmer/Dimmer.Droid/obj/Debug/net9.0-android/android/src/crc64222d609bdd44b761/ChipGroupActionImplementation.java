package crc64222d609bdd44b761;


public class ChipGroupActionImplementation
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.devexpress.editors.ChipGroupAction
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onLayoutChanged:()V:GetOnLayoutChangedHandler:DevExpress.Android.Editors.IChipGroupActionInvoker, DXEditors.a\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Internal.ChipGroupActionImplementation, DevExpress.Maui.Editors", ChipGroupActionImplementation.class, __md_methods);
	}

	public ChipGroupActionImplementation ()
	{
		super ();
		if (getClass () == ChipGroupActionImplementation.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Internal.ChipGroupActionImplementation, DevExpress.Maui.Editors", "", this, new java.lang.Object[] {  });
		}
	}

	public void onLayoutChanged ()
	{
		n_onLayoutChanged ();
	}

	private native void n_onLayoutChanged ();

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
