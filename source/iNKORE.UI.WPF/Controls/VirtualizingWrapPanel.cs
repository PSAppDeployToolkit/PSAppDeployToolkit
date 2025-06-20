using iNKORE._Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using iNKORE.UI.WPF.Helpers;

namespace iNKORE.UI.WPF.Controls
{
    public enum ScrollDirection
    {
        Vertical,
        Horizontal
    }
    /// <summary>
    /// Base class for panels which are supporting virtualization.
    /// </summary>
    public abstract class VirtualizingPanelBase : VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty ScrollLineDeltaProperty = DependencyProperty.Register(nameof(ScrollLineDelta), typeof(double), typeof(VirtualizingPanelBase), new FrameworkPropertyMetadata(16.0));
        public static readonly DependencyProperty MouseWheelDeltaProperty = DependencyProperty.Register(nameof(MouseWheelDelta), typeof(double), typeof(VirtualizingPanelBase), new FrameworkPropertyMetadata(48.0));
        public static readonly DependencyProperty ScrollLineDeltaItemProperty = DependencyProperty.Register(nameof(ScrollLineDeltaItem), typeof(int), typeof(VirtualizingPanelBase), new FrameworkPropertyMetadata(1));
        public static readonly DependencyProperty MouseWheelDeltaItemProperty = DependencyProperty.Register(nameof(MouseWheelDeltaItem), typeof(int), typeof(VirtualizingPanelBase), new FrameworkPropertyMetadata(3));

        public ScrollViewer? ScrollOwner { get; set; }

        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }

        /// <summary>
        /// Scroll line delta for pixel based scrolling. The default value is 16 dp.
        /// </summary>
        public double ScrollLineDelta { get => (double)GetValue(ScrollLineDeltaProperty); set => SetValue(ScrollLineDeltaProperty, value); }

        /// <summary>
        /// Mouse wheel delta for pixel based scrolling. The default value is 48 dp.
        /// </summary>        
        public double MouseWheelDelta { get => (double)GetValue(MouseWheelDeltaProperty); set => SetValue(MouseWheelDeltaProperty, value); }

        /// <summary>
        /// Scroll line delta for item based scrolling. The default value is 1 item.
        /// </summary>
        public int ScrollLineDeltaItem { get => (int)GetValue(ScrollLineDeltaItemProperty); set => SetValue(ScrollLineDeltaItemProperty, value); }

        /// <summary>
        /// Mouse wheel delta for item based scrolling. The default value is 3 items.
        /// </summary> 
        public int MouseWheelDeltaItem { get => (int)GetValue(MouseWheelDeltaItemProperty); set => SetValue(MouseWheelDeltaItemProperty, value); }

        protected ScrollUnit ScrollUnit => GetScrollUnit(ItemsControl);

        /// <summary>
        /// The direction in which the panel scrolls when user turns the mouse wheel.
        /// </summary>
        protected ScrollDirection MouseWheelScrollDirection { get; set; } = ScrollDirection.Vertical;

        protected bool IsVirtualizing => GetIsVirtualizing(ItemsControl);

        protected VirtualizationMode VirtualizationMode => GetVirtualizationMode(ItemsControl);

        /// <summary>
        /// Returns true if the panel is in VirtualizationMode.Recycling, otherwise false.
        /// </summary>
        protected bool IsRecycling => VirtualizationMode == VirtualizationMode.Recycling;

        /// <summary>
        /// The ItemsControl (e.g. ListView).
        /// </summary>
        public ItemsControl ItemsControl => ItemsControl.GetItemsOwner(this);

        /// <summary>
        /// The ItemsControl (e.g. ListView) or if the ItemsControl is grouping a GroupItem.
        /// </summary>
        protected DependencyObject ItemsOwner
        {
            get
            {
                if (_itemsOwner is null)
                {
                    /* Use reflection to access internal method because the public 
                     * GetItemsOwner method does always return the itmes control instead 
                     * of the real items owner for example the group item when grouping */
                    MethodInfo getItemsOwnerInternalMethod = typeof(ItemsControl).GetMethod(
                        "GetItemsOwnerInternal",
                        BindingFlags.Static | BindingFlags.NonPublic,
                        null,
                        new Type[] { typeof(DependencyObject) },
                        null
                    )!;
                    _itemsOwner = (DependencyObject)getItemsOwnerInternalMethod.Invoke(null, new object[] { this })!;
                }
                return _itemsOwner;
            }
        }
        private DependencyObject? _itemsOwner;

        protected ReadOnlyCollection<object> Items => ItemContainerGenerator.Items;

        protected IRecyclingItemContainerGenerator RecyclingItemContainerGenerator => ItemContainerGenerator;

        protected new ItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_itemContainerGenerator is null)
                {
                    // The ItemContainerGenerator is null until InternalChildren is accessed at least one time.
                    _ = InternalChildren;
                    _itemContainerGenerator = base.ItemContainerGenerator.GetItemContainerGeneratorForPanel(this);

                }
                return _itemContainerGenerator;
            }
        }
        private ItemContainerGenerator? _itemContainerGenerator;

        public double ExtentWidth => Extent.Width;
        public double ExtentHeight => Extent.Height;

        public double HorizontalOffset => ScrollOffset.X;
        public double VerticalOffset => ScrollOffset.Y;

        public double ViewportWidth => ViewportSize.Width;
        public double ViewportHeight => ViewportSize.Height;

        protected Size Extent { get; set; } = new Size(0, 0);
        protected Size ViewportSize { get; set; } = new Size(0, 0);
        protected Point ScrollOffset { get; set; } = new Point(0, 0);

        private Visibility previousVerticalScrollBarVisibility = Visibility.Collapsed;
        private Visibility previousHorizontalScrollBarVisibility = Visibility.Collapsed;

        protected bool ShouldIgnoreMeasure()
        {
            /* Sometimes when scrolling the scrollbar gets hidden without any reason. 
             * To prevent a infinite circle the mesasure call has to be ignored. */

            IScrollInfo scrollInfo = this;
            ScrollViewer? scrollOwner = ScrollOwner;

            if (ItemsOwner is GroupItem groupItem
                && VisualTreeHelper.GetParent(groupItem) is IScrollInfo parentScrollInfo
                && parentScrollInfo.ScrollOwner is { } parentScrollOwner)
            {
                scrollInfo = parentScrollInfo;
                scrollOwner = parentScrollOwner;
            }

            if (scrollOwner != null)
            {
                bool verticalScrollBarGotHidden = scrollOwner.VerticalScrollBarVisibility == ScrollBarVisibility.Auto
                    && scrollOwner.ComputedVerticalScrollBarVisibility != Visibility.Visible
                    && scrollOwner.ComputedVerticalScrollBarVisibility != previousVerticalScrollBarVisibility;

                bool horizontalScrollBarGotHidden = scrollOwner.HorizontalScrollBarVisibility == ScrollBarVisibility.Auto
                   && scrollOwner.ComputedHorizontalScrollBarVisibility != Visibility.Visible
                   && scrollOwner.ComputedHorizontalScrollBarVisibility != previousHorizontalScrollBarVisibility;

                previousVerticalScrollBarVisibility = scrollOwner.ComputedVerticalScrollBarVisibility;
                previousHorizontalScrollBarVisibility = scrollOwner.ComputedHorizontalScrollBarVisibility;

                if ((verticalScrollBarGotHidden && scrollInfo.ExtentHeight > scrollInfo.ViewportHeight)
                    || (horizontalScrollBarGotHidden && scrollInfo.ExtentWidth > scrollInfo.ViewportWidth))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual Rect MakeVisible(Visual visual, Rect rectangle)
        {
            var transformedBounds = visual.TransformToAncestor(this).TransformBounds(rectangle);

            double offsetX = 0;
            double offsetY = 0;

            double visibleX = 0;
            double visibleY = 0;
            double visibleWidth = Math.Min(rectangle.Width, ViewportWidth);
            double visibleHeight = Math.Min(rectangle.Height, ViewportHeight);

            if (transformedBounds.Left < 0)
            {
                offsetX = transformedBounds.Left;
            }
            else if (transformedBounds.Right > ViewportWidth)
            {
                offsetX = Math.Min(transformedBounds.Right - ViewportWidth, transformedBounds.Left);

                if (rectangle.Width > ViewportWidth)
                {
                    visibleX = rectangle.Width - ViewportWidth;
                }
            }

            if (transformedBounds.Top < 0)
            {
                offsetY = transformedBounds.Top;
            }
            else if (transformedBounds.Bottom > ViewportHeight)
            {
                offsetY = Math.Min(transformedBounds.Bottom - ViewportHeight, transformedBounds.Top);

                if (rectangle.Height > ViewportHeight)
                {
                    visibleY = rectangle.Height - ViewportHeight;
                }
            }

            SetHorizontalOffset(HorizontalOffset + offsetX);
            SetVerticalOffset(VerticalOffset + offsetY);

            return new Rect(visibleX, visibleY, visibleWidth, visibleHeight);
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || ViewportSize.Height >= Extent.Height)
            {
                offset = 0;
            }
            else if (offset + ViewportSize.Height >= Extent.Height)
            {
                offset = Extent.Height - ViewportSize.Height;
            }
            if (offset != ScrollOffset.Y)
            {
                ScrollOffset = new Point(ScrollOffset.X, offset);
                ScrollOwner?.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || ViewportSize.Width >= Extent.Width)
            {
                offset = 0;
            }
            else if (offset + ViewportSize.Width >= Extent.Width)
            {
                offset = Extent.Width - ViewportSize.Width;
            }
            if (offset != ScrollOffset.X)
            {
                ScrollOffset = new Point(offset, ScrollOffset.Y);
                ScrollOwner?.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public void LineUp()
        {
            ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? -ScrollLineDelta : GetLineUpScrollAmount());
        }
        public void LineDown()
        {
            ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? ScrollLineDelta : GetLineDownScrollAmount());
        }
        public void LineLeft()
        {
            ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? -ScrollLineDelta : GetLineLeftScrollAmount());
        }
        public void LineRight()
        {
            ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? ScrollLineDelta : GetLineRightScrollAmount());
        }

        public void MouseWheelUp()
        {
            if (MouseWheelScrollDirection == ScrollDirection.Vertical)
            {
                ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? -MouseWheelDelta : GetMouseWheelUpScrollAmount());
            }
            else
            {
                MouseWheelLeft();
            }
        }
        public void MouseWheelDown()
        {
            if (MouseWheelScrollDirection == ScrollDirection.Vertical)
            {
                ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? MouseWheelDelta : GetMouseWheelDownScrollAmount());
            }
            else
            {
                MouseWheelRight();
            }
        }
        public void MouseWheelLeft()
        {
            ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? -MouseWheelDelta : GetMouseWheelLeftScrollAmount());
        }
        public void MouseWheelRight()
        {
            ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? MouseWheelDelta : GetMouseWheelRightScrollAmount());
        }

        public void PageUp()
        {
            ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? -ViewportSize.Height : GetPageUpScrollAmount());
        }
        public void PageDown()
        {
            ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? ViewportSize.Height : GetPageDownScrollAmount());
        }
        public void PageLeft()
        {
            ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? -ViewportSize.Width : GetPageLeftScrollAmount());
        }
        public void PageRight()
        {
            ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? ViewportSize.Width : GetPageRightScrollAmount());
        }

        protected abstract double GetLineUpScrollAmount();
        protected abstract double GetLineDownScrollAmount();
        protected abstract double GetLineLeftScrollAmount();
        protected abstract double GetLineRightScrollAmount();

        protected abstract double GetMouseWheelUpScrollAmount();
        protected abstract double GetMouseWheelDownScrollAmount();
        protected abstract double GetMouseWheelLeftScrollAmount();
        protected abstract double GetMouseWheelRightScrollAmount();

        protected abstract double GetPageUpScrollAmount();
        protected abstract double GetPageDownScrollAmount();
        protected abstract double GetPageLeftScrollAmount();
        protected abstract double GetPageRightScrollAmount();

        private void ScrollVertical(double amount)
        {
            SetVerticalOffset(ScrollOffset.Y + amount);
        }

        private void ScrollHorizontal(double amount)
        {
            SetHorizontalOffset(ScrollOffset.X + amount);
        }
    }
    /// <summary>
    /// A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation.
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanelBase
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure, (obj, args) => ((VirtualizingWrapPanel)obj).Orientation_Changed()));

        public static readonly DependencyProperty ItemSizeProperty = DependencyProperty.Register(nameof(ItemSize), typeof(Size), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, (obj, args) => ((VirtualizingWrapPanel)obj).ItemSize_Changed()));

        public static readonly DependencyProperty AllowDifferentSizedItemsProperty = DependencyProperty.Register(nameof(AllowDifferentSizedItems), typeof(bool), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure, (obj, args) => ((VirtualizingWrapPanel)obj).AllowDifferentSizedItems_Changed()));

        public static readonly DependencyProperty ItemSizeProviderProperty = DependencyProperty.Register(nameof(ItemSizeProvider), typeof(IItemSizeProvider), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty SpacingModeProperty = DependencyProperty.Register(nameof(SpacingMode), typeof(SpacingMode), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(SpacingMode.Uniform, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty StretchItemsProperty = DependencyProperty.Register(nameof(StretchItems), typeof(bool), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Gets or sets a value that specifies the orientation in which items are arranged before wrapping. The default value is <see cref="Orientation.Horizontal"/>.
        /// </summary>
        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        /// <summary>
        /// Gets or sets a value that specifies the size of the items. The default value is <see cref="Size.Empty"/>. 
        /// If the value is <see cref="Size.Empty"/> the item size is determined by measuring the first realized item.
        /// </summary>
        public Size ItemSize { get => (Size)GetValue(ItemSizeProperty); set => SetValue(ItemSizeProperty, value); }

        /// <summary>
        /// Specifies whether items can have different sizes. The default value is false. If this property is enabled, 
        /// it is strongly recommended to also set the <see cref="ItemSizeProvider"/> property. Otherwise, the position 
        /// of the items is not always guaranteed to be correct.
        /// </summary>
        public bool AllowDifferentSizedItems { get => (bool)GetValue(AllowDifferentSizedItemsProperty); set => SetValue(AllowDifferentSizedItemsProperty, value); }

        /// <summary>
        /// Specifies an instance of <see cref="IItemSizeProvider"/> which provides the size of the items. In order to allow
        /// different sized items, also enable the <see cref="AllowDifferentSizedItems"/> property.
        /// </summary>
        public IItemSizeProvider? ItemSizeProvider { get => (IItemSizeProvider?)GetValue(ItemSizeProviderProperty); set => SetValue(ItemSizeProviderProperty, value); }

        /// <summary>
        /// Gets or sets the spacing mode used when arranging the items. The default value is <see cref="SpacingMode.Uniform"/>.
        /// </summary>
        public SpacingMode SpacingMode { get => (SpacingMode)GetValue(SpacingModeProperty); set => SetValue(SpacingModeProperty, value); }

        /// <summary>
        /// Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
        /// </summary>
        /// <remarks>
        /// The MaxWidth and MaxHeight properties of the ItemContainerStyle can be used to limit the stretching. 
        /// In this case the use of the remaining space will be determined by the SpacingMode property. 
        /// </remarks>
        public bool StretchItems { get => (bool)GetValue(StretchItemsProperty); set => SetValue(StretchItemsProperty, value); }

        /// <summary>
        /// Gets value that indicates whether the <see cref="VirtualizingPanel"/> can virtualize items 
        /// that are grouped or organized in a hierarchy.
        /// </summary>
        /// <returns>always true for <see cref="VirtualizingWrapPanel"/></returns>
        protected override bool CanHierarchicallyScrollAndVirtualizeCore => true;

        protected override bool HasLogicalOrientation => false;

        protected override Orientation LogicalOrientation => Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;

        private static readonly Size InfiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        private static readonly Size FallbackItemSize = new Size(48, 48);

        private ItemContainerManager ItemContainerManager
        {
            get
            {
                if (_itemContainerManager is null)
                {
                    _itemContainerManager = new ItemContainerManager(
                        ItemContainerGenerator,
                        AddInternalChild,
                        child => RemoveInternalChildRange(InternalChildren.IndexOf(child), 1));
                    _itemContainerManager.ItemsChanged += ItemContainerManager_ItemsChanged;
                }
                return _itemContainerManager;
            }
        }
        private ItemContainerManager? _itemContainerManager;

        /// <summary>
        /// The cache length before and after the viewport. 
        /// </summary>
        private VirtualizationCacheLength cacheLength;
        /// <summary>
        /// The Unit of the cache length. Can be Pixel, Item or Page. 
        /// When the ItemsOwner is a group item it can only be pixel or item.
        /// </summary>
        private VirtualizationCacheLengthUnit cacheLengthUnit;

        private Size? sizeOfFirstItem;

        private readonly Dictionary<object, Size> itemSizesCache = new Dictionary<object, Size>();
        private Size? averageItemSizeCache;

        private int startItemIndex = -1;
        private int endItemIndex = -1;

        private double startItemOffsetX = 0;
        private double startItemOffsetY = 0;

        private double knownExtendX = 0;

        private int bringIntoViewItemIndex = -1;
        private FrameworkElement? bringIntoViewContainer;

        public void ClearItemSizeCache()
        {
            itemSizesCache.Clear();
            averageItemSizeCache = null;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (ShouldIgnoreMeasure())
            {
                return DesiredSize;
            }

            ItemContainerManager.IsRecycling = IsRecycling;

            MeasureBringIntoViewContainer(InfiniteSize);

            Size newViewportSize;

            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo groupItem)
            {
                ScrollOffset = groupItem.Constraints.Viewport.Location;
                newViewportSize = GetViewportSizeFromGroupItem(groupItem);
                cacheLength = groupItem.Constraints.CacheLength;
                cacheLengthUnit = groupItem.Constraints.CacheLengthUnit;
            }
            else
            {
                newViewportSize = availableSize;
                cacheLength = GetCacheLength(ItemsOwner);
                cacheLengthUnit = GetCacheLengthUnit(ItemsOwner);
            }

            averageItemSizeCache = null;

            UpdateViewportSize(newViewportSize);
            RealizeAndVirtualizeItems();
            UpdateExtent();

            if (ItemsOwner is not IHierarchicalVirtualizationAndScrollInfo
                && GetY(ScrollOffset) != 0
                && GetY(ScrollOffset) + GetHeight(ViewportSize) > GetHeight(Extent))
            {
                ScrollOffset = CreatePoint(GetX(ScrollOffset), Math.Max(0, GetHeight(Extent) - GetHeight(ViewportSize)));
                ScrollOwner?.InvalidateScrollInfo();
                return MeasureOverride(availableSize); // repeat measure with correct ScrollOffset
            }

            double desiredWidth = Math.Min(availableSize.Width, Extent.Width);
            double desiredHeight = Math.Min(availableSize.Height, Extent.Height);

            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    desiredWidth = Math.Max(desiredWidth, newViewportSize.Width);
                }
                else
                {
                    desiredHeight = Math.Max(desiredHeight, newViewportSize.Height);
                }
            }

            return new Size(desiredWidth, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            ViewportSize = finalSize;

            ArrangeBringIntoViewContainer();

            foreach (var cachedContainer in ItemContainerManager.CachedContainers)
            {
                cachedContainer.Arrange(new Rect(0, 0, 0, 0));
            }

            if (startItemIndex == -1)
            {
                return finalSize;
            }

            bool hierarchical = ItemsOwner is IHierarchicalVirtualizationAndScrollInfo;
            double x = startItemOffsetX + GetX(ScrollOffset);
            double y = hierarchical ? startItemOffsetY : startItemOffsetY - GetY(ScrollOffset);
            double rowHeight = 0;
            var rowChilds = new List<UIElement>();
            var childSizes = new List<Size>();

            for (int i = startItemIndex; i <= endItemIndex; i++)
            {
                var item = Items[i];
                var child = ItemContainerManager.RealizedContainers[item];

                Size? upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

                Size childSize = upfrontKnownItemSize ?? itemSizesCache[item];

                if (rowChilds.Count > 0 && x + GetWidth(childSize) > GetWidth(finalSize))
                {
                    ArrangeRow(GetWidth(finalSize), rowChilds, childSizes, y, hierarchical);
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    rowChilds.Clear();
                    childSizes.Clear();
                }

                x += GetWidth(childSize);
                rowHeight = Math.Max(rowHeight, GetHeight(childSize));
                rowChilds.Add(child);
                childSizes.Add(childSize);
            }

            if (rowChilds.Any())
            {
                ArrangeRow(GetWidth(finalSize), rowChilds, childSizes, y, hierarchical);
            }

            return finalSize;
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"The argument {nameof(index)} must be >= 0 and < the count of items.");
            }

            var container = (FrameworkElement)ItemContainerManager.Realize(index);

            bringIntoViewItemIndex = index;
            bringIntoViewContainer = container;

            // make sure the container is measured and arranged before calling BringIntoView        
            InvalidateMeasure();
            UpdateLayout();

            container.BringIntoView();
        }

        private void ItemContainerManager_ItemsChanged(object? sender, ItemContainerManagerItemsChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (var key in itemSizesCache.Keys.Except(Items).ToList())
                {
                    itemSizesCache.Remove(key);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                itemSizesCache.Clear();

                if (AllowDifferentSizedItems && ItemSizeProvider is null)
                {
                    ScrollOffset = new Point(0, 0);
                }
            }
        }

        private void Orientation_Changed()
        {
            MouseWheelScrollDirection = Orientation == Orientation.Horizontal
                                        ? ScrollDirection.Vertical
                                        : ScrollDirection.Horizontal;
            SetVerticalOffset(0);
            SetHorizontalOffset(0);
        }

        private void AllowDifferentSizedItems_Changed()
        {
            foreach (var child in InternalChildren.Cast<UIElement>())
            {
                child.InvalidateMeasure();
            }
        }

        private void ItemSize_Changed()
        {
            foreach (var child in InternalChildren.Cast<UIElement>())
            {
                child.InvalidateMeasure();
            }
        }

        private Size GetViewportSizeFromGroupItem(IHierarchicalVirtualizationAndScrollInfo groupItem)
        {
            double viewportWidth = Math.Max(groupItem.Constraints.Viewport.Size.Width, 0);
            double viewporteHeight = Math.Max(groupItem.Constraints.Viewport.Size.Height, 0);

            if (VisualTreeHelper.GetParent(this) is ItemsPresenter itemsPresenter)
            {
                var margin = itemsPresenter.Margin;

                if (Orientation == Orientation.Horizontal)
                {
                    viewportWidth = Math.Max(0, viewportWidth - (margin.Left + margin.Right));
                }
                else
                {
                    viewporteHeight = Math.Max(0, viewporteHeight - (margin.Top + margin.Bottom));
                }
            }

            if (Orientation == Orientation.Vertical)
            {
                viewporteHeight = Math.Max(0, viewporteHeight - groupItem.HeaderDesiredSizes.PixelSize.Height);
            }

            return new Size(viewportWidth, viewporteHeight);
        }

        private void MeasureBringIntoViewContainer(Size availableSize)
        {
            if (bringIntoViewContainer is not null && !bringIntoViewContainer.IsMeasureValid)
            {
                bringIntoViewContainer.Measure(GetUpfrontKnownItemSize(Items[bringIntoViewItemIndex]) ?? availableSize);
                sizeOfFirstItem ??= bringIntoViewContainer.DesiredSize;
            }
        }

        private void ArrangeBringIntoViewContainer()
        {
            if (bringIntoViewContainer is not null)
            {
                bool hierarchical = ItemsOwner is IHierarchicalVirtualizationAndScrollInfo;
                var offset = FindItemOffset(bringIntoViewItemIndex);
                offset = new Point(offset.X - GetX(ScrollOffset), hierarchical ? offset.Y : offset.Y - GetY(ScrollOffset));
                var size = GetUpfrontKnownItemSize(Items[bringIntoViewItemIndex]) ?? bringIntoViewContainer.DesiredSize;
                bringIntoViewContainer.Arrange(new Rect(offset, size));
            }
        }

        private void RealizeAndVirtualizeItems()
        {
            FindStartIndexAndOffset();
            VirtualizeItemsBeforeStartIndex();
            RealizeItemsAndFindEndIndex();
            VirtualizeItemsAfterEndIndex();
        }

        private Size GetAverageItemSize()
        {
            if (ItemSize != Size.Empty)
            {
                return ItemSize;
            }
            else if (!AllowDifferentSizedItems)
            {
                return sizeOfFirstItem ?? FallbackItemSize;
            }
            else
            {
                return averageItemSizeCache ??= CalculateAverageItemSize();
            }
        }

        private Point FindItemOffset(int itemIndex)
        {
            double x = 0, y = 0, rowHeight = 0;

            for (int i = 0; i <= itemIndex; i++)
            {
                Size itemSize = GetAssumedItemSize(Items[i]);

                if (x != 0 && x + GetWidth(itemSize) > GetWidth(ViewportSize))
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }

                if (i != itemIndex)
                {
                    x += GetWidth(itemSize);
                    rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
                }
            }

            return CreatePoint(x, y);
        }

        private void UpdateViewportSize(Size newViewportSize)
        {
            // Retain the current viewport size if the new viewport size
            // received from the parent virtualizing panel is zero. This 
            // is necessary for the BringIndexIntoView function to work.
            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo
                && newViewportSize.Width == 0
                && newViewportSize.Height == 0)
            {
                return;
            }

            if (newViewportSize != ViewportSize)
            {
                ViewportSize = newViewportSize;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        private void FindStartIndexAndOffset()
        {
            if (ViewportSize.Width == 0 && ViewportSize.Height == 0)
            {
                startItemIndex = -1;
                startItemOffsetX = 0;
                startItemOffsetY = 0;
                return;
            }

            double startOffsetY = DetermineStartOffsetY();

            if (startOffsetY <= 0)
            {
                startItemIndex = 0;
                startItemOffsetX = 0;
                startItemOffsetY = 0;
                return;
            }

            startItemIndex = -1;

            double x = 0, y = 0, rowHeight = 0;
            int indexOfFirstRowItem = 0;

            int itemIndex = 0;
            foreach (var item in Items)
            {
                Size itemSize = GetAssumedItemSize(item);

                if (x + GetWidth(itemSize) > GetWidth(ViewportSize) && x != 0)
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    indexOfFirstRowItem = itemIndex;
                }
                x += GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));

                if (y + rowHeight > startOffsetY)
                {
                    if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                    {
                        startItemIndex = Math.Max(indexOfFirstRowItem - (int)cacheLength.CacheBeforeViewport, 0);
                        var itemOffset = FindItemOffset(startItemIndex);
                        startItemOffsetX = GetX(itemOffset);
                        startItemOffsetY = GetY(itemOffset);
                    }
                    else
                    {
                        startItemIndex = indexOfFirstRowItem;
                        startItemOffsetX = 0;
                        startItemOffsetY = y;
                    }
                    break;
                }

                itemIndex++;
            }

            // make sure that at least one item is realized to allow correct calculation of the extend
            if (startItemIndex == -1 && Items.Count > 0)
            {
                startItemIndex = Items.Count - 1;
                startItemOffsetX = x;
                startItemOffsetY = y;
            }
        }

        private void RealizeItemsAndFindEndIndex()
        {
            if (startItemIndex == -1)
            {
                endItemIndex = -1;
                knownExtendX = 0;
                return;
            }

            int newEndItemIndex = Items.Count - 1;
            bool endItemIndexFound = false;

            double endOffsetY = DetermineEndOffsetY();

            double x = startItemOffsetX;
            double y = startItemOffsetY;
            double rowHeight = 0;

            knownExtendX = 0;

            for (int itemIndex = startItemIndex; itemIndex <= newEndItemIndex; itemIndex++)
            {
                if (itemIndex == 0)
                {
                    sizeOfFirstItem = null;
                }

                object item = Items[itemIndex];

                var container = ItemContainerManager.Realize(itemIndex);

                if (container == bringIntoViewContainer)
                {
                    bringIntoViewItemIndex = -1;
                    bringIntoViewContainer = null;
                }

                Size? upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

                container.Measure(upfrontKnownItemSize ?? InfiniteSize);

                var containerSize = DetermineContainerSize(item, container, upfrontKnownItemSize);

                if (AllowDifferentSizedItems == false && sizeOfFirstItem is null)
                {
                    sizeOfFirstItem = containerSize;
                }

                if (x != 0 && x + GetWidth(containerSize) > GetWidth(ViewportSize))
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }

                x += GetWidth(containerSize);
                knownExtendX = Math.Max(x, knownExtendX);
                rowHeight = Math.Max(rowHeight, GetHeight(containerSize));

                if (endItemIndexFound == false)
                {
                    if (y >= endOffsetY
                        || (AllowDifferentSizedItems == false
                            && x + GetWidth(sizeOfFirstItem!.Value) > GetWidth(ViewportSize)
                            && y + rowHeight >= endOffsetY))
                    {
                        endItemIndexFound = true;

                        newEndItemIndex = itemIndex;

                        if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                        {
                            newEndItemIndex = Math.Min(newEndItemIndex + (int)cacheLength.CacheAfterViewport, Items.Count - 1);
                            // loop continues unitl newEndItemIndex is reached
                        }
                    }
                }
            }

            endItemIndex = newEndItemIndex;
        }

        private Size DetermineContainerSize(object item, UIElement container, Size? upfrontKnownItemSize)
        {
            Size containerSize = upfrontKnownItemSize ?? container.DesiredSize;

            if (AllowDifferentSizedItems)
            {
                itemSizesCache[item] = containerSize;
            }

            return containerSize;
        }

        private void VirtualizeItemsBeforeStartIndex()
        {
            var containers = ItemContainerManager.RealizedContainers.Values.ToList();
            foreach (var container in containers.Where(container => container != bringIntoViewContainer))
            {
                int itemIndex = ItemContainerManager.FindItemIndexOfContainer(container);

                if (itemIndex < startItemIndex)
                {
                    ItemContainerManager.Virtualize(container);
                }
            }
        }

        private void VirtualizeItemsAfterEndIndex()
        {
            var containers = ItemContainerManager.RealizedContainers.Values.ToList();
            foreach (var container in containers.Where(container => container != bringIntoViewContainer))
            {
                int itemIndex = ItemContainerManager.FindItemIndexOfContainer(container);

                if (itemIndex > endItemIndex)
                {
                    ItemContainerManager.Virtualize(container);
                }
            }
        }

        private void UpdateExtent()
        {
            Size extent;

            if (startItemIndex == -1)
            {
                extent = new Size(0, 0);
            }
            else if (!AllowDifferentSizedItems)
            {
                extent = CalculateExtentForSameSizedItems();
            }
            else
            {
                extent = CalculateExtentForDifferentSizedItems();
            }

            if (extent != Extent)
            {
                Extent = extent;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        private Size CalculateExtentForSameSizedItems()
        {
            var itemSize = ItemSize != Size.Empty ? ItemSize : sizeOfFirstItem!.Value;
            int itemsPerRow = (int)Math.Max(1, Math.Floor(GetWidth(ViewportSize) / GetWidth(itemSize)));
            double extentY = Math.Ceiling(((double)Items.Count) / itemsPerRow) * GetHeight(itemSize);
            return CreateSize(knownExtendX, extentY);
        }

        private Size CalculateExtentForDifferentSizedItems()
        {
            double x = 0;
            double y = 0;
            double rowHeight = 0;
            foreach (var item in Items)
            {
                Size itemSize = GetAssumedItemSize(item);

                if (x + GetWidth(itemSize) > GetWidth(ViewportSize) && x != 0)
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }
                x += GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
            }

            return CreateSize(knownExtendX, y + rowHeight);
        }

        private double DetermineStartOffsetY()
        {
            double cacheLength = 0;

            if (cacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                cacheLength = this.cacheLength.CacheBeforeViewport * GetHeight(ViewportSize);
            }
            else if (cacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                cacheLength = this.cacheLength.CacheBeforeViewport;
            }

            return Math.Max(GetY(ScrollOffset) - cacheLength, 0);
        }

        private double DetermineEndOffsetY()
        {
            double cacheLength = 0;

            if (cacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                cacheLength = this.cacheLength.CacheAfterViewport * GetHeight(ViewportSize);
            }
            else if (cacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                cacheLength = this.cacheLength.CacheAfterViewport;
            }

            return Math.Max(GetY(ScrollOffset), 0) + GetHeight(ViewportSize) + cacheLength;
        }

        private Size? GetUpfrontKnownItemSize(object item)
        {
            if (ItemSize != Size.Empty)
            {
                return ItemSize;
            }
            if (!AllowDifferentSizedItems && sizeOfFirstItem != null)
            {
                return sizeOfFirstItem;
            }
            if (ItemSizeProvider != null)
            {
                return ItemSizeProvider.GetSizeForItem(item);
            }
            return null;
        }

        private Size GetAssumedItemSize(object item)
        {
            if (GetUpfrontKnownItemSize(item) is Size upfrontKnownItemSize)
            {
                return upfrontKnownItemSize;
            }

            if (itemSizesCache.TryGetValue(item, out Size cachedItemSize))
            {
                return cachedItemSize;
            }

            return GetAverageItemSize();
        }

        private void ArrangeRow(double rowWidth, List<UIElement> children, List<Size> childSizes, double y, bool hierarchical)
        {
            double summedUpChildWidth;
            double extraWidth = 0;

            if (AllowDifferentSizedItems)
            {
                summedUpChildWidth = childSizes.Sum(childSize => GetWidth(childSize));

                if (StretchItems)
                {
                    double unusedWidth = rowWidth - summedUpChildWidth;
                    extraWidth = unusedWidth / children.Count;
                    summedUpChildWidth = rowWidth;
                }
            }
            else
            {
                double childWidth = GetWidth(childSizes[0]);
                int itemsPerRow = (int)Math.Max(Math.Floor(rowWidth / childWidth), 1);

                if (StretchItems)
                {
                    var firstChild = (FrameworkElement)children[0];
                    double maxWidth = Orientation == Orientation.Horizontal ? firstChild.MaxWidth : firstChild.MaxHeight;
                    double stretchedChildWidth = Math.Min(rowWidth / itemsPerRow, maxWidth);
                    stretchedChildWidth = Math.Max(stretchedChildWidth, childWidth); // ItemSize might be greater than MaxWidth/MaxHeight
                    extraWidth = stretchedChildWidth - childWidth;
                    summedUpChildWidth = itemsPerRow * stretchedChildWidth;
                }
                else
                {
                    summedUpChildWidth = itemsPerRow * childWidth;
                }
            }

            double innerSpacing = 0;
            double outerSpacing = 0;

            if (summedUpChildWidth < rowWidth)
            {
                CalculateRowSpacing(rowWidth, children, summedUpChildWidth, out innerSpacing, out outerSpacing);
            }

            double x = hierarchical ? outerSpacing : -GetX(ScrollOffset) + outerSpacing;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                Size childSize = childSizes[i];
                child.Arrange(CreateRect(x, y, GetWidth(childSize) + extraWidth, GetHeight(childSize)));
                x += GetWidth(childSize) + extraWidth + innerSpacing;
            }
        }

        private void CalculateRowSpacing(double rowWidth, List<UIElement> children, double summedUpChildWidth, out double innerSpacing, out double outerSpacing)
        {
            int childCount;

            if (AllowDifferentSizedItems)
            {
                childCount = children.Count;
            }
            else
            {
                childCount = (int)Math.Max(1, Math.Floor(rowWidth / GetWidth(sizeOfFirstItem!.Value)));
            }

            double unusedWidth = Math.Max(0, rowWidth - summedUpChildWidth);

            switch (SpacingMode)
            {
                case SpacingMode.Uniform:
                    innerSpacing = outerSpacing = unusedWidth / (childCount + 1);
                    break;

                case SpacingMode.BetweenItemsOnly:
                    innerSpacing = unusedWidth / Math.Max(childCount - 1, 1);
                    outerSpacing = 0;
                    break;

                case SpacingMode.StartAndEndOnly:
                    innerSpacing = 0;
                    outerSpacing = unusedWidth / 2;
                    break;

                case SpacingMode.None:
                default:
                    innerSpacing = 0;
                    outerSpacing = 0;
                    break;
            }
        }

        private Size CalculateAverageItemSize()
        {
            if (itemSizesCache.Values.Count > 0)
            {
                return new Size(
                    Math.Round(itemSizesCache.Values.Average(size => size.Width)),
                    Math.Round(itemSizesCache.Values.Average(size => size.Height)));
            }
            return FallbackItemSize;
        }

        #region scroll info

        // TODO determine exact scoll amount for item based scrolling when AllowDifferentSizedItems is true

        protected override double GetLineUpScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);
        }

        protected override double GetLineDownScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);
        }

        protected override double GetLineLeftScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);
        }

        protected override double GetLineRightScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);
        }

        protected override double GetMouseWheelUpScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);
        }

        protected override double GetMouseWheelDownScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);
        }

        protected override double GetMouseWheelLeftScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);
        }

        protected override double GetMouseWheelRightScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);
        }

        protected override double GetPageUpScrollAmount()
        {
            return -ViewportSize.Height;
        }

        protected override double GetPageDownScrollAmount()
        {
            return ViewportSize.Height;
        }

        protected override double GetPageLeftScrollAmount()
        {
            return -ViewportSize.Width;
        }

        protected override double GetPageRightScrollAmount()
        {
            return ViewportSize.Width;
        }

        #endregion

        #region orientation aware helper methods

        private double GetX(Point point) => Orientation == Orientation.Horizontal ? point.X : point.Y;
        private double GetY(Point point) => Orientation == Orientation.Horizontal ? point.Y : point.X;
        private double GetWidth(Size size) => Orientation == Orientation.Horizontal ? size.Width : size.Height;
        private double GetHeight(Size size) => Orientation == Orientation.Horizontal ? size.Height : size.Width;
        private Point CreatePoint(double x, double y) => Orientation == Orientation.Horizontal ? new Point(x, y) : new Point(y, x);
        private Size CreateSize(double width, double height) => Orientation == Orientation.Horizontal ? new Size(width, height) : new Size(height, width);
        private Rect CreateRect(double x, double y, double width, double height) => Orientation == Orientation.Horizontal ? new Rect(x, y, width, height) : new Rect(y, x, height, width);

        #endregion
    }

    internal class ItemContainerManagerItemsChangedEventArgs
    {
        public NotifyCollectionChangedAction Action { get; }

        public ItemContainerManagerItemsChangedEventArgs(NotifyCollectionChangedAction action)
        {
            Action = action;
        }
    }

    internal class ItemContainerManager
    {
        /// <summary>
        /// Occurs when the <see cref="Items"/> collection changes.
        /// </summary>
        public event EventHandler<ItemContainerManagerItemsChangedEventArgs>? ItemsChanged;

        /// <summary>
        /// Indicates whether containers are recycled or not.
        /// </summary>
        public bool IsRecycling { get; set; }

        /// <summary>
        /// Collection that contains the items for which containers are generated.
        /// </summary>
        public IReadOnlyList<object> Items => itemContainerGenerator.Items;

        /// <summary>
        /// Dictionary that contains the realised containers. The keys are the items, the values are the containers.
        /// </summary>
        public IReadOnlyDictionary<object, UIElement> RealizedContainers => realizedContainers;

        /// <summary>
        /// Collection that contains the cached containers. Always emtpy if <see cref="IsRecycling"/> is false.
        /// </summary>
        public IReadOnlyCollection<UIElement> CachedContainers => cachedContainers.ToArray();

        private readonly Dictionary<object, UIElement> realizedContainers = new Dictionary<object, UIElement>();

        private readonly HashSet<UIElement> cachedContainers = new HashSet<UIElement>();

        private readonly ItemContainerGenerator itemContainerGenerator;

        private readonly IRecyclingItemContainerGenerator recyclingItemContainerGenerator;

        private readonly Action<UIElement> addInternalChild;
        private readonly Action<UIElement> removeInternalChild;

        public ItemContainerManager(ItemContainerGenerator itemContainerGenerator, Action<UIElement> addInternalChild, Action<UIElement> removeInternalChild)
        {
            this.itemContainerGenerator = itemContainerGenerator;
            this.recyclingItemContainerGenerator = itemContainerGenerator;
            this.addInternalChild = addInternalChild;
            this.removeInternalChild = removeInternalChild;
            itemContainerGenerator.ItemsChanged += ItemContainerGenerator_ItemsChanged;
        }

        public UIElement Realize(int itemIndex)
        {
            var item = Items[itemIndex];

            if (realizedContainers.TryGetValue(item, out var existingContainer))
            {
                return existingContainer;
            }

            var generatorPosition = recyclingItemContainerGenerator.GeneratorPositionFromIndex(itemIndex);
            using (recyclingItemContainerGenerator.StartAt(generatorPosition, GeneratorDirection.Forward))
            {
                var container = (UIElement)recyclingItemContainerGenerator.GenerateNext(out bool isNewContainer);

                cachedContainers.Remove(container);
                realizedContainers.Add(item, container);

                if (isNewContainer)
                {
                    addInternalChild(container);
                }
                else
                {
                    InvalidateMeasureRecursively(container);
                }

                recyclingItemContainerGenerator.PrepareItemContainer(container);

                return container;
            }
        }

        public void Virtualize(UIElement container)
        {
            int itemIndex = FindItemIndexOfContainer(container);

            if (itemIndex == -1) // the item is already virtualized (can happen when grouping)
            {
                realizedContainers.Remove(realizedContainers.Where(entry => entry.Value == container).Single().Key);

                if (IsRecycling)
                {
                    cachedContainers.Add(container);
                }
                else
                {
                    removeInternalChild(container);
                }

                return;
            }

            var item = Items[itemIndex];

            var generatorPosition = recyclingItemContainerGenerator.GeneratorPositionFromIndex(itemIndex);

            if (IsRecycling)
            {
                recyclingItemContainerGenerator.Recycle(generatorPosition, 1);
                realizedContainers.Remove(item);
                cachedContainers.Add(container);
            }
            else
            {
                recyclingItemContainerGenerator.Remove(generatorPosition, 1);
                realizedContainers.Remove(item);
                removeInternalChild(container);
            }
        }

        public int FindItemIndexOfContainer(UIElement container)
        {
            return itemContainerGenerator.IndexFromContainer(container);
        }

        private void ItemContainerGenerator_ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                realizedContainers.Clear();
                cachedContainers.Clear();
                // children collection is cleared automatically

                ItemsChanged?.Invoke(this, new ItemContainerManagerItemsChangedEventArgs(e.Action));
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (var (item, container) in realizedContainers.Where(entry => !Items.Contains(entry.Key)).ToList())
                {
                    realizedContainers.Remove(item);

                    if (IsRecycling)
                    {
                        cachedContainers.Add(container);
                    }
                    else
                    {
                        removeInternalChild(container);
                    }
                }

                ItemsChanged?.Invoke(this, new ItemContainerManagerItemsChangedEventArgs(e.Action));
            }
            else
            {
                ItemsChanged?.Invoke(this, new ItemContainerManagerItemsChangedEventArgs(e.Action));
            }
        }

        private static void InvalidateMeasureRecursively(UIElement element)
        {
            element.InvalidateMeasure();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                if (VisualTreeHelper.GetChild(element, i) is UIElement child)
                {
                    InvalidateMeasureRecursively(child);
                }
            }
        }
    }

    /// <summary>
    /// Specifies how remaining space is distributed.
    /// </summary>
    public enum SpacingMode
    {
        /// <summary>
        /// Spacing is disabled and all items will be arranged as closely as possible.
        /// </summary>
        None,
        /// <summary>
        /// The remaining space is evenly distributed between the items on a layout row, as well as the start and end of each row.
        /// </summary>
        Uniform,
        /// <summary>
        /// The remaining space is evenly distributed between the items on a layout row, excluding the start and end of each row.
        /// </summary>
        BetweenItemsOnly,
        /// <summary>
        /// The remaining space is evenly distributed between start and end of each row.
        /// </summary>
        StartAndEndOnly
    }

    /// <summary>
    /// Provides the size of items displayed in an VirtualizingPanel.
    /// </summary>
    public interface IItemSizeProvider
    {
        /// <summary>
        /// Gets the size for the specified item.
        /// </summary>
        Size GetSizeForItem(object item);
    }
}