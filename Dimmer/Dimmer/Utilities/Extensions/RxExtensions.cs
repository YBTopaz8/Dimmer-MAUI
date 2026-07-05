namespace Dimmer.Utilities.Extensions;

public static class RxExtensions
{
    // Watches a specific property name and returns the new value
    public static IObservable<TProp> WhenPropertyChanged<TObj, TProp>(this TObj source, string propertyName, Func<TObj, TProp> valueSelector)
        where TObj : INotifyPropertyChanged
    {
        return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                h => source.PropertyChanged += h,
                h => source.PropertyChanged -= h)
            .Where(e => e.EventArgs.PropertyName == propertyName)
            .Select(_ => valueSelector(source))
            .StartWith(valueSelector(source)); // Important: Emit current value immediately!
    }
}