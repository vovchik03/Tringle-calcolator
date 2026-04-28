namespace Tringle_calcolator
{
    /// <summary>
    /// Математичне ядро. Не залежить від UI.
    /// 
    /// Угода про імена (відповідає TriangleResult):
    ///   sideAB  — сторона AB, навпроти кута C
    ///   sideBC  — сторона BC, навпроти кута A
    ///   sideCA  — сторона CA, навпроти кута B
    ///   angleA, angleB, angleC — кути в градусах
    /// </summary>
    public static class TriangleCalculator
    {
        // ─────────────────────────────────────────────
        //  ПУБЛІЧНИЙ МЕТОД — єдина точка входу
        // ─────────────────────────────────────────────

        public static TriangleResult? TryCalculate(
            double? sideAB, double? sideBC, double? sideCA,
            double? angleA, double? angleB, double? angleC)
        {
            // Якщо є два кути — знаходимо третій
            (angleA, angleB, angleC) = FillThirdAngle(angleA, angleB, angleC);

            // Перевірка валідності кутів якщо всі три відомі
            if (angleA.HasValue && angleB.HasValue && angleC.HasValue)
                if (!AnglesAreValid(angleA.Value, angleB.Value, angleC.Value))
                    return null;

            // Перебираємо всі можливі комбінації
            TriangleResult? result =
                // ССС
                TrySSS(sideAB, sideBC, sideCA) ??

                // СКС — кут між двома сторонами
                // ∠C між AB і CA → але ∠C навпроти AB, тому кут між BC і CA
                TrySAS_A(sideBC, angleC, sideCA) ??   // ∠C між BC і CA 
                TrySAS_B(sideCA, angleA, sideAB) ??   // ∠A між CA і AB  
                TrySAS_C(sideAB, angleB, sideBC) ??   // ∠B між AB і BC 
                                                      // ∠B між BC і АВ


                // КСК — сторона між двома кутами
                TryASA(angleA, sideAB, angleB) ??   // ∠A, AB, ∠B  → сторона AB між A і B
                TryASA(angleB, sideBC, angleC) ??   // ∠B, BC, ∠C
                TryASA(angleA, sideCA, angleC) ??   // ∠A, CA, ∠C

                // ККС — два кути і будь-яка сторона
                TryAAS(angleA, angleB, sideAB) ??
                TryAAS(angleA, angleB, sideBC) ??
                TryAAS(angleA, angleB, sideCA) ??
                TryAAS(angleA, angleC, sideAB) ??
                TryAAS(angleA, angleC, sideBC) ??
                TryAAS(angleA, angleC, sideCA) ??
                TryAAS(angleB, angleC, sideAB) ??
                TryAAS(angleB, angleC, sideBC) ??
                TryAAS(angleB, angleC, sideCA);

            if (result == null) return null;
            return IsPhysicallyValid(result) ? result : null;
        }

        // ─────────────────────────────────────────────
        //  ПЕРЕВІРКА ДОСТАТНОСТІ (без розрахунку)
        // ─────────────────────────────────────────────

        public static bool HasEnoughData(
            double? sideAB, double? sideBC, double? sideCA,
            double? angleA, double? angleB, double? angleC)
        {
            (angleA, angleB, angleC) = FillThirdAngle(angleA, angleB, angleC);

            int sides  = Count(sideAB, sideBC, sideCA);
            int angles = Count(angleA, angleB, angleC);

            if (sides == 3) return true;                    // ССС
            if (sides >= 2 && angles >= 1) return true;    // СКС
            if (angles >= 2 && sides >= 1) return true;    // КСК або ККС

            return false;
        }

        // ─────────────────────────────────────────────
        //  КОМБІНАЦІЇ
        // ─────────────────────────────────────────────

        // ССС: три сторони
        private static TriangleResult? TrySSS(double? ab, double? bc, double? ca)
        {
            if (!ab.HasValue || !bc.HasValue || !ca.HasValue) return null;

            double a = ab.Value, b = bc.Value, c = ca.Value;

            double cosA = (b * b + c * c - a * a) / (2 * b * c);  // кут навпроти AB
            // Стоп: AB — навпроти C, BC — навпроти A, CA — навпроти B
            // Перейменуємо для ясності:
            // p = sideAB (навпроти angleC)
            // q = sideBC (навпроти angleA)
            // r = sideCA (навпроти angleB)
            double p = ab.Value, q = bc.Value, r = ca.Value;

            double cosC = (q * q + r * r - p * p) / (2 * q * r);
            double cosA2 = (p * p + r * r - q * q) / (2 * p * r);
            double cosB = (p * p + q * q - r * r) / (2 * p * q);

            if (OutOfRange(cosA2) || OutOfRange(cosB) || OutOfRange(cosC)) return null;

            double angA = Acos(cosA2);
            double angB = Acos(cosB);
            double angC = Acos(cosC);

            return new TriangleResult(p, q, r, angA, angB, angC);
        }

        // СКС: дві сторони і кут між ними
        // side1 і side2 — дві відомі сторони, angleBetween — кут між ними
        // Третя сторона розраховується через теорему косинусів
        private static TriangleResult? TrySAS_A(double? bc, double? angleC,double? ca)
        {
            if (!bc.HasValue || !angleC.HasValue || !ca.HasValue) return null;

            // p = sideAB (навпроти angleC)
            // q = sideBC (навпроти angleA)
            // r = sideCA (навпроти angleB)
            double q = bc.Value, angC = angleC.Value, r = ca.Value;
            double p = Math.Sqrt(r * r + q * q - 2 * q * r * Math.Cos(ToRad(angC)));
            double rSquared = q * q + r * r - 2 * r * q * Math.Cos(ToRad(angC)) - p * p;
            //double rSquared = p * p + r * r - 2 * p * r * Math.Cos(ToRad(ang));
            if (rSquared != 0) return null;
            //double ab = Math.Sqrt(rSquared);
            //double cosB = (ab * ab + bc.Value * bc.Value - ca.Value * ca.Value) / (2 * ab * bc.Value);
            //double cosC = (ab * ab + ca.Value * ca.Value - bc.Value * bc.Value) / (2 * ab * ca.Value);
            double cosA = (p * p + r * r - q * q) / (2 * p * r);  // ∠A навпроти BC
            double cosB = (p * p + q * q - r * r) / (2 * p * q);  // ∠B навпроти CA
            if (OutOfRange(cosA) || OutOfRange(cosB)) return null;

            return new TriangleResult(p, q, r, Acos(cosA), Acos(cosB), angC);
        } 
        private static TriangleResult? TrySAS_B(double? ca, double? angleA, double? ab)
        {
            if (!ca.HasValue || !angleA.HasValue || !ab.HasValue) return null;
            double r = ca.Value, ang = angleA.Value, p = ab.Value;
            double pSquared = r * r + p * p - 2 * r * p * Math.Cos(ToRad(ang));
            if (pSquared <= 0) return null;
            double bc = Math.Sqrt(pSquared);
            double cosA = (p * p + bc * bc - r * r) / (2 * p * bc);
            double cosC = (r * r + bc * bc - p * p) / (2 * r * bc);
            if (OutOfRange(cosA) || OutOfRange(cosC)) return null;
            return new TriangleResult(p, bc, r, Acos(cosA), ang, Acos(cosC));
        }
        private static TriangleResult? TrySAS_C(double? ab, double? angleC, double? ca)
        {
            if (!ab.HasValue || !angleC.HasValue || !ca.HasValue) return null;
            double p = ab.Value, ang = angleC.Value, r = ca.Value;
            double pSquared = r * r + p * p - 2 * r * p * Math.Cos(ToRad(ang));
            if (pSquared <= 0) return null;
            double bc = Math.Sqrt(pSquared);
            double cosA = (p * p + bc * bc - r * r) / (2 * p * bc);
            double cosB = (r * r + bc * bc - p * p) / (2 * r * bc);
            if (OutOfRange(cosA) || OutOfRange(cosB)) return null;
            return new TriangleResult(p, bc, r, Acos(cosA), Acos(cosB), ang);
        }
        // КСК: два кути і сторона між ними
        private static TriangleResult? TryASA(double? ang1, double? sideBetween, double? ang2)
        {
            if (!ang1.HasValue || !sideBetween.HasValue || !ang2.HasValue) return null;

            double angC = 180.0 - ang1.Value - ang2.Value;
            if (angC <= 0) return null;

            double c = sideBetween.Value;  // сторона між ang1 і ang2

            // Теорема синусів: a/sin(A) = b/sin(B) = c/sin(C)
            double k = c / Math.Sin(ToRad(angC));
            double a = k * Math.Sin(ToRad(ang1.Value));
            double b = k * Math.Sin(ToRad(ang2.Value));

            // ang1=angA, ang2=angB, angC=angC
            // sideBC=a (навпроти angA), sideCA=b (навпроти angB), sideAB=c (навпроти angC)
            return new TriangleResult(c, a, b, ang1.Value, ang2.Value, angC);
        }

        // ККС: два кути і одна сторона (навпроти першого кута)
        private static TriangleResult? TryAAS(double? ang1, double? ang2, double? sideOpp1)
        {
            if (!ang1.HasValue || !ang2.HasValue || !sideOpp1.HasValue) return null;

            double angC = 180.0 - ang1.Value - ang2.Value;
            if (angC <= 0) return null;

            double sinAng1 = Math.Sin(ToRad(ang1.Value));
            if (sinAng1 < 1e-10) return null;

            double k = sideOpp1.Value / sinAng1;
            double side2 = k * Math.Sin(ToRad(ang2.Value));
            double side3 = k * Math.Sin(ToRad(angC));

            // sideOpp1 навпроти ang1 → це sideBC (навпроти angA)
            return new TriangleResult(side3, sideOpp1.Value, side2, ang1.Value, ang2.Value, angC);
        }
        // ССК: 

        // ─────────────────────────────────────────────
        //  ДОПОМІЖНІ МЕТОДИ
        // ─────────────────────────────────────────────

        private static (double? a, double? b, double? c) FillThirdAngle(
            double? a, double? b, double? c)
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
            if (r.SideAB + r.SideBC <= r.SideCA) return false;
            if (r.SideAB + r.SideCA <= r.SideBC) return false;
            if (r.SideBC + r.SideCA <= r.SideAB) return false;
            if (Math.Abs(r.AngleA + r.AngleB + r.AngleC - 180.0) > 0.1) return false;
            return true;
        }

        private static bool OutOfRange(double v) => v < -1.0 || v > 1.0;
        private static int Count(double? a, double? b, double? c)
            => (a.HasValue ? 1 : 0) + (b.HasValue ? 1 : 0) + (c.HasValue ? 1 : 0);
        private static double ToRad(double deg) => deg * Math.PI / 180.0;
        private static double ToDeg(double rad) => rad * 180.0 / Math.PI;
        private static double Acos(double x) => ToDeg(Math.Acos(Math.Clamp(x, -1.0, 1.0)));
    }
}
