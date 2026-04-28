namespace Tringle_calcolator
{
    /// <summary>
    /// Повний результат розрахунку трикутника ABC.
    /// Сторони: AB, BC, CA
    /// Кути: AngleA, AngleB, AngleC (в градусах)
    /// </summary>
    public record TriangleResult(
        double SideAB,   // сторона AB (навпроти кута C)
        double SideBC,   // сторона BC (навпроти кута A)
        double SideCA,   // сторона CA (навпроти кута B)
        double AngleA,   // кут A в градусах
        double AngleB,   // кут B в градусах
        double AngleC    // кут C в градусах
    );
}
