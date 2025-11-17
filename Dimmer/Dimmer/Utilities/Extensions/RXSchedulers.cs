using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class RxSchedulers
{

    public static readonly IScheduler UI = new MauiUiScheduler();

    public static readonly IScheduler Background = TaskPoolScheduler.Default;
}