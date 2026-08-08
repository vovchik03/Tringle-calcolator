using System;

namespace Tringle_calcolator
{
    /// <summary>
    /// Математичне ядро. Не залежить від UI.
    /// 
    /// Угода про імена:
    ///   sideAB (c) — навпроти кута C
    ///   sideBC (a) — навпроти кута A
    ///   sideCA (b) — навпроти кута B
    /// </summary>
    public static class TriangleCalculator
    {
        public static TriangleResult? TryCalculate(
            double? sideAB, double? sideBC, double? sideCA,
            double? angleA, double? angleB, double? angleC)
        {
            // 1. Якщо є два кути — знаходимо третій
            (angleA, angleB, angleC) = FillThirdAngle(angleA, angleB, angleC);

            if (angleA.HasValue && angleB.HasValue && angleC.HasValue)
                if (!AnglesAreValid(angleA.Value, angleB.Value, angleC.Value))
                    return null;

            // Для зручності мапимо змінні до стандартних математичних назв
            double? c = sideAB, a = sideBC, b = sideCA;
            double? A = angleA, B = angleB, C = angleC;

            // 2. Якщо відомі всі 3 кути та хоча б 1 сторона (Вирішує КСК, ККС)
            // Використовуємо Теорему Синусів
            if (A.HasValue && B.HasValue && C.HasValue)
            {
                if (a.HasValue)
                {
                    double ratio = a.Value / Math.Sin(ToRad(A.Value));
                    b ??= ratio * Math.Sin(ToRad(B.Value));
                    c ??= ratio * Math.Sin(ToRad(C.Value));
                }
                else if (b.HasValue)
                {
                    double ratio = b.Value / Math.Sin(ToRad(B.Value));
                    a ??= ratio * Math.Sin(ToRad(A.Value));
                    c ??= ratio * Math.Sin(ToRad(C.Value));
                }
                else if (c.HasValue)
                {
                    double ratio = c.Value / Math.Sin(ToRad(C.Value));
                    a ??= ratio * Math.Sin(ToRad(A.Value));
                    b ??= ratio * Math.Sin(ToRad(B.Value));
                }
            }

            // 3. СКС (Дві сторони і кут між ними)
            // Знаходимо третю сторону через Теорему Косинусів
            if (a.HasValue && b.HasValue && C.HasValue && !c.HasValue)
                c = Math.Sqrt(a.Value * a.Value + b.Value * b.Value - 2 * a.Value * b.Value * Math.Cos(ToRad(C.Value)));
            else if (b.HasValue && c.HasValue && A.HasValue && !a.HasValue)
                a = Math.Sqrt(b.Value * b.Value + c.Value * c.Value - 2 * b.Value * c.Value * Math.Cos(ToRad(A.Value)));
            else if (c.HasValue && a.HasValue && B.HasValue && !b.HasValue)
                b = Math.Sqrt(c.Value * c.Value + a.Value * a.Value - 2 * c.Value * a.Value * Math.Cos(ToRad(B.Value)));

            // 4. ССК (Дві сторони і кут не між ними - неоднозначний випадок)
            // Знаходимо другий кут через Теорему Синусів
            if (!a.HasValue || !b.HasValue || !c.HasValue)
            {
                if (A.HasValue && a.HasValue && b.HasValue && !B.HasValue) B = SafeAsin((b.Value * Math.Sin(ToRad(A.Value))) / a.Value);
                else if (A.HasValue && a.HasValue && c.HasValue && !C.HasValue) C = SafeAsin((c.Value * Math.Sin(ToRad(A.Value))) / a.Value);
                else if (B.HasValue && b.HasValue && a.HasValue && !A.HasValue) A = SafeAsin((a.Value * Math.Sin(ToRad(B.Value))) / b.Value);
                else if (B.HasValue && b.HasValue && c.HasValue && !C.HasValue) C = SafeAsin((c.Value * Math.Sin(ToRad(B.Value))) / b.Value);
                else if (C.HasValue && c.HasValue && a.HasValue && !A.HasValue) A = SafeAsin((a.Value * Math.Sin(ToRad(C.Value))) / c.Value);
                else if (C.HasValue && c.HasValue && b.HasValue && !B.HasValue) B = SafeAsin((b.Value * Math.Sin(ToRad(C.Value))) / c.Value);

                // Якщо ми знайшли новий кут, перераховуємо залишки
                (A, B, C) = FillThirdAngle(A, B, C);

                if (A.HasValue && B.HasValue && C.HasValue)
                {
                    if (!c.HasValue && a.HasValue) c = a.Value * Math.Sin(ToRad(C.Value)) / Math.Sin(ToRad(A.Value));
                    if (!a.HasValue && b.HasValue) a = b.Value * Math.Sin(ToRad(A.Value)) / Math.Sin(ToRad(B.Value));
                    if (!b.HasValue && c.HasValue) b = c.Value * Math.Sin(ToRad(B.Value)) / Math.Sin(ToRad(C.Value));
                }
            }

            // 5. ССС (Три сторони відомі)
            // Знаходимо всі відсутні кути через Теорему Косинусів
            if (a.HasValue && b.HasValue && c.HasValue)
            {
                double aVal = a.Value, bVal = b.Value, cVal = c.Value;
                A ??= Acos((bVal * bVal + cVal * cVal - aVal * aVal) / (2 * bVal * cVal));
                B ??= Acos((aVal * aVal + cVal * cVal - bVal * bVal) / (2 * aVal * cVal));
                C ??= Acos((aVal * aVal + bVal * bVal - cVal * cVal) / (2 * aVal * bVal));
            }

            // 6. Фінальна перевірка та повернення результату
            if (a.HasValue && b.HasValue && c.HasValue && A.HasValue && B.HasValue && C.HasValue)
            {
                var result = new TriangleResult(c.Value, a.Value, b.Value, A.Value, B.Value, C.Value);
                return IsPhysicallyValid(result) ? result : null;
            }

            return null;
        }

        public static bool HasEnoughData(
            double? sideAB, double? sideBC, double? sideCA,
            double? angleA, double? angleB, double? angleC)
        {
            (angleA, angleB, angleC) = FillThirdAngle(angleA, angleB, angleC);
            int sides = Count(sideAB, sideBC, sideCA);
            int angles = Count(angleA, angleB, angleC);

            if (sides == 3) return true;
            if (sides >= 2 && angles >= 1) return true;
            if (angles >= 2 && sides >= 1) return true;

            return false;
        }

        // ─────────────────────────────────────────────
        //  ДОПОМІЖНІ МЕТОДИ
        // ─────────────────────────────────────────────

        private static (double? a, double? b, double? c) FillThirdAngle(double? a, double? b, double? c)
        {
            if (!a.HasValue && b.HasValue && c.HasValue) a = 180.0 - b.Value - c.Value;
            else if (!b.HasValue && a.HasValue && c.HasValue) b = 180.0 - a.Value - c.Value;
            else if (!c.HasValue && a.HasValue && b.HasValue) c = 180.0 - a.Value - b.Value;
            return (a, b, c);
        }

        private static bool AnglesAreValid(double a, double b, double c)
            => a > 0 && b > 0 && c > 0 && Math.Abs(a + b + c - 180.0) < 0.01;

        private static bool IsPhysicallyValid(TriangleResult r)
        {
            if (r.SideAB <= 0 || r.SideBC <= 0 || r.SideCA <= 0) return false;
            if (r.AngleA <= 0 || r.AngleB <= 0 || r.AngleC <= 0) return false;
            if (r.SideAB + r.SideBC <= r.SideCA + 0.0001) return false;
            if (r.SideAB + r.SideCA <= r.SideBC + 0.0001) return false;
            if (r.SideBC + r.SideCA <= r.SideAB + 0.0001) return false;
            if (Math.Abs(r.AngleA + r.AngleB + r.AngleC - 180.0) > 0.1) return false;
            return true;
        }

        private static double? SafeAsin(double value)
        {
            if (value > 1.0 || value < -1.0) return null;
            return ToDeg(Math.Asin(value));
        }

        private static int Count(double? a, double? b, double? c)
            => (a.HasValue ? 1 : 0) + (b.HasValue ? 1 : 0) + (c.HasValue ? 1 : 0);
        private static double ToRad(double deg) => deg * Math.PI / 180.0;
        private static double ToDeg(double rad) => rad * 180.0 / Math.PI;
        private static double Acos(double x) => ToDeg(Math.Acos(Math.Clamp(x, -1.0, 1.0)));
    }
}