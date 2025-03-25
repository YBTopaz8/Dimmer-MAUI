namespace Dimmer_MAUI.Platforms.Android.CurrentActivity;
public class CrossCurrentActivity
{
    static Lazy<ICurrentActivity> implementation = new Lazy<ICurrentActivity>(() => CreateCurrentActivity(), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Current settings to use
    /// </summary>
    public static ICurrentActivity Current
    {
        get
        {
            ICurrentActivity ret = implementation.Value;
            if (ret == null)
            {
                throw NotImplementedInReferenceAssembly();
            }
            return ret;
        }
    }

    static ICurrentActivity CreateCurrentActivity()
    {
#if NETSTANDARD1_0 || NETSTANDARD2_0
            return null;
#else
        return new CurrentActivityImplementation();
#endif
    }

    internal static Exception NotImplementedInReferenceAssembly()
    {
        return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
    }
}