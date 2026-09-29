using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NoYesCute
{
    // The heart effects live in their own file. "partial" lets one class be split
    // across several files - the compiler glues them together.
    public partial class MainWindow
    {
        private const string HeartGlyph = "♥";

        private static readonly Brush[] HeartBrushes =
        {
            CreateBrush(0xE9, 0x1E, 0x63),
            CreateBrush(0xF0, 0x62, 0x92),
            CreateBrush(0xFF, 0x40, 0x81),
            CreateBrush(0xF4, 0x8F, 0xB1),
            CreateBrush(0xD8, 0x1B, 0x60),
            CreateBrush(0xFF, 0x80, 0xAB)
        };

        private readonly DispatcherTimer _heartTimer = new();

        // Settings for the hearts that float up from the bottom.
        // Calm while asking, a lot more (and bigger) after YES.
        private double _floatMinSize = 14;
        private double _floatMaxSize = 30;
        private double _floatOpacity = 0.35;

        private static Brush CreateBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze(); // frozen brushes are read-only and cheaper to render
            return brush;
        }

        // ---------- Floating hearts (rise from the bottom) ----------

        private void StartFloatingHearts()
        {
            _heartTimer.Interval = TimeSpan.FromMilliseconds(700);
            _heartTimer.Tick += (_, _) => SpawnFloatingHeart();
            _heartTimer.Start();
        }

        private void StartCelebrationHearts()
        {
            _floatMinSize = 18;
            _floatMaxSize = 50;
            _floatOpacity = 0.9;
            _heartTimer.Interval = TimeSpan.FromMilliseconds(120);
        }

        private void SpawnFloatingHeart()
        {
            double width = HeartLayer.ActualWidth;
            double height = HeartLayer.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            double size = RandomBetween(_floatMinSize, _floatMaxSize);
            var (heart, _, translate) = CreateHeart(size);

            Canvas.SetLeft(heart, RandomBetween(0, width - size));
            Canvas.SetTop(heart, height);
            HeartLayer.Children.Add(heart);

            var duration = TimeSpan.FromSeconds(RandomBetween(4, 7));

            // Rise from below the window to above it.
            var rise = new DoubleAnimation(height, -size * 1.5, duration);

            // Sway left and right like a balloon.
            double swayWidth = RandomBetween(10, 30);
            var sway = new DoubleAnimation(-swayWidth, swayWidth, TimeSpan.FromSeconds(RandomBetween(0.8, 1.6)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            translate.BeginAnimation(TranslateTransform.XProperty, sway);

            // When it's gone off the top: remove it (or the canvas fills up forever)
            // and stop the endless sway animation so its clock doesn't keep running.
            rise.Completed += (_, _) =>
            {
                translate.BeginAnimation(TranslateTransform.XProperty, null);
                HeartLayer.Children.Remove(heart);
            };
            // Subscribe to Completed *before* BeginAnimation: starting an animation
            // freezes it, and a frozen object can't get new event handlers.
            heart.BeginAnimation(Canvas.TopProperty, rise);

            // Fade in at the start, fade out near the top.
            heart.BeginAnimation(OpacityProperty, CreateFadeInOut(_floatOpacity, duration, fadeOutFrom: 0.7));
        }

        // ---------- Burst (explode out of a point) ----------

        private void HeartBurst(Point center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                double size = RandomBetween(16, 40);
                var (heart, scale, translate) = CreateHeart(size);

                // Place the heart so its middle is on the centre point.
                Canvas.SetLeft(heart, center.X - size / 2);
                Canvas.SetTop(heart, center.Y - size * 0.6);
                HeartLayer.Children.Add(heart);

                // Random direction (angle) and distance = a point on a circle:
                // x = cos(angle) * distance, y = sin(angle) * distance
                double angle = RandomBetween(0, 2 * Math.PI);
                double distance = RandomBetween(120, 340);
                var duration = TimeSpan.FromMilliseconds(RandomBetween(900, 1500));
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

                translate.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, Math.Cos(angle) * distance, duration) { EasingFunction = ease });
                translate.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, Math.Sin(angle) * distance, duration) { EasingFunction = ease });

                // Pop from small to full size.
                var grow = new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new BackEase() };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);

                var fade = CreateFadeInOut(1, duration, fadeOutFrom: 0.6);
                fade.Completed += (_, _) => HeartLayer.Children.Remove(heart);
                heart.BeginAnimation(OpacityProperty, fade);
            }
        }

        // ---------- Helpers ----------

        /// <summary>
        /// Creates a heart TextBlock with a Scale + Rotate + Translate transform,
        /// and returns the transforms we want to animate later.
        /// </summary>
        private (TextBlock Heart, ScaleTransform Scale, TranslateTransform Translate) CreateHeart(double size)
        {
            var scale = new ScaleTransform();
            var translate = new TranslateTransform();
            var transforms = new TransformGroup();
            transforms.Children.Add(scale);
            transforms.Children.Add(new RotateTransform(RandomBetween(-25, 25)));
            transforms.Children.Add(translate);

            var heart = new TextBlock
            {
                Text = HeartGlyph,
                FontSize = size,
                Foreground = HeartBrushes[_random.Next(HeartBrushes.Length)],
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = transforms
            };
            return (heart, scale, translate);
        }

        /// <summary>
        /// Opacity: 0 -> peak quickly, stay, then fade to 0 from <paramref name="fadeOutFrom"/>
        /// (a fraction 0..1 of the total time) until the end.
        /// </summary>
        private static DoubleAnimationUsingKeyFrames CreateFadeInOut(double peak, TimeSpan duration, double fadeOutFrom)
        {
            var fade = new DoubleAnimationUsingKeyFrames { Duration = duration };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(peak, KeyTime.FromPercent(0.1)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(peak, KeyTime.FromPercent(fadeOutFrom)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            return fade;
        }

        private double RandomBetween(double min, double max) => min + _random.NextDouble() * (max - min);
    }
}
