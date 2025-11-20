using Godot;
using System;
using System.Numerics;
using Vector2 = Godot.Vector2;
namespace ExpressionToGLSL
{
    public static class HelperMath
    {
        public static (float hi, float lo) SplitDouble(double x)
        {
            float x_h = (float)x;
            float x_l = (float)(x - x_h);
            return (x_h, x_l);
        }
        public static (Vector2 hi, Vector2 lo) SplitVec(Complex x)
        {
            Vector2 x_h = new Vector2((float)x.Real, (float)x.Imaginary);
            Vector2 x_l = new Vector2((float)(x.Real - x_h.X), (float)(x.Imaginary - x_h.Y));
            return (x_h, x_l);
        }
        public static Vector2 ComplexToVec(Complex c)
        {
            Godot.Vector2 v = new Godot.Vector2((float)c.Real, (float)c.Imaginary);
            return v;
        }

        public static Complex VecToComplex(Godot.Vector2 vector)
        {
            return new Complex(vector.X, vector.Y);
        }
    }
}

