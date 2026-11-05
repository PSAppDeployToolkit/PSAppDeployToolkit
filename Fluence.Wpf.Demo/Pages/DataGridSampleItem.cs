/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// A product row for the DataGrid gallery samples, with change notification and validation.
    /// </summary>
    public sealed class DataGridSampleItem : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private static readonly string[] Categories = ["Hardware", "Software", "Services", "Accessories"];
        private static readonly string[] Suppliers = ["Contoso", "Fabrikam", "Northwind", "Tailspin", "Wingtip"];

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// Gets or sets the product name. It must not be blank.
        /// </summary>
        public string Name
        {
            get;
            set
            {
                if (SetField(ref field, value))
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Name)));
                }
            }
        } = string.Empty;

        /// <summary>
        /// Gets or sets the product category used for grouping.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the supplier name.
        /// </summary>
        public string Supplier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity on hand. It must not be negative.
        /// </summary>
        public int Quantity
        {
            get;
            set
            {
                if (SetField(ref field, value))
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Quantity)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the product is in stock.
        /// </summary>
        public bool InStock
        {
            get;
            set => _ = SetField(ref field, value);
        }

        /// <summary>
        /// Gets or sets the customer rating, from 1 to 5.
        /// </summary>
        public double Rating
        {
            get;
            set => _ = SetField(ref field, value);
        }

        /// <summary>
        /// Gets or sets the order fulfilment percentage.
        /// </summary>
        public double Progress
        {
            get;
            set => _ = SetField(ref field, value);
        }

        /// <inheritdoc />
        public bool HasErrors => string.IsNullOrWhiteSpace(Name) || Quantity < 0;

        /// <summary>
        /// Creates a deterministic catalog of <paramref name="count"/> products.
        /// </summary>
        /// <param name="count">The number of products.</param>
        /// <returns>The products.</returns>
        public static ObservableCollection<DataGridSampleItem> CreateCatalog(int count)
        {
            ObservableCollection<DataGridSampleItem> items = [];
            for (int i = 0; i < count; i++)
            {
                items.Add(new DataGridSampleItem
                {
                    Name = "Product " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    Category = Categories[i % Categories.Length],
                    Supplier = Suppliers[i % Suppliers.Length],
                    Quantity = i * 7 % 120,
                    InStock = i % 3 is not 0,
                    Rating = (i % 5) + 1,
                    Progress = i * 13 % 101,
                });
            }

            return items;
        }

        /// <inheritdoc />
        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.Equals(propertyName, nameof(Name), StringComparison.Ordinal) && string.IsNullOrWhiteSpace(Name))
            {
                yield return "Enter a product name.";
            }

            if (string.Equals(propertyName, nameof(Quantity), StringComparison.Ordinal) && Quantity < 0)
            {
                yield return "Quantity cannot be negative.";
            }
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
