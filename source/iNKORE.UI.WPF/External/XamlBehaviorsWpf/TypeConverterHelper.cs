// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System.ComponentModel;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    internal static class TypeConverterHelper
    {
        internal static object DoConversionFrom(TypeConverter converter, object value)
        {
            object returnValue = value;

            try
            {
                if (converter != null && value != null && converter.CanConvertFrom(value.GetType()))
                {
                    // This utility class is used to convert value that come from XAML, so we should use the invariant culture.
                    returnValue = converter.ConvertFrom(context: null, culture: CultureInfo.InvariantCulture, value: value);
                }
            }
            catch (Exception e)
            {
                if (!TypeConverterHelper.ShouldEatException(e))
                {
                    throw;
                }
            }

            return returnValue;
        }

        private static bool ShouldEatException(Exception e)
        {
            bool shouldEat = false;

            if (e.InnerException != null)
            {
                shouldEat |= ShouldEatException(e.InnerException);
            }

            shouldEat |= e is FormatException;
            return shouldEat;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Activator.CreateInstance could be calling user code which we don't want to bring us down.")]
        internal static TypeConverter GetTypeConverter(Type type)
        {
            return TypeDescriptor.GetConverter(type);
        }
    }
}
