using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class RxSchedulers
{
#if WINDOWS
    public static readonly IScheduler UI = DispatcherScheduler.Current;
#else
    public static readonly IScheduler UI = CurrentThreadScheduler.Instance;
#endif

    public static readonly IScheduler Background = TaskPoolScheduler.Default;
}