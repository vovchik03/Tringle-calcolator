using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Tringle_calcolator
{
    /// <summary>
    /// Анімація появи трикутника на канвасі:
    /// 1) контур "малюється" від першої точки полігону, букви вершин з'являються, коли лінія до них доходить;
    /// 2) проявляються заливка і glow;
    /// 3) малюється мітка прямого кута і проявляються поля на сторонах.
    ///
    /// "Малювання" лінії — пунктир з одним штрихом довжиною в лінію:
    /// StrokeDashOffset їде від довжини штриха до 0, і штрих поступово проходить усю лінію.
    /// Довжини в StrokeDashArray / StrokeDashOffset задаються в одиницях товщини лінії, а не в пікселях.
    /// </summary>
    internal sealed class TriangleDrawAnimator
    {
        // Фінальний вигляд трикутника
        public const double FillOpacity = 0.06;
        public const double GlowOpacity = 0.7;

        // Таймлайн: контур (+ букви) → заливка і glow → мітка прямого кута і поля на сторонах
        private static readonly TimeSpan StrokeDrawDuration = TimeSpan.FromMilliseconds(600);
        private static readonly TimeSpan LabelFadeDuration = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan DetailsBegin = StrokeDrawDuration + FadeInDuration;
        private static readonly TimeSpan DetailsDuration = TimeSpan.FromMilliseconds(200);

        private readonly TextBlock[] _vertexLabels;
        private readonly Border[] _sideBoxes;

        // Номер поточної анімації: Completed старої анімації не повинен чіпати нову
        private int _version;

        /// <param name="vertexLabels">Букви вершин у тому ж порядку, що й точки полігону.</param>
        /// <param name="sideBoxes">Поля вводу на сторонах трикутника.</param>
        public TriangleDrawAnimator(TextBlock[] vertexLabels, Border[] sideBoxes)
        {
            _vertexLabels = vertexLabels;
            _sideBoxes = sideBoxes;
        }

        /// <summary>
        /// Запускає анімацію з початку. Незавершену попередню спершу знімає —
        /// інакше нова анімація, поки чекає свого BeginTime, показувала б застигле значення старої.
        /// </summary>
        public void Play(Polygon polygon, Polyline? rightAngleMark)
        {
            int version = ++_version;
            ClearAnimations(polygon, rightAngleMark);

            var fill = (SolidColorBrush)polygon.Fill;
            var glow = (DropShadowEffect)polygon.Effect;

            // Поки анімація чекає свого BeginTime, видно звичайне значення властивості,
            // тому все, що з'являється пізніше, спершу ховаємо
            fill.Opacity = 0;
            glow.Opacity = 0;
            foreach (var label in _vertexLabels) label.Opacity = 0;
            foreach (var box in _sideBoxes) box.Opacity = 0;

            // 1. Контур і букви вершин
            var strokeEase = new CubicEase { EasingMode = EasingMode.EaseInOut };
            var pts = polygon.Points;
            double perimeter = PathLength(pts, closed: true);
            AnimateStrokeDrawing(polygon, perimeter, TimeSpan.Zero, StrokeDrawDuration, strokeEase);

            double passed = 0;
            for (int i = 0; i < pts.Count && i < _vertexLabels.Length; i++)
            {
                // Момент, коли лінія доходить до вершини: частка пройденого шляху,
                // перерахована через easing контуру (лінія рухається нерівномірно)
                var arrival = TimeSpan.FromTicks((long)(StrokeDrawDuration.Ticks * InverseEase(strokeEase, passed / perimeter)));
                _vertexLabels[i].BeginAnimation(UIElement.OpacityProperty, FadeIn(1, arrival, LabelFadeDuration));
                passed += (pts[(i + 1) % pts.Count] - pts[i]).Length;
            }

            // 2. Заливка і glow
            fill.BeginAnimation(Brush.OpacityProperty, FadeIn(FillOpacity, StrokeDrawDuration, FadeInDuration));
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, FadeIn(GlowOpacity, StrokeDrawDuration, FadeInDuration));

            // 3. Мітка прямого кута (якщо є) і поля на сторонах
            if (rightAngleMark is { Visibility: Visibility.Visible })
            {
                AnimateStrokeDrawing(rightAngleMark, PathLength(rightAngleMark.Points, closed: false),
                    DetailsBegin, DetailsDuration, new CubicEase { EasingMode = EasingMode.EaseInOut });
            }

            // Поля — остання анімація таймлайну, по її завершенню ставимо фінальний стан.
            // Одна анімація на всі поля: Completed спрацює для кожного, але перевірка версії пропустить повтори
            var boxesFade = FadeIn(1, DetailsBegin, DetailsDuration);
            boxesFade.Completed += (_, _) =>
            {
                if (version == _version) Finish(polygon, rightAngleMark);
            };
            foreach (var box in _sideBoxes)
                box.BeginAnimation(UIElement.OpacityProperty, boxesFade);
        }

        /// <summary>
        /// Знімає всі анімації і ставить фінальний вигляд.
        /// Викликається в кінці анімації і при малюванні без анімації (ресайз) — тоді обриває незавершену.
        /// </summary>
        public void Finish(Polygon polygon, Polyline? rightAngleMark)
        {
            _version++;
            ClearAnimations(polygon, rightAngleMark);

            polygon.Fill.Opacity = FillOpacity;
            ((DropShadowEffect)polygon.Effect).Opacity = GlowOpacity;
            foreach (var label in _vertexLabels) label.Opacity = 1;
            foreach (var box in _sideBoxes) box.Opacity = 1;
        }

        /// <summary>Знімає анімації з усіх елементів і прибирає пунктир — лінії знову суцільні.</summary>
        private void ClearAnimations(Polygon polygon, Polyline? rightAngleMark)
        {
            ClearStrokeDrawing(polygon);
            if (rightAngleMark != null) ClearStrokeDrawing(rightAngleMark);

            polygon.Fill.BeginAnimation(Brush.OpacityProperty, null);
            polygon.Effect.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            foreach (var label in _vertexLabels) label.BeginAnimation(UIElement.OpacityProperty, null);
            foreach (var box in _sideBoxes) box.BeginAnimation(UIElement.OpacityProperty, null);
        }

        private static DoubleAnimation FadeIn(double to, TimeSpan begin, TimeSpan duration) =>
            new DoubleAnimation(to, duration)
            {
                BeginTime = begin,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

        /// <summary>
        /// Анімує "малювання" лінії. До BeginTime лінія повністю схована (offset = довжина штриха).
        /// </summary>
        private static void AnimateStrokeDrawing(Shape shape, double length, TimeSpan begin, TimeSpan duration, IEasingFunction ease)
        {
            double dash = length / shape.StrokeThickness;
            shape.StrokeDashArray = new DoubleCollection { dash, dash };
            shape.StrokeDashOffset = dash;
            shape.BeginAnimation(Shape.StrokeDashOffsetProperty, new DoubleAnimation(dash, 0, duration)
            {
                BeginTime = begin,
                EasingFunction = ease
            });
        }

        private static void ClearStrokeDrawing(Shape shape)
        {
            shape.BeginAnimation(Shape.StrokeDashOffsetProperty, null);
            shape.StrokeDashArray = null;
            shape.StrokeDashOffset = 0;
        }

        private static double PathLength(PointCollection pts, bool closed)
        {
            double length = 0;
            int segments = closed ? pts.Count : pts.Count - 1;
            for (int i = 0; i < segments; i++)
                length += (pts[(i + 1) % pts.Count] - pts[i]).Length;
            return length;
        }

        /// <summary>
        /// Обернена функція до easing: за часткою пройденого шляху (0..1) повертає частку часу (0..1).
        /// Бінарний пошук — easing монотонний.
        /// </summary>
        private static double InverseEase(IEasingFunction ease, double progress)
        {
            double lo = 0, hi = 1;
            for (int i = 0; i < 20; i++)
            {
                double mid = (lo + hi) / 2;
                if (ease.Ease(mid) < progress) lo = mid;
                else hi = mid;
            }
            return (lo + hi) / 2;
        }
    }
}
