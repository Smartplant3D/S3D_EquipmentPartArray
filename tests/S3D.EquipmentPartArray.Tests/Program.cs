using System;
using S3D.EquipmentPartArray.Core;

class Program
{
    static int Main()
    {
        try
        {
            var rectangle = ArrayPlanner.Rectangle(2, 3, 2, 2, 3, 4, new V3(1, 0, 0), new V3(0, 1, 0), new V3(0, 0, 1));
            Equal(11, rectangle.Length, "rectangular additional groups");
            Near(new V3(2, 0, 0), rectangle[0].Offset, "first column");
            Near(new V3(0, 3, 0), rectangle[2].Offset, "first row");
            Near(new V3(0, 0, 4), rectangle[5].Offset, "first layer");
            var full = ArrayPlanner.Circular(4, 360, new V3(0, 0, 1));
            Equal(3, full.Length, "full circle additional groups");
            Near(90, full[0].AngleDegrees, "full circle spacing");
            Near(270, full[2].AngleDegrees, "full circle last");
            var arc = ArrayPlanner.Circular(3, 180, new V3(0, 0, 1));
            Near(180, arc[1].AngleDegrees, "open arc endpoint");
            Near(new V3(0, 1, 0), ArrayPlanner.Rotate(new V3(1, 0, 0), new V3(0, 0, 1), 90), "axis rotation");
            Throws(() => ArrayPlanner.Rectangle(1, 0, 1, 1, 1, 1, V3.XAxis, V3.YAxis, V3.ZAxis), "zero count");
            Throws(() => ArrayPlanner.Circular(2, 90, new V3(0, 0, 0)), "zero axis");
            Console.WriteLine("8 placement checks passed");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    static void Equal(int expected, int actual, string message) { if (expected != actual) throw new Exception(message); }
    static void Near(double expected, double actual, string message) { if (Math.Abs(expected - actual) > 1e-8) throw new Exception(message); }
    static void Near(V3 expected, V3 actual, string message) { if ((expected - actual).Length > 1e-8) throw new Exception(message); }
    static void Throws(Action action, string message) { try { action(); } catch (ArgumentException) { return; } throw new Exception(message); }
}
