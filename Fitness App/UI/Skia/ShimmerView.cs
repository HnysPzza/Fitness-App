using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Fitness_App.UI.Skia
{
    public sealed class ShimmerView : SKCanvasView
    {
        public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
            nameof(IsActive),
            typeof(bool),
            typeof(ShimmerView),
            true,
            propertyChanged: (b, o, n) => ((ShimmerView)b).OnIsActiveChanged());

        public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
            nameof(CornerRadius),
            typeof(float),
            typeof(ShimmerView),
            16f,
            propertyChanged: (b, o, n) => ((ShimmerView)b).InvalidateSurface());

        public static readonly BindableProperty BaseColorProperty = BindableProperty.Create(
            nameof(BaseColor),
            typeof(Color),
            typeof(ShimmerView),
            Colors.LightGray,
            propertyChanged: (b, o, n) => ((ShimmerView)b).InvalidateSurface());

        public static readonly BindableProperty HighlightColorProperty = BindableProperty.Create(
            nameof(HighlightColor),
            typeof(Color),
            typeof(ShimmerView),
            Colors.White,
            propertyChanged: (b, o, n) => ((ShimmerView)b).InvalidateSurface());

        private float _phase;
        private bool _isTicking;

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public float CornerRadius
        {
            get => (float)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public Color BaseColor
        {
            get => (Color)GetValue(BaseColorProperty);
            set => SetValue(BaseColorProperty, value);
        }

        public Color HighlightColor
        {
            get => (Color)GetValue(HighlightColorProperty);
            set => SetValue(HighlightColorProperty, value);
        }

        public ShimmerView()
        {
            EnableTouchEvents = false;
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            EnsureTicking();
        }

        private void OnIsActiveChanged()
        {
            EnsureTicking();
            InvalidateSurface();
        }

        private void EnsureTicking()
        {
            if (!IsActive || Handler is null)
            {
                _isTicking = false;
                return;
            }

            if (_isTicking)
                return;

            _isTicking = true;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
            {
                if (!IsActive || Handler is null)
                {
                    _isTicking = false;
                    return false;
                }

                _phase += 0.018f;
                if (_phase > 1f)
                    _phase -= 1f;

                InvalidateSurface();
                return true;
            });
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var w = e.Info.Width;
            var h = e.Info.Height;
            if (w <= 0 || h <= 0)
                return;

            var baseColor = BaseColor.ToSKColor();
            var highlight = HighlightColor.ToSKColor();

            using var paint = new SKPaint { IsAntialias = true };

            var rect = new SKRect(0, 0, w, h);
            var rr = new SKRoundRect(rect, CornerRadius, CornerRadius);

            paint.Shader = null;
            paint.Color = baseColor;
            canvas.DrawRoundRect(rr, paint);

            if (!IsActive)
                return;

            var shimmerWidth = w * 0.55f;
            var start = (-shimmerWidth) + (w + shimmerWidth * 2f) * _phase;
            var end = start + shimmerWidth;

            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(start, 0),
                new SKPoint(end, 0),
                new[] { baseColor, highlight, baseColor },
                new[] { 0f, 0.5f, 1f },
                SKShaderTileMode.Clamp);

            canvas.Save();
            canvas.ClipRoundRect(rr, SKClipOperation.Intersect, true);
            canvas.DrawRect(rect, paint);
            canvas.Restore();
        }
    }
}
