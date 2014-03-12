using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CitizenMatt.ReSharper.Plugins.Clippy.AgentApi.Balloon
{
    public class BalloonDecorator : Decorator
    {
        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register("BorderThickness", typeof (double), typeof (BalloonDecorator),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender |
                                                   FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof (Brush), typeof (BalloonDecorator));

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register("BorderBrush", typeof (Brush), typeof (BalloonDecorator));

        public static readonly DependencyProperty PointerLengthProperty =
            DependencyProperty.Register("PointerLength", typeof (double), typeof (BalloonDecorator),
                new FrameworkPropertyMetadata(13.0, FrameworkPropertyMetadataOptions.AffectsRender |
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        // TODO: Change this to an anchor location? Definitely shouldn't be a string position
        public static readonly DependencyProperty PointerPositionProperty =
            DependencyProperty.Register("PointerPosition", typeof (string), typeof (BalloonDecorator),
                new FrameworkPropertyMetadata("Left", FrameworkPropertyMetadataOptions.AffectsRender |
                                                      FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof (double), typeof (BalloonDecorator),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender |
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public string PointerPosition
        {
            get { return (string) GetValue(PointerPositionProperty); }
            set { SetValue(PointerPositionProperty, value); }
        }

        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return (Brush) GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public double BorderThickness
        {
            get { return (double) GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        public double PointerLength
        {
            get { return (double) GetValue(PointerLengthProperty); }
            set { SetValue(PointerLengthProperty, value); }
        }

        public double CornerRadius
        {
            get { return (double) GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var pointerWidth = PointerPosition == "Bottom" ? 0.0 : PointerLength;
            var pointerHeight = PointerPosition == "Bottom" ? PointerLength : 0.0;

            var child = Child;
            var size = new Size();
            if (child != null)
            {
                var innerSize = new Size(Math.Max(0, constraint.Width - pointerWidth),
                    Math.Max(0, constraint.Height - pointerHeight));
                child.Measure(innerSize);
                size.Width += child.DesiredSize.Width;
                size.Height += child.DesiredSize.Height;
            }

            var borderSize = new Size(2*BorderThickness, 2*BorderThickness);
            size.Width += borderSize.Width + pointerWidth;
            size.Height += borderSize.Height + pointerHeight;

            return size;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var pointerWidth = PointerPosition == "Bottom" ? 0.0 : PointerLength;
            var pointerHeight = PointerPosition == "Bottom" ? PointerLength : 0.0;

            var borderThickness = BorderThickness;

            var child = Child;
            if (child != null)
            {
                var innerRect = new Rect(0, 0, Math.Max(0, arrangeSize.Width - pointerWidth),
                    Math.Max(0, arrangeSize.Height - pointerHeight));
                if (PointerPosition == "Left")
                    innerRect.X += pointerWidth;
                innerRect.Inflate(-borderThickness, -borderThickness);
                child.Arrange(innerRect);
            }

            return arrangeSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Rect rect = new Rect(0, 0, RenderSize.Width, RenderSize.Height);

            dc.PushClip(new RectangleGeometry(rect));
            dc.DrawGeometry(Background, new Pen(BorderBrush, BorderThickness), CreateBalloonGeometry(rect));

            dc.Pop();
        }

        private StreamGeometry CreateBalloonGeometry(Rect rect)
        {
            var radius = Math.Min(CornerRadius, rect.Height/2);

            var pointerLength = PointerLength;
            var bounds = new Rect(0, 0, rect.Width, rect.Height);
            if (PointerPosition != "Bottom")
                bounds.Width -= pointerLength;
            if (PointerPosition == "Left")
                bounds.X += pointerLength;
            if (PointerPosition == "Bottom")
                bounds.Height -= pointerLength;

            var geometry = new StreamGeometry {FillRule = FillRule.Nonzero};
            using (var context = geometry.Open())
            {
                var radiusSize = new Size(radius, radius);
                var startPoint = new Point(bounds.Left + radius, bounds.Top);

                context.BeginFigure(startPoint, true, true);
                context.LineTo(new Point(bounds.Right - radius, bounds.Top), true, false);
                context.ArcTo(new Point(bounds.Right, bounds.Top + radius), radiusSize, 0, false,
                    SweepDirection.Clockwise, true, false);
                InsertBalloonTailRight(context, bounds);
                context.LineTo(new Point(bounds.Right, bounds.Bottom - radius), true, false);
                context.ArcTo(new Point(bounds.Right - radius, bounds.Bottom), radiusSize, 0, false,
                    SweepDirection.Clockwise, true, false);
                InsertBalloonTailBottom(context, bounds);
                context.LineTo(new Point(bounds.Left + radius, bounds.Bottom), true, false);
                context.ArcTo(new Point(bounds.Left, bounds.Bottom - radius), radiusSize, 0, false,
                    SweepDirection.Clockwise, true, false);
                InsertBalloonTailLeft(context, bounds);
                context.LineTo(new Point(bounds.Left, bounds.Top + radius), true, false);
                context.ArcTo(startPoint, radiusSize, 0, false, SweepDirection.Clockwise, true, false);
            }

            return geometry;
        }

        private void InsertBalloonTailLeft(StreamGeometryContext context, Rect bounds)
        {
            if (PointerPosition == "Left")
            {
                var pointerLength = PointerLength;
                var tailStart = bounds.Height/2.0;
                var tailEnd = tailStart - (pointerLength/1.5);
                context.LineTo(new Point(bounds.Left, tailStart), true, false);
                context.LineTo(new Point(bounds.Left - pointerLength, tailStart), true, false);
                context.LineTo(new Point(bounds.Left, tailEnd), true, false);
            }
        }

        private void InsertBalloonTailRight(StreamGeometryContext context, Rect bounds)
        {
            if (PointerPosition == "Right")
            {
                var pointerLength = PointerLength;
                var tailStart = bounds.Height/2.0;
                var tailEnd = tailStart + (pointerLength/1.5);
                context.LineTo(new Point(bounds.Right, tailStart), true, false);
                context.LineTo(new Point(bounds.Right + pointerLength, tailStart), true, false);
                context.LineTo(new Point(bounds.Right, tailEnd), true, false);
            }
        }

        private void InsertBalloonTailBottom(StreamGeometryContext context, Rect bounds)
        {
            if (PointerPosition == "Bottom")
            {
                var pointerLength = PointerLength;
                var tailStart = bounds.Width/2.0;
                var tailEnd = tailStart - (pointerLength/1.5);
                context.LineTo(new Point(tailStart, bounds.Bottom), true, false);
                context.LineTo(new Point(tailStart, bounds.Bottom + pointerLength), true, false);
                context.LineTo(new Point(tailEnd, bounds.Bottom), true, false);
            }
        }
    }
}