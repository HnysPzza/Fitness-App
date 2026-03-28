using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Fitness_App.UI.Skia;

/// <summary>
/// Premium athletic runner loading animation – fluid easing, glow trails, bounce, particles.
/// </summary>
public sealed class RunnerLoadingView : SKCanvasView
{
    // ── Bindable ─────────────────────────────────────────────────────────────
    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(RunnerLoadingView),
            Color.FromArgb("#FC5200"),
            propertyChanged: (b, _, _) => ((RunnerLoadingView)b).InvalidateSurface());

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    // ── State ─────────────────────────────────────────────────────────────────
    private float _t;          // master clock [0, 2π)
    private float _trailT;     // separate phase for fading speed lines
    private bool  _running;

    // simple particle pool
    private record struct Particle(float X, float Y, float Vx, float Vy, float Life, float MaxLife, float Size);
    private const int MaxParticles = 18;
    private readonly Particle[] _particles = new Particle[MaxParticles];
    private int _nextParticle;
    private float _particleSpawn;

    public RunnerLoadingView()
    {
        EnableTouchEvents = false;
        HeightRequest     = 180;
        WidthRequest      = 180;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null && !_running)
            StartLoop();
        else
            _running = false;
    }

    private void StartLoop()
    {
        _running = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(14), () =>
        {
            if (Handler is null) { _running = false; return false; }

            const float dt = 0.042f;
            _t       = (_t + dt) % (MathF.PI * 2f);
            _trailT  = (_trailT + dt * 0.7f) % (MathF.PI * 2f);

            // Spawn ground-dust particles periodically
            _particleSpawn += dt;
            if (_particleSpawn > 0.22f)
            {
                _particleSpawn = 0f;
                SpawnParticle();
            }

            // Advance live particles
            for (int i = 0; i < MaxParticles; i++)
            {
                ref var p = ref _particles[i];
                if (p.Life <= 0f) continue;
                p = p with
                {
                    X    = p.X + p.Vx,
                    Y    = p.Y + p.Vy,
                    Life = p.Life - 0.016f
                };
            }

            InvalidateSurface();
            return true;
        });
    }

    private void SpawnParticle()
    {
        ref var p = ref _particles[_nextParticle % MaxParticles];
        _nextParticle++;
        // Will be positioned at foot-strike below ground line; w/h resolved at paint time
        p = new Particle(
            X:       0f,        // overridden at paint using canvas info
            Y:       0f,
            Vx:      -1.4f - Random.Shared.NextSingle() * 1.8f,
            Vy:      -0.6f - Random.Shared.NextSingle() * 0.5f,
            Life:    0.55f + Random.Shared.NextSingle() * 0.35f,
            MaxLife: 0.9f,
            Size:    2.2f + Random.Shared.NextSingle() * 3.5f);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        float W = e.Info.Width;
        float H = e.Info.Height;
        if (W <= 0 || H <= 0) return;

        float s   = Math.Min(W, H) / 200f;   // universal scale
        float cx  = W * 0.5f;
        float gY  = H * 0.80f;               // ground line Y

        var accent = AccentColor.ToSKColor();

        // ── 1. Ground glow ────────────────────────────────────────────────────
        DrawGround(canvas, cx, gY, W, accent, s);

        // ── 2. Speed lines ───────────────────────────────────────────────────
        DrawSpeedLines(canvas, cx, gY, W, accent, s);

        // ── 3. Shadow under runner ───────────────────────────────────────────
        DrawShadow(canvas, cx, gY, s, accent);

        // ── 4. Particles ─────────────────────────────────────────────────────
        DrawParticles(canvas, cx, gY, s, accent);

        // ── 5. Runner body ────────────────────────────────────────────────────
        DrawRunner(canvas, cx, gY, s, accent);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Ground
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawGround(SKCanvas canvas, float cx, float gY, float W,
                             SKColor accent, float s)
    {
        // Glowing horizontal bar
        using var glow = new SKPaint { IsAntialias = true };
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(cx - 70f * s, gY),
            new SKPoint(cx + 70f * s, gY),
            [new SKColor(accent.Red, accent.Green, accent.Blue, 0),
             new SKColor(accent.Red, accent.Green, accent.Blue, 90),
             new SKColor(accent.Red, accent.Green, accent.Blue, 0)],
            SKShaderTileMode.Clamp);
        glow.Shader = shader;
        glow.StrokeWidth = 2f * s;
        glow.Style = SKPaintStyle.Stroke;
        canvas.DrawLine(cx - 70f * s, gY, cx + 70f * s, gY, glow);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Speed / motion lines
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawSpeedLines(SKCanvas canvas, float cx, float gY,
                                 float W, SKColor accent, float s)
    {
        using var p = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeCap   = SKStrokeCap.Round,
        };

        // Widths and vertical offsets for 4 streaks
        float[] widths   = [38f, 55f, 28f, 45f];
        float[] yOff     = [  0f, -8f, -20f, -36f];
        float[] alphaOff = [0f, 0.6f, 1.1f, 1.8f];
        float[] thick    = [2.5f, 1.8f, 1.4f, 1.1f];

        float shift = (_trailT / (MathF.PI * 2f)) * 28f;

        for (int i = 0; i < 4; i++)
        {
            float alpha = 0.28f + 0.22f * MathF.Sin(_trailT + alphaOff[i]);
            p.Color       = new SKColor(accent.Red, accent.Green, accent.Blue,
                                        (byte)(alpha * 200));
            p.StrokeWidth = thick[i] * s;

            float x2 = cx - 12f * s - (shift % 28f) * s;
            float x1 = x2 - widths[i] * s;
            float y  = gY + yOff[i] * s - 14f * s;     // offset from torso
            canvas.DrawLine(x1, y, x2, y, p);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shadow (ellipse below runner, pulses slightly)
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawShadow(SKCanvas canvas, float cx, float gY, float s,
                              SKColor accent)
    {
        float scaleX = 0.9f + 0.1f * MathF.Sin(_t);
        using var p = new SKPaint { IsAntialias = true };
        using var sh = SKShader.CreateRadialGradient(
            new SKPoint(cx, gY + 2f * s),
            38f * s * scaleX,
            [new SKColor(accent.Red, accent.Green, accent.Blue, 55),
             new SKColor(0, 0, 0, 0)],
            SKShaderTileMode.Clamp);
        p.Shader = sh;
        canvas.DrawOval(cx, gY + 2f * s, 38f * s * scaleX, 7f * s, p);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Ground-dust particles
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawParticles(SKCanvas canvas, float cx, float gY, float s,
                                SKColor accent)
    {
        using var p = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

        for (int i = 0; i < MaxParticles; i++)
        {
            ref var part = ref _particles[i];
            if (part.Life <= 0f) continue;

            // Lazily set spawn position the first frame
            if (part.X == 0f && part.Y == 0f)
            {
                part = part with { X = cx + 4f * s, Y = gY };
            }

            float alpha = (part.Life / part.MaxLife);
            p.Color = new SKColor(accent.Red, accent.Green, accent.Blue,
                                   (byte)(alpha * 140));
            canvas.DrawCircle(part.X, part.Y, part.Size * s * alpha, p);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Athletic Runner  (bezier-eased limbs, glowing joints, bouncing torso)
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawRunner(SKCanvas canvas, float cx, float gY, float s,
                             SKColor accent)
    {
        // Ease function: smooth in-out on each limb cycle
        float EaseInOut(float x) => x < 0.5f
            ? 4f * x * x * x
            : 1f - MathF.Pow(-2f * x + 2f, 3f) / 2f;

        // Normalize [0,1] across a full stride (one full 2π cycle)
        float stride = (_t / (MathF.PI * 2f));            // 0→1
        float strideB = ((stride + 0.5f) % 1f);           // opposite leg

        float eA = EaseInOut(stride);
        float eB = EaseInOut(strideB);

        // Bobbing torso: slight vertical bounce + forward lean oscillation
        float bounce  = -6f * MathF.Abs(MathF.Sin(_t)) * s;   // always upward
        float lean    =  6f * s;                                // constant forward lean

        // Key body points
        float headY   = gY - 88f * s + bounce;
        float headX   = cx + lean * 0.3f;
        float neckX   = cx + lean * 0.3f;
        float neckY   = headY + 14f * s;
        float hipX    = cx;
        float hipY    = gY - 44f * s + bounce * 0.5f;

        // ── Torso line ────────────────────────────────────────────────────────
        using var bodyStroke = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = 4.5f * s,
            StrokeCap   = SKStrokeCap.Round,
            Color       = accent
        };
        canvas.DrawLine(neckX, neckY, hipX, hipY, bodyStroke);

        // ── Head ──────────────────────────────────────────────────────────────
        // Glow halo
        using var headGlow = new SKPaint { IsAntialias = true };
        using var haloSh = SKShader.CreateRadialGradient(
            new SKPoint(headX, headY),
            20f * s,
            [new SKColor(accent.Red, accent.Green, accent.Blue, 60), SKColors.Transparent],
            SKShaderTileMode.Clamp);
        headGlow.Shader = haloSh;
        canvas.DrawCircle(headX, headY, 20f * s, headGlow);

        // Head solid
        using var headFill = new SKPaint { IsAntialias = true, Color = accent };
        canvas.DrawCircle(headX, headY, 9f * s, headFill);

        // ── Arms (cubic bezier path) ──────────────────────────────────────────
        using var armPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = 3.5f * s,
            StrokeCap   = SKStrokeCap.Round,
            Color       = new SKColor(accent.Red, accent.Green, accent.Blue, 220)
        };

        // Shoulder position
        float shoulderX = neckX;
        float shoulderY = neckY + 5f * s;

        // Arm A (forward swing)
        float armAAngle = -0.5f + eA * 1.1f;      // -0.5 → 0.6 rad
        float elbowAX = shoulderX + MathF.Cos(armAAngle) * 20f * s;
        float elbowAY = shoulderY + MathF.Sin(armAAngle + 0.6f) * 20f * s;
        float handAX  = elbowAX + MathF.Cos(armAAngle + 0.3f) * 18f * s;
        float handAY  = elbowAY + MathF.Sin(armAAngle + 0.5f) * 18f * s;

        using var pathA = new SKPath();
        pathA.MoveTo(shoulderX, shoulderY);
        pathA.CubicTo(elbowAX - 3f * s, elbowAY,
                      elbowAX,          elbowAY,
                      handAX,           handAY);
        canvas.DrawPath(pathA, armPaint);

        // Arm B (back swing)
        float armBAngle = 0.5f - eB * 1.1f;
        float elbowBX = shoulderX + MathF.Cos(armBAngle + MathF.PI) * 18f * s;
        float elbowBY = shoulderY + MathF.Sin(armBAngle + MathF.PI + 0.4f) * 18f * s;
        float handBX  = elbowBX + MathF.Cos(armBAngle + MathF.PI + 0.3f) * 16f * s;
        float handBY  = elbowBY + MathF.Sin(armBAngle + MathF.PI + 0.5f) * 16f * s;

        using var pathB = new SKPath();
        pathB.MoveTo(shoulderX, shoulderY);
        pathB.CubicTo(elbowBX + 3f * s, elbowBY,
                      elbowBX,           elbowBY,
                      handBX,            handBY);
        canvas.DrawPath(pathB, armPaint);

        // ── Legs ─────────────────────────────────────────────────────────────
        using var legPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = 4.5f * s,
            StrokeCap   = SKStrokeCap.Round,
            Color       = accent
        };

        DrawLeg(canvas, hipX, hipY, gY, s, eA,  lean,  legPaint);   // leg A
        DrawLeg(canvas, hipX, hipY, gY, s, eB, -lean,  legPaint);   // leg B

        // ── Glowing joint dots ────────────────────────────────────────────────
        using var dot = new SKPaint { IsAntialias = true };
        void GlowDot(float x, float y, float r, byte alpha)
        {
            using var dSh = SKShader.CreateRadialGradient(
                new SKPoint(x, y), r * 2.5f,
                [new SKColor(accent.Red, accent.Green, accent.Blue, alpha),
                 SKColors.Transparent],
                SKShaderTileMode.Clamp);
            dot.Shader = dSh;
            canvas.DrawCircle(x, y, r * 2.5f, dot);
        }

        GlowDot(shoulderX, shoulderY, 3.5f * s, 120);
        GlowDot(hipX,      hipY,      4f * s,   110);
    }

    private static void DrawLeg(SKCanvas canvas,
                                  float hipX, float hipY, float gY, float s,
                                  float ease, float xBias,
                                  SKPaint legPaint)
    {
        // Thigh angle: forward kick (ease near 1) → behind (ease near 0)
        float thighAngle = MathF.PI * 0.5f + (ease - 0.5f) * 1.5f;

        float kneeX = hipX + xBias * 0.2f + MathF.Cos(thighAngle) * 26f * s;
        float kneeY = hipY + MathF.Sin(thighAngle) * 26f * s;

        // Shin folds back when leg is behind, extends when forward
        float shinFold = (1f - ease) * 0.9f;   // 0 = straight, 0.9 = folded
        float shinAngle = thighAngle + 0.4f + shinFold * 1.2f;

        float footX = kneeX + MathF.Cos(shinAngle) * 24f * s;
        float footY = Math.Min(kneeY + MathF.Sin(shinAngle) * 24f * s, gY);

        // Thigh (upper leg)
        using var thighPath = new SKPath();
        thighPath.MoveTo(hipX, hipY);
        thighPath.CubicTo(hipX + (kneeX - hipX) * 0.4f,
                           hipY + (kneeY - hipY) * 0.1f,
                           hipX + (kneeX - hipX) * 0.6f,
                           hipY + (kneeY - hipY) * 0.9f,
                           kneeX, kneeY);
        canvas.DrawPath(thighPath, legPaint);

        // Shin (lower leg)
        using var shinPath = new SKPath();
        shinPath.MoveTo(kneeX, kneeY);
        shinPath.CubicTo(kneeX + (footX - kneeX) * 0.3f,
                          kneeY + (footY - kneeY) * 0.2f,
                          kneeX + (footX - kneeX) * 0.7f,
                          kneeY + (footY - kneeY) * 0.8f,
                          footX, footY);
        canvas.DrawPath(shinPath, legPaint);
    }
}
