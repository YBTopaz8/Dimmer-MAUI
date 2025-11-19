

using Android.Runtime;

namespace Dimmer.ViewsAndPages.NativeViews.DemoCS;

public class MusicPlayerLibraryDemoFragment : Fragment, IMenuItemOnMenuItemClickListener, IParcelable
{
    private static int GRID_SPAN_COUNT = 2;
    private static int ALBUM_RECYCLER_VIEW_ID = View.GenerateViewId();
    private FrameLayout listContainer;

    private bool listTypeGrid = true;
    private bool listSorted = true;

    public int DescribeContents()
    {
        throw new NotImplementedException();
    }

    public bool OnMenuItemClick(IMenuItem item)
    {
        throw new NotImplementedException();
    }

    public void WriteToParcel(Parcel dest, [GeneratedEnum] ParcelableWriteFlags flags)
    {
        throw new NotImplementedException();
    }


    private int GetDemoLayoutResId()
    {

        // need to to 
        throw new NotImplementedException();
    }
}
