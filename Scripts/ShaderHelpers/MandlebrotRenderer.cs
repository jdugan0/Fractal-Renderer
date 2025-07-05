using ExpressionToGLSL;
using Godot;
using Meta.Numerics.Analysis;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class MandlebrotRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;

    public Vector2 juliaPoint;

    public bool julia = false;
    [Export] public int plotterIterations = 250;
    [Export] public Plotter plotter;
    [Export] public RecompileComplexRenderer compiler;
    [Export] public Button juliaBox;
    [Export] public Button intColorBox;
    bool intColor = false;
    bool juliaFromBox;

    int maxIters = 500;
    public void ComputeOrbit()
    {

        Complex c_ref = new Complex(offset.X, offset.Y);
        Complex z = compiler.function.Invoke(Complex.Zero, c_ref);
        GD.Print(zoom);
        Func<Complex, Complex, Complex> dfdc = (z, c) => ComplexDiff.DfDc(compiler.function, z, c);
        Func<Complex, Complex, Complex> dfdz = (z, c) => ComplexDiff.DfDz(compiler.function, z, c);
        Complex[] J = new Complex[maxIters];   // partial z
        Complex[] K = new Complex[maxIters];   // partial c
        Complex[] F = new Complex[maxIters];
        for (int i = 0; i < maxIters; i++)
        {
            J[i] = dfdz(z, c_ref);
            K[i] = dfdc(z, c_ref);
            F[i] = z;

            z = compiler.function(z, c_ref);

        }

        Vector2[] Jvec = new Vector2[maxIters];
        Vector2[] Kvec = new Vector2[maxIters];
        Vector2[] Fvec = new Vector2[maxIters];
        for (int i = 0; i < maxIters; i++)
        {
            Jvec[i] = new Vector2((float)J[i].Real, (float)J[i].Imaginary);
            Kvec[i] = new Vector2((float)K[i].Real, (float)K[i].Imaginary);
            Fvec[i] = new Vector2((float)F[i].Real, (float)F[i].Imaginary);
        }
        _mat.SetShaderParameter("ref_J", Jvec);
        _mat.SetShaderParameter("ref_K", Kvec);
        _mat.SetShaderParameter("ref_F", Fvec);
    }

    public override void HandleInput(double delta)
    {
        if (TextEdit.HasFocus())
        {
            return;
        }
        if (!julia)
        {
            juliaFromBox = false;
        }
        if (Input.IsActionJustPressed("RightClick"))
        {
            julia = !julia;
            juliaBox.SetPressedNoSignal(julia);
        }
        Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-_w / 2, -_h / 2);
        Vector2 scale = (mouse / _w / zoom) + offset;
        if (Input.IsActionPressed("RightClick") || juliaFromBox)
        {
            juliaPoint = scale;
        }

        if (Input.IsActionPressed("Click"))
        {
            List<Vector2> points = new List<Vector2>();
            Complex start = new Complex();
            Complex c = new Complex(scale.X, scale.Y);
            if (julia)
            {
                start = new Complex(scale.X, scale.Y);
                c = new Complex(juliaPoint.X, juliaPoint.Y);
            }
            Complex previous = compiler.function(start, c);
            for (int i = 0; i < plotterIterations; i++)
            {
                Complex pointPixel = ((previous - new Complex(offset.X, offset.Y))
                * zoom * _w);
                Vector2 point = new Vector2((float)pointPixel.Real, (float)pointPixel.Imaginary);
                points.Add(point);
                previous = compiler.function(previous, c);
            }
            plotter.SetPoints(points);
        }
        else
        {
            plotter.SetPoints(new List<Vector2>());
        }
        base.HandleInput(delta);
    }

    public void ToggleJulia(bool toggle)
    {

        julia = toggle;
        juliaFromBox = true;
    }
    public void ToggleIntColor(bool toggle)
    {
        intColor = toggle;
    }
    public override void PushUniforms()
    {
        base.PushUniforms();
        _mat.SetShaderParameter("julia", julia);
        _mat.SetShaderParameter("juliaPoint", juliaPoint);
        _mat.SetShaderParameter("intColoring", intColor);
        ComputeOrbit();
    }
}
