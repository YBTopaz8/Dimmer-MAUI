package crc644741187cca50a741;


public class AutoCompleteItemsSourceAdapter
	extends com.devexpress.editors.SimpleComboBoxAdapter
	implements
		mono.android.IGCUserPeer,
		android.widget.Filterable
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_getCount:()I:GetGetCountHandler\n" +
			"n_getPosition:(Ljava/lang/Object;)I:GetGetPosition_Ljava_lang_Object_Handler\n" +
			"n_getSourcePosition:(Ljava/lang/Object;)I:GetGetSourcePosition_Ljava_lang_Object_Handler\n" +
			"n_getItem:(I)Ljava/lang/Object;:GetGetItem_IHandler\n" +
			"n_getSourceItem:(I)Ljava/lang/Object;:GetGetSourceItem_IHandler\n" +
			"n_getText:(I)Ljava/lang/CharSequence;:GetGetText_IHandler\n" +
			"n_getFilter:()Landroid/widget/Filter;:GetGetFilterHandler:Android.Widget.IFilterableInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("DevExpress.Maui.Editors.Android.Internal.AutoCompleteItemsSourceAdapter, DevExpress.Maui.Editors", AutoCompleteItemsSourceAdapter.class, __md_methods);
	}

	public AutoCompleteItemsSourceAdapter (android.content.Context p0)
	{
		super (p0);
		if (getClass () == AutoCompleteItemsSourceAdapter.class) {
			mono.android.TypeManager.Activate ("DevExpress.Maui.Editors.Android.Internal.AutoCompleteItemsSourceAdapter, DevExpress.Maui.Editors", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}

	public int getCount ()
	{
		return n_getCount ();
	}

	private native int n_getCount ();

	public int getPosition (java.lang.Object p0)
	{
		return n_getPosition (p0);
	}

	private native int n_getPosition (java.lang.Object p0);

	public int getSourcePosition (java.lang.Object p0)
	{
		return n_getSourcePosition (p0);
	}

	private native int n_getSourcePosition (java.lang.Object p0);

	public java.lang.Object getItem (int p0)
	{
		return n_getItem (p0);
	}

	private native java.lang.Object n_getItem (int p0);

	public java.lang.Object getSourceItem (int p0)
	{
		return n_getSourceItem (p0);
	}

	private native java.lang.Object n_getSourceItem (int p0);

	public java.lang.CharSequence getText (int p0)
	{
		return n_getText (p0);
	}

	private native java.lang.CharSequence n_getText (int p0);

	public android.widget.Filter getFilter ()
	{
		return n_getFilter ();
	}

	private native android.widget.Filter n_getFilter ();

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
