﻿using CommunityToolkit.Maui.Converters;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;

[AcceptEmptyServiceProvider]
public partial class IsEnumerableNullOrEmptyConverter : BaseConverterOneWay<IEnumerable?, bool>
{
    /// <inheritdoc/>
    public override bool DefaultConvertReturnValue { get; set; } = false;

    /// <summary>
    /// Converts the incoming value to a <see cref="bool"/> indicating whether the value is null or empty.
    /// </summary>
    /// <param name="value">IEnumerable to convert</param>
    /// <param name="culture">(Not Used)</param>
    /// <returns>A <see cref="bool"/> indicating if the incoming value is null or empty.</returns>
    public override bool ConvertFrom(IEnumerable? value, CultureInfo? culture = null) => IsListNullOrEmpty(value);

    internal static bool IsListNullOrEmpty(IEnumerable? value)
    {
        if (value is null)
        {
            return true;
        }

        var enumerator = value.GetEnumerator();
        bool result = !enumerator.MoveNext();

        if (enumerator is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return result;
    }
}