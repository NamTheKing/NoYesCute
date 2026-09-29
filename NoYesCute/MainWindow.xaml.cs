using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NoYesCute
{
    public partial class MainWindow : Window
    {
        // The NO button must land at least this far (in pixels) from the mouse.
        private const double MinDistanceFromMouse = 150;

        // Gap kept between the NO button and the YES button.
        private const double YesButtonPadding = 15;

        private const double MaxYesScale = 1.8;

        private static readonly string[] Taunts =
        {
            "Nope, try again!",
            "Too slow!",
            "Are you sure?",
            "Think again...",
            "The YES button looks nice, right?",
            "You can't catch me!",
            "Just click YES already!"
        };

        private readonly Random _random = new();
        private bool _buttonsPlaced;
        private int _dodgeCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ---------- Layout ----------

        private void PlayArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // YES always stays a bit left of the centre.
            double top = (PlayArea.ActualHeight - YesButton.Height) / 2;
            double centerX = PlayArea.ActualWidth / 2;
            Canvas.SetLeft(YesButton, centerX - YesButton.Width - 20);
            Canvas.SetTop(YesButton, top);

            if (!_buttonsPlaced)
            {
                // First time: put NO next to YES so it looks innocent.
                Canvas.SetLeft(NoButton, centerX + 20);
                Canvas.SetTop(NoButton, top);
                _buttonsPlaced = true;
            }
            else if (IsNoButtonOutOfBounds())
            {
                // Window got smaller and NO is now hidden -> bring it back inside.
                MoveNoButton();
            }
        }

        private bool IsNoButtonOutOfBounds()
        {
            double left = Canvas.GetLeft(NoButton);
            double top = Canvas.GetTop(NoButton);
            return left + NoButton.Width > PlayArea.ActualWidth
                || top + NoButton.Height > PlayArea.ActualHeight;
        }

        // ---------- NO button: run away! ----------

        private void NoButton_MouseEnter(object sender, MouseEventArgs e)
        {
            MoveNoButton();
        }

        private void NoButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If someone is fast enough to press it anyway, swallow the click
            // (so Click never fires) and escape again.
            e.Handled = true;
            MoveNoButton();
        }

        private void NoButton_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            // Touch screens don't have "hover", so dodge on touch instead.
            e.Handled = true;
            MoveNoButton();
        }

        private void MoveNoButton()
        {
            _dodgeCount++;

            // Grow YES first, so the new NO position avoids YES's *final* size.
            double yesScale = Math.Min(1 + _dodgeCount * 0.1, MaxYesScale);
            AnimateYesScale(yesScale);

            Point target = PickNewPosition(yesScale);
            AnimateDouble(NoButton, Canvas.LeftProperty, target.X, 250, new QuadraticEase { EasingMode = EasingMode.EaseOut });
            AnimateDouble(NoButton, Canvas.TopProperty, target.Y, 250, new QuadraticEase { EasingMode = EasingMode.EaseOut });

            HintText.Text = Taunts[_random.Next(Taunts.Length)];
        }

        /// <summary>
        /// Picks a random spot inside the canvas that is far from the mouse
        /// and does not overlap the YES button.
        /// </summary>
        private Point PickNewPosition(double yesScale)
        {
            double maxX = Math.Max(0, PlayArea.ActualWidth - NoButton.Width);
            double maxY = Math.Max(0, PlayArea.ActualHeight - NoButton.Height);

            Point mouse = Mouse.GetPosition(PlayArea);
            Rect yesRect = GetYesBounds(yesScale);

            Point candidate = new(_random.NextDouble() * maxX, _random.NextDouble() * maxY);

            // Random tries; if the window is tiny and nothing fits, keep the last try.
            for (int attempt = 0; attempt < 100; attempt++)
            {
                candidate = new Point(_random.NextDouble() * maxX, _random.NextDouble() * maxY);

                Rect noRect = new(candidate.X, candidate.Y, NoButton.Width, NoButton.Height);
                Point noCenter = new(candidate.X + NoButton.Width / 2, candidate.Y + NoButton.Height / 2);
                bool farFromMouse = (noCenter - mouse).Length >= MinDistanceFromMouse;

                if (farFromMouse && !noRect.IntersectsWith(yesRect))
                    return candidate;
            }

            return candidate;
        }

        /// <summary>
        /// Bounds of the YES button after it is scaled around its centre.
        /// </summary>
        private Rect GetYesBounds(double scale)
        {
            double width = YesButton.Width * scale;
            double height = YesButton.Height * scale;
            double centerX = Canvas.GetLeft(YesButton) + YesButton.Width / 2;
            double centerY = Canvas.GetTop(YesButton) + YesButton.Height / 2;

            Rect rect = new(centerX - width / 2, centerY - height / 2, width, height);
            rect.Inflate(YesButtonPadding, YesButtonPadding);
            return rect;
        }

        // ---------- YES button ----------

        private void AnimateYesScale(double scale)
        {
            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
            AnimateDouble(YesScale, ScaleTransform.ScaleXProperty, scale, 300, ease);
            AnimateDouble(YesScale, ScaleTransform.ScaleYProperty, scale, 300, ease);
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            QuestionText.Text = "YES!!! Best day ever! ♥";
            HintText.Text = " ";
            PlayArea.Visibility = Visibility.Collapsed;

            // 1) Fade the celebration in.
            CelebrationPanel.Visibility = Visibility.Visible;
            AnimateDouble(CelebrationPanel, OpacityProperty, 1, 600, null);

            // 2) Make the heart beat forever.
            var beat = new DoubleAnimation(1, 1.25, TimeSpan.FromMilliseconds(450))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            HeartScale.BeginAnimation(ScaleTransform.ScaleXProperty, beat);
            HeartScale.BeginAnimation(ScaleTransform.ScaleYProperty, beat);
        }

        // ---------- Helper ----------

        /// <summary>
        /// Animates a double property from its current value to <paramref name="to"/>.
        /// Leaving "From" empty makes WPF start from wherever the value is right now,
        /// so a new animation smoothly takes over from one that is still running.
        /// </summary>
        private static void AnimateDouble(IAnimatable target, DependencyProperty property,
                                          double to, int milliseconds, IEasingFunction? easing)
        {
            var animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(milliseconds))
            {
                EasingFunction = easing
            };
            target.BeginAnimation(property, animation);
        }
    }
}
