using System;
using System.Collections.Generic;

namespace S3D.EquipmentPartArray.Core
{
    public struct V3
    {
        public readonly double X, Y, Z;
        public static readonly V3 XAxis = new V3(1, 0, 0);
        public static readonly V3 YAxis = new V3(0, 1, 0);
        public static readonly V3 ZAxis = new V3(0, 0, 1);
        public V3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public V3 Normalized { get { if (Length < 1e-12) throw new ArgumentException("Axis length must be positive."); return this / Length; } }
        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static V3 operator *(V3 a, double b) => new V3(a.X * b, a.Y * b, a.Z * b);
        public static V3 operator /(V3 a, double b) => new V3(a.X / b, a.Y / b, a.Z / b);
        public double Dot(V3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public V3 Cross(V3 b) => new V3(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
    }

    public struct Placement
    {
        public readonly V3 Offset;
        public readonly double AngleDegrees;
        public Placement(V3 offset, double angleDegrees) { Offset = offset; AngleDegrees = angleDegrees; }
    }

    public static class ArrayPlanner
    {
        public static Placement[] Rectangle(int rows, int columns, int layers, double columnPitch, double rowPitch, double layerPitch, V3 columnAxis, V3 rowAxis, V3 layerAxis)
        {
            if (rows < 1 || columns < 1 || layers < 1 || (long)rows * columns * layers > 10000) throw new ArgumentException("Group count must be 1..10000.");
            CheckFinite(columnPitch, rowPitch, layerPitch);
            var result = new List<Placement>();
            for (int layer = 0; layer < layers; layer++)
                for (int row = 0; row < rows; row++)
                    for (int column = 0; column < columns; column++)
                    {
                        if (layer == 0 && row == 0 && column == 0) continue;
                        result.Add(new Placement(columnAxis * (column * columnPitch) + rowAxis * (row * rowPitch) + layerAxis * (layer * layerPitch), 0));
                    }
            return result.ToArray();
        }

        public static Placement[] Circular(int count, double fillDegrees, V3 axis)
        {
            if (count < 1 || count > 10000 || !Finite(fillDegrees) || fillDegrees <= 0 || fillDegrees > 360 || axis.Length < 1e-12) throw new ArgumentException("Invalid circular parameters.");
            var result = new Placement[count - 1];
            double step = fillDegrees == 360 ? 360.0 / count : count == 1 ? 0 : fillDegrees / (count - 1);
            for (int i = 1; i < count; i++) result[i - 1] = new Placement(new V3(), i * step);
            return result;
        }

        public static V3 Rotate(V3 vector, V3 axis, double degrees)
        {
            CheckFinite(vector.X, vector.Y, vector.Z, degrees);
            V3 unit = axis.Normalized;
            double radians = degrees * Math.PI / 180;
            return vector * Math.Cos(radians) + unit.Cross(vector) * Math.Sin(radians) + unit * (unit.Dot(vector) * (1 - Math.Cos(radians)));
        }

        private static void CheckFinite(params double[] values) { foreach (double value in values) if (!Finite(value)) throw new ArgumentException("Values must be finite."); }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
