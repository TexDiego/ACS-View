using Microsoft.Maui.Controls.Shapes;

namespace ACS_View.Controls
{
    public sealed class ShimmerSkeletonRow : ContentView
    {
        public static readonly BindableProperty ShowTrailingBlockProperty =
            BindableProperty.Create(nameof(ShowTrailingBlock), typeof(bool), typeof(ShimmerSkeletonRow), false, propertyChanged: OnLayoutPropertyChanged);

        public static readonly BindableProperty ShowSecondaryLineProperty =
            BindableProperty.Create(nameof(ShowSecondaryLine), typeof(bool), typeof(ShimmerSkeletonRow), true, propertyChanged: OnLayoutPropertyChanged);

        private readonly BoxView _shine = new()
        {
            WidthRequest = 96,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill,
            Opacity = 0.55,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                {
                    new GradientStop(Colors.Transparent, 0f),
                    new GradientStop(ThemeColors.SkeletonShine, 0.5f),
                    new GradientStop(Colors.Transparent, 1f)
                }
            }
        };

        public ShimmerSkeletonRow()
        {
            BuildContent();
            Loaded += (_, _) => StartShimmer();
            Unloaded += (_, _) => this.CancelAnimations();
        }

        public bool ShowTrailingBlock
        {
            get => (bool)GetValue(ShowTrailingBlockProperty);
            set => SetValue(ShowTrailingBlockProperty, value);
        }

        public bool ShowSecondaryLine
        {
            get => (bool)GetValue(ShowSecondaryLineProperty);
            set => SetValue(ShowSecondaryLineProperty, value);
        }

        private static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ShimmerSkeletonRow)bindable).BuildContent();
        }

        private void BuildContent()
        {
            var titleLine = CreatePill(180, 18);
            var secondaryLine = CreatePill(120, 13);
            secondaryLine.IsVisible = ShowSecondaryLine;

            var textStack = new VerticalStackLayout
            {
                Spacing = 10,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    titleLine,
                    secondaryLine
                }
            };

            var root = new Grid
            {
                Padding = new Thickness(10),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                BackgroundColor = ThemeColors.Surface
            };

            root.Add(textStack, 0, 0);

            if (ShowTrailingBlock)
            {
                root.Add(CreatePill(48, 48), 1, 0);
            }

            root.Add(_shine, 0, 0);
            Grid.SetColumnSpan(_shine, 2);

            Content = new Border
            {
                Margin = new Thickness(0, 5),
                Stroke = ThemeColors.BorderSoft,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = ThemeColors.Surface,
                Content = root
            };
        }

        private static BoxView CreatePill(double width, double height)
        {
            return new BoxView
            {
                WidthRequest = width,
                HeightRequest = height,
                CornerRadius = 8,
                HorizontalOptions = LayoutOptions.Start,
                Color = ThemeColors.SkeletonFill
            };
        }

        private void StartShimmer()
        {
            this.CancelAnimations();
            _shine.TranslationX = -120;

            new Animation(value => _shine.TranslationX = value, -120, 420)
                .Commit(this, "Shimmer", 16, 1200, Easing.CubicInOut, repeat: () => IsVisible);
        }
    }
}
