using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Fitness_App.UI.Skia
{
    public sealed class MeshGradientView : SKCanvasView
    {
        public static readonly BindableProperty ColorsProperty = BindableProperty.Create(
            nameof(Colors),
            typeof(string),
            typeof(MeshGradientView),
            "#FC5200,#FF6B35,#FF8C42,#2563EB",
            propertyChanged: (b, o, n) => ((MeshGradientView)b).OnColorsChanged());

        public static readonly BindableProperty IsAnimatedProperty = BindableProperty.Create(
            nameof(IsAnimated),
            typeof(bool),
            typeof(MeshGradientView),
            false,
            propertyChanged: (b, o, n) => ((MeshGradientView)b).OnIsAnimatedChanged());

        private float _phase;
        private bool _isTicking;
        private SKColor[] _gradientColors = Array.Empty<SKColor>();

        public string Colors
        {
            get => (string)GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }

        public bool IsAnimated
        {
            get => (bool)GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }

        public MeshGradientView()
        {
            EnableTouchEvents = false;
            OnColorsChanged();
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            EnsureTicking();
        }

        private void OnColorsChanged()
        {
            var colorString = Colors ?? "#FC5200,#FF6B35,#FF8C42,#2563EB";
            var colorValues = colorString.Split(',');
            
            _gradientColors = colorValues.Select(c =>
            {
                var color = Color.FromArgb(c.Trim());
                return color.ToSKColor();
            }).ToArray();

            InvalidateSurface();
        }

        private void OnIsAnimatedChanged()
        {
            EnsureTicking();
            InvalidateSurface();
        }

        private void EnsureTicking()
        {
            if (!IsAnimated || Handler is null)
            {
                _isTicking = false;
                return;
            }

            if (_isTicking)
                return;

            _isTicking = true;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(50), () =>
            {
                if (!IsAnimated || Handler is null)
                {
                    _isTicking = false;
                    return false;
                }

                _phase += 0.01f;
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
            if (w <= 0 || h <= 0 || _gradientColors.Length < 2)
                return;

            using var paint = new SKPaint 
            { 
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };

            // Create animated mesh gradient effect
            var rect = new SKRect(0, 0, w, h);

            if (_gradientColors.Length >= 4)
            {
                // Create a complex mesh gradient with 4+ colors
                var centerX = w / 2f;
                var centerY = h / 2f;
                
                // Animate positions slightly
                var offset1 = _phase * MathF.PI * 2;
                var offset2 = (_phase + 0.33f) * MathF.PI * 2;
                var offset3 = (_phase + 0.66f) * MathF.PI * 2;

                var radius = Math.Max(w, h);

                // Create multiple overlapping radial gradients for mesh effect
                using var layer = new SKPaint { BlendMode = SKBlendMode.Screen };
                
                // Gradient 1
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(centerX + MathF.Cos(offset1) * w * 0.2f, centerY + MathF.Sin(offset1) * h * 0.2f),
                    radius * 0.7f,
                    new[] { _gradientColors[0], SKColors.Transparent },
                    new[] { 0f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(rect, paint);

                // Gradient 2
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(centerX + MathF.Cos(offset2) * w * 0.3f, centerY + MathF.Sin(offset2) * h * 0.3f),
                    radius * 0.6f,
                    new[] { _gradientColors[1], SKColors.Transparent },
                    new[] { 0f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(rect, paint);

                // Gradient 3
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(centerX + MathF.Cos(offset3) * w * 0.25f, centerY + MathF.Sin(offset3) * h * 0.25f),
                    radius * 0.65f,
                    new[] { _gradientColors[2], SKColors.Transparent },
                    new[] { 0f, 1f },
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(rect, paint);

                // Base gradient overlay
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(w, h),
                    _gradientColors,
                    null,
                    SKShaderTileMode.Clamp);
                paint.Color = paint.Color.WithAlpha(128);
                canvas.DrawRect(rect, paint);
            }
            else
            {
                // Simple linear gradient
                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(w, h),
                    _gradientColors,
                    null,
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(rect, paint);
            }
        }
    }
}
