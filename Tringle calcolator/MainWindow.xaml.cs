using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace Tringle_calcolator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        // ─────────────────────────────────────────────
        //  СТАН
        // ─────────────────────────────────────────────

        // Єдине джерело даних — всі 6 полів
        // null = поле порожнє або невалідне
        private double? _sideAB, _sideBC, _sideCA;
        private double? _angleA, _angleB, _angleC;

        // Чи є активним режим прямокутного трикутника
        private bool _isRightTriangleMode = false;

        // Захист від зациклення при програмному заповненні TextBox-ів
        private bool _suppressTextChanged = false;

        // Полігон трикутника на канвасі мітка прямого кута
        private Polygon? _trianglePolygon;
        private Polyline? _rightAngleMark;
        // ─────────────────────────────────────────────
        //  ІНІЦІАЛІЗАЦІЯ
        // ─────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            // Підписка на події TextBox лівої панелі
            SubscribeTextBox(ABTextBox, v => _sideAB = v);
            SubscribeTextBox(BCTextBox, v => _sideBC = v);
            SubscribeTextBox(CATextBox, v => _sideCA = v);
            SubscribeTextBox(ATextBox, v => _angleA = v);
            SubscribeTextBox(BTextBox, v => _angleB = v);
            SubscribeTextBox(CTextBox, v => _angleC = v);
            // Підписка на події TextBox на канвасі для синхронізації з лівою панеллю
            SyncCanvasBox(CanvasABBox, ABTextBox);
            SyncCanvasBox(CanvasBCBox, BCTextBox);
            SyncCanvasBox(CanvasCABox, CATextBox);

            // Кнопки режиму
            TriangleDovilnyi.Click += (_, _) => SetMode(rightTriangle: false);
            TrianglePriamokutnyi.Click += (_, _) => SetMode(rightTriangle: true);

            // Канвас
            CANVAS.Loaded += (_, _) => DrawDefaultTriangle();
            CANVAS.SizeChanged += (_, _) => RedrawCurrentTriangle();

            // Початковий стан кнопки Enter — сіра, неактивна
            UpdateEnterButton(ready: false);
        }

        // ─────────────────────────────────────────────
        //  ПІДПИСКА НА TEXTBOX
        // ─────────────────────────────────────────────

        /// <summary>
        /// Підписує TextBox на TextChanged.
        /// При зміні — парсить значення, зберігає в змінну через setter,
        /// потім перевіряє чи достатньо даних.
        /// </summary>
        /// 
        private readonly Dictionary<TextBox, Action<double?>> _boxSetters = new();
        private void SubscribeTextBox(TextBox box, Action<double?> setter)
        {
            _boxSetters[box] = setter;
            box.TextChanged += (_, _) =>
            {
                if (_suppressTextChanged) return;
                setter(ParseBox(box));
                OnAnyInputChanged();
            };
        }


        // ─────────────────────────────────────────────
        //  РЕАКЦІЯ НА ЗМІНУ БУДЬ-ЯКОГО ПОЛЯ
        // ─────────────────────────────────────────────

        private void OnAnyInputChanged()
        {
            //ClearAll(); // Очищуємо результат при будь-якій зміні
            bool ready = TriangleCalculator.HasEnoughData(
                _sideAB, _sideBC, _sideCA,
                _angleA, _angleB, _angleC);

            UpdateEnterButton(ready);
        }


        /// <summary>
        /// Двостороння синхронізація: канвас ↔ ліва панель
        /// </summary>
        private void SyncCanvasBox(TextBox canvasBox, TextBox panelBox)
        {
            // Канвас → панель
            canvasBox.TextChanged += (_, _) =>
            {
                if (_suppressTextChanged) return;
                _suppressTextChanged = true;
                panelBox.Text = canvasBox.Text;
                _suppressTextChanged = false;
                // Тригеримо OnAnyInputChanged вручну бо panelBox.TextChanged заблокований
                var setter = _boxSetters[panelBox];
                setter(ParseBox(panelBox));
                OnAnyInputChanged();
            };

            // Панель → канвас
            panelBox.TextChanged += (_, _) =>
            {
                if (_suppressTextChanged) return;
                _suppressTextChanged = true;
                canvasBox.Text = panelBox.Text;
                _suppressTextChanged = false;
            };
        }


        // ─────────────────────────────────────────────
        //  НАТИСКАННЯ ENTER
        // ─────────────────────────────────────────────

        private void OnEnterClick(object sender, RoutedEventArgs e)
        {
            
            ComputeAndDraw();
        }

        // Enter з клавіатури — спрацьовує якщо фокус у будь-якому TextBox, Space - очищення даних
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Enter && EnterButton.IsEnabled)
                ComputeAndDraw();

            if (e.Key == Key.Space)
                ClearAll();
        }

        private void ComputeAndDraw()
        {
            //ClearAll();
            TriangleResult? result = TriangleCalculator.TryCalculate(
                _sideAB, _sideBC, _sideCA,
                _angleA, _angleB, _angleC);

            if (result == null)
            {
                // Дані є але трикутник неможливий (наприклад сума кутів ≠ 180°)
                ShowError();
                return;
            }

            // Заповнюємо всі поля результатом
            FillResults(result);

            // Малюємо трикутник
            DrawTriangle(result);

            // Кнопка повертається в неактивний стан
            UpdateEnterButton(ready: false);
        }

        // ─────────────────────────────────────────────
        //  ЗАПОВНЕННЯ РЕЗУЛЬТАТІВ
        // ─────────────────────────────────────────────

        private void FillResults(TriangleResult r)
        {
            _suppressTextChanged = true;
            try
            {
                ABTextBox.Text = FormatValue(r.SideAB);
                BCTextBox.Text = FormatValue(r.SideBC);
                CATextBox.Text = FormatValue(r.SideCA);
                ATextBox.Text = FormatAngle(r.AngleA);
                BTextBox.Text = FormatAngle(r.AngleB);
                CTextBox.Text = FormatAngle(r.AngleC);

                // Оновлюємо внутрішній стан
                _sideAB = r.SideAB;
                _sideBC = r.SideBC;
                _sideCA = r.SideCA;
                _angleA = r.AngleA;
                _angleB = r.AngleB;
                _angleC = r.AngleC;
            }
            finally
            {
                _suppressTextChanged = false;
            }
        }

        // ─────────────────────────────────────────────
        //  РЕЖИМИ (довільний / прямокутний)
        // ─────────────────────────────────────────────

        private void SetMode(bool rightTriangle)
        {
            
            _isRightTriangleMode = rightTriangle;
            ClearAll();

            if (rightTriangle)
            {
                // Прямокутний: ∠C = 90°, поле заблоковане
                _suppressTextChanged = true;
                CTextBox.Text = "90";
                _suppressTextChanged = false;

                _angleC = 90.0;
                CTextBox.IsReadOnly = true;
                CTextBox.Opacity = 0.5;
            }
            else
            {
                // Довільний: ∠C очищається
                _suppressTextChanged = true;
                CTextBox.Text = string.Empty;
                _suppressTextChanged = false;

                _angleC = null;
                CTextBox.IsReadOnly = false;
                CTextBox.Opacity = 1.0;
            }

            OnAnyInputChanged();
        }

        // ─────────────────────────────────────────────
        //  МАЛЮВАННЯ
        // ─────────────────────────────────────────────

        /// <summary>
        /// Малює стартовий трикутник 3-4-5 (помаранчевий)
        /// </summary>
        private void DrawDefaultTriangle()
        {
            /* var defaultResult = TriangleCalculator.TryCalculate(3, 4, 5, null, null, null);

             if (defaultResult != null)
                 DrawTriangle(defaultResult, defaultColor: true);
            */

            var defaultResult = _isRightTriangleMode ? TriangleCalculator.TryCalculate(5, 4, 3, null, null, null) : TriangleCalculator.TryCalculate(6, 7, 5, null, null, null);
            if(defaultResult != null)
            {
                DrawTriangle(defaultResult, defaultColor: true);
            }
        }

        private void RedrawCurrentTriangle()
        {
            // Якщо є результат — перемальовуємо, інакше дефолтний
            if (_sideAB.HasValue && _sideBC.HasValue && _sideCA.HasValue &&
                _angleA.HasValue && _angleB.HasValue && _angleC.HasValue)
            {
                var r = new TriangleResult(
                    _sideAB.Value, _sideBC.Value, _sideCA.Value,
                    _angleA.Value, _angleB.Value, _angleC.Value);
                DrawTriangle(r);
            }
            else
            {
                DrawDefaultTriangle();
            }
        }

        /// <summary>
        /// Малює трикутник на канвасі за результатом розрахунку.
        /// Вершина A — вгорі зліва, B — вгорі справа, C — внизу зліва.
        /// </summary>
        /// 
        private static double ToRoad(double deg) => deg * Math.PI / 180.0;
        private void DrawTriangle(TriangleResult r, bool defaultColor = false)
        {
            if (CANVAS.ActualWidth <= 0 || CANVAS.ActualHeight <= 0) return;
            // DEFAULT COLOR SET UP HERE ----------------------------------------------------- DEFAULT COLOR SET UP HERE 
            Color fill = defaultColor
                ? Color.FromRgb(0xFF, 0x92, 0x27)
                : Color.FromRgb(0x33, 0xAA, 0xFF);

            const double eps = 0.01;

            double padding = Math.Min(CANVAS.ActualWidth, CANVAS.ActualHeight) * 0.2;
            double availW = CANVAS.ActualWidth - padding * 2;
            double availH = CANVAS.ActualHeight - padding * 2;

            double angleC_rad = r.AngleC * Math.PI / 180.0;
            double cosC = Math.Cos(angleC_rad);
            double sinC = Math.Sin(angleC_rad);

            double height = r.SideCA * sinC;
            double aXoffset = r.SideCA * cosC;  // від'ємне якщо ∠C > 90°

            double leftExtent = Math.Min(0, aXoffset);
            double rightExtent = Math.Max(r.SideBC, aXoffset);
            double totalWidth = rightExtent - leftExtent;

            double scale = Math.Min(availW / totalWidth, availH / height);
            double startX = padding + (availW - totalWidth * scale) / 2.0 - leftExtent * scale;
            double baseY = padding + availH;

            Point ptC = new Point(startX, baseY);
            Point ptB = new Point(startX + r.SideBC * scale, baseY);
            Point ptA = new Point(startX + aXoffset * scale, baseY - height * scale);

            if (height <= 0 || totalWidth <= 0) return;
            if (_trianglePolygon == null)
            {
                _trianglePolygon = new Polygon
                {
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(_trianglePolygon, 0);
                Canvas.SetTop(_trianglePolygon, 0);
                CANVAS.Children.Add(_trianglePolygon);
            }

            _trianglePolygon.Fill = new SolidColorBrush(fill);
            _trianglePolygon.Points = new PointCollection { ptA, ptB, ptC };

            if (Math.Abs(r.AngleA - 90) < eps)      DrawRightAngleMark(ptA, ptB, ptC, true);
            else if (Math.Abs(r.AngleB - 90) < eps) DrawRightAngleMark(ptB, ptA, ptC, true);
            else if (Math.Abs(r.AngleC - 90) < eps) DrawRightAngleMark(ptC, ptA, ptB, true);
            else                                    DrawRightAngleMark(default,default,default,default);

            PositionCanvasLabels(ptA, ptB, ptC);
        }

        private void DrawRightAngleMark(Point vertex, Point p1, Point p2, bool show)
        {
            if (_rightAngleMark == null)
            {
                _rightAngleMark = new Polyline
                {
                    Stroke = new SolidColorBrush(Colors.LightGray),
                    StrokeThickness = 1.5,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(_rightAngleMark, 0);
                Canvas.SetTop(_rightAngleMark, 0);
                CANVAS.Children.Add(_rightAngleMark);
            }
            if (!show)
            {
                _rightAngleMark.Visibility = Visibility.Collapsed;
                return;
            }

            _rightAngleMark.Visibility = Visibility.Visible;

            const double size = 12.0;
            Vector d1 = p1 - vertex; d1.Normalize();
            Vector d2 = p2 - vertex; d2.Normalize();

            _rightAngleMark.Points = new PointCollection
            {
                vertex + d1 * size,
                vertex + (d1 + d2) * size,
                vertex + d2 * size
            };
            Panel.SetZIndex(_rightAngleMark, 150);
        }

        private void PositionCanvasLabels(Point ptA, Point ptB, Point ptC)
        {
            const double labelOffset = 18.0;
            const double boxOffset = 12.0;

            // Підписи вершин — зміщуємо "назовні" від центру трикутника
            Point center = new Point(
                (ptA.X + ptB.X + ptC.X) / 3.0,
                (ptA.Y + ptB.Y + ptC.Y) / 3.0);

            PlaceLabel(LabelA, ptA, center, labelOffset);
            PlaceLabel(LabelB, ptB, center, labelOffset);
            PlaceLabel(LabelC, ptC, center, labelOffset);

            // Поля на серединах сторін
            PlaceBorder(CanvasABBorder, MidPoint(ptA, ptB), boxOffset);
            PlaceBorder(CanvasBCBorder, MidPoint(ptB, ptC), boxOffset);
            PlaceBorder(CanvasCАBorder, MidPoint(ptC, ptA), boxOffset);
        }

        private void PlaceLabel(TextBlock label, Point vertex, Point center, double offset)
        {
            // Напрямок від центру до вершини — туди зміщуємо підпис
            Vector dir = vertex - center;
            if (dir.Length > 0) dir.Normalize();

            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double x = vertex.X + dir.X * offset - label.DesiredSize.Width / 2.0;
            double y = vertex.Y + dir.Y * offset - label.DesiredSize.Height / 2.0;

            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);
            Panel.SetZIndex(label, 200);
        }

        private void PlaceBorder(Border border, Point mid, double offset)
        {
            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double x = mid.X - border.DesiredSize.Width / 2.0;
            double y = mid.Y - border.DesiredSize.Height / 2.0 - offset;

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            Panel.SetZIndex(border, 200);
        }

        private static Point MidPoint(Point p1, Point p2) =>
            new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);

        // ─────────────────────────────────────────────
        //  СТАН КНОПКИ ENTER
        // ─────────────────────────────────────────────

        private void UpdateEnterButton(bool ready)
        {
            if (EnterButton == null) return;
            EnterButton.IsEnabled = ready;   // все інше робить XAML
        }

        // ─────────────────────────────────────────────
        //  ПОМИЛКА
        // ─────────────────────────────────────────────

        private void ShowError()
        {
            // Підсвічуємо трикутник червоним якщо дані несумісні
            if (_trianglePolygon != null)
            {
                _trianglePolygon.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44));
                MessageBox.Show("INVALID");
            }
        }

        // ─────────────────────────────────────────────
        //  ДОПОМІЖНІ МЕТОДИ
        // ─────────────────────────────────────────────

        private static double? ParseBox(TextBox box)
        {
            string text = box.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text)) return null;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) && v > 0)
                return v;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v2) && v2 > 0)
                return v2;

            return null;
        }

        private static string FormatValue(double v) =>
            v.ToString("F4", CultureInfo.CurrentCulture);

        private static string FormatAngle(double v) =>
            v.ToString("F2", CultureInfo.CurrentCulture);

        /// <summary>
        /// Знаходить дочірній елемент у Visual Tree за іменем.
        /// Потрібно для зміни кольору KeyBody всередині шаблону кнопки.
        /// </summary>
        
        // ────────────────────────────────────────────
        // ОБНУЛЕННЯ ЗНАЧЕНЬ ПРИ ПЕРЕКЛЮЧЕННІ
        //─────────────────────────────────────────────

        private void ClearAll()
        {
            _suppressTextChanged = true;
            try
            {
                ABTextBox.Text = string.Empty;
                BCTextBox.Text = string.Empty;
                CATextBox.Text = string.Empty;
                ATextBox.Text = string.Empty;
                BTextBox.Text = string.Empty;
                CTextBox.Text = string.Empty;
                _sideAB = null;
                _sideBC = null;
                _sideCA = null;
                _angleA = null;
                _angleB = null;
                _angleC = null;
                CanvasABBox.Text = string.Empty;
                CanvasBCBox.Text = string.Empty;
                CanvasCABox.Text = string.Empty;
            }
            finally
            {
                _suppressTextChanged = false;
            }

            DrawDefaultTriangle();
            UpdateEnterButton(ready: false);
        }


    }
}