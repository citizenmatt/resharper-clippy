using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public class JustifiedUniformGrid : UniformGrid
    {
        protected override Size MeasureOverride(Size constraint)
        {
            if (InternalChildren.Count == 0)
                return Size.Empty;

            var columns = Columns == 0 ? InternalChildren.Count : Columns;
            var availableSize = new Size(constraint.Width / columns, constraint.Height / Rows);

            var firstItem = InternalChildren[0];
            firstItem.Measure(availableSize);

            if (columns == 1)
                return base.MeasureOverride(constraint);

            var lastItem = InternalChildren[InternalChildren.Count - 1];
            lastItem.Measure(availableSize);

            var firstItemWidth = firstItem.DesiredSize.Width;
            var lastItemWidth = lastItem.DesiredSize.Width;
            var sidesHeight = Math.Max(firstItem.DesiredSize.Height, lastItem.DesiredSize.Height);
            if (columns == 2)
                return new Size(firstItemWidth + lastItemWidth, sidesHeight);

            var remainingColumns = columns - 2;
            availableSize = new Size((constraint.Width - firstItemWidth - lastItemWidth) / remainingColumns, sidesHeight);
            var width = 0.0;
            var height = 0.0;
            foreach (var child in InternalChildren.OfType<UIElement>().Skip(1).Take(columns - 2))
            {
                child.Measure(availableSize);
                var desiredSize = child.DesiredSize;
                if (width < desiredSize.Width)
                    width = desiredSize.Width;
                if (height < desiredSize.Height)
                    height = desiredSize.Height;
            }

            return new Size((width * remainingColumns) + firstItemWidth + lastItemWidth, height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (InternalChildren.Count == 0)
                return arrangeSize;

            if (InternalChildren.Count == 1)
                return base.ArrangeOverride(arrangeSize);

            var firstItem = InternalChildren[0];
            var lastItem = InternalChildren[InternalChildren.Count - 1];

            var firstItemSize = firstItem.DesiredSize;
            var lastItemSize = lastItem.DesiredSize;

            var sidesRect = new Rect(0.0, 0.0, Math.Max(firstItemSize.Width, lastItemSize.Width),
                Math.Max(firstItemSize.Height, lastItemSize.Height));

            firstItem.Arrange(sidesRect);

            if (InternalChildren.Count > 2)
            {
                var columns = Columns == 0 ? InternalChildren.Count : Columns;

                // Note this doesn't support wrapping
                var remainingWidth = arrangeSize.Width - (sidesRect.Width * 2);
                var remainingColumns = columns - 2;
                var finalRect = new Rect(sidesRect.Width, 0.0, remainingWidth / remainingColumns, arrangeSize.Height / Rows);
                var width = finalRect.Width;
                foreach (var child in InternalChildren.OfType<UIElement>().Skip(1).Take(remainingColumns))
                {
                    child.Arrange(finalRect);
                    if (child.Visibility != Visibility.Collapsed)
                    {
                        finalRect.X += width;
                    }
                }
            }

            sidesRect.X = arrangeSize.Width - lastItemSize.Width;
            lastItem.Arrange(sidesRect);

            return arrangeSize;
        }
    }
}