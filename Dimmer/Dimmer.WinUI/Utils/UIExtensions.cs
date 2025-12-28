using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils;

public static class UIExtensions
{
    private static readonly ConditionalWeakTable<MenuFlyoutItem, RoutedEventHandler> HandlerMap = new();

    public static T WithClick<T>(this T item, RoutedEventHandler handler) where T : MenuFlyoutItemBase
    {
        if (item is MenuFlyoutItem menuItem)
        {
            menuItem.Click += handler;
            HandlerMap.Add(menuItem, handler);
        }
        return item;
    }

    public static void RemoveClick(this MenuFlyoutItem item)
    {
        if (HandlerMap.TryGetValue(item, out var handler))
        {
            item.Click -= handler;
            HandlerMap.Remove(item);
        }
    }
    public static T WithPointer<T>(this T item,
    PointerEventHandler handler) where T : MenuFlyoutItemBase
    {
        if (item is MenuFlyoutItem menuItem)
            menuItem.PointerPressed += handler;

        return item;
    }
}