using Godot;
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

    [Export] public int maxIters = 500;
    [Export] public double diffStep = 1e-8;
    private Vector2[] _orbitCPU;
    private Vector2[] _dzdC_CPU;
    private Func<Complex, Complex, Complex> _f;
    Complex c_ref = new Complex();
    private Func<Complex, Complex, double, Complex> _partialZ;
    private Func<Complex, Complex, double, Complex> _partialC;

    private void InitNumerics()
    {
        _f = compiler.function;
        _partialZ = (z, c, h) => ExpressionToGLSL.ComplexDiff.DfDz(_f, z, c, h);
        _partialC = (z, c, h) => ExpressionToGLSL.ComplexDiff.DfDc(_f, z, c, h);
        _orbitCPU = new Vector2[maxIters + 1];
        _dzdC_CPU = new Vector2[maxIters + 1];
    }



    public void ComputeOrbital()
    {
        InitNumerics();
        Complex z = Complex.Zero;
        Complex dzdc = Complex.Zero;
        c_ref = new Complex(offset.X, offset.Y);
        for (int i = 0; i <= maxIters; i++)
        {
            _orbitCPU[i] = new Vector2((float)z.Real, (float)z.Imaginary);
            _dzdC_CPU[i] = new Vector2((float)dzdc.Real, (float)dzdc.Imaginary);

            // forward 1 step
            var fz = _f(z, c_ref);
            var fz_z = _partialZ(z, c_ref, diffStep);
            var fz_c = _partialC(z, c_ref, diffStep);
            dzdc = fz_z * dzdc + fz_c;
            z = fz;
        }
        PackOrbit();
    }

    public void PackOrbit()
    {
        int n = maxIters + 1;

        Image imgOrbit = Image.CreateEmpty(n, 1, false, Image.Format.Rgf);
        Image imgDzdc = Image.CreateEmpty(n, 1, false, Image.Format.Rgf);

        for (int i = 0; i < n; i++)
        {
            var o = _orbitCPU[i];
            var d = _dzdC_CPU[i];
            imgOrbit.SetPixelv(new Vector2I(i, 0), new Color(o.X, o.Y, 0, 0));
            imgDzdc.SetPixelv(new Vector2I(i, 0), new Color(d.X, d.Y, 0, 0));
        }

        ImageTexture orbitTex = ImageTexture.CreateFromImage(imgOrbit);
        ImageTexture dzdcTex = ImageTexture.CreateFromImage(imgDzdc);

        _mat.SetShaderParameter("orbitTex", orbitTex);
        _mat.SetShaderParameter("dzdCTex", dzdcTex);
        _mat.SetShaderParameter("c_ref", new Vector2((float)c_ref.Real, (float)c_ref.Imaginary));
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
        ComputeOrbital();
        base.PushUniforms();
        _mat.SetShaderParameter("julia", julia);
        _mat.SetShaderParameter("juliaPoint", juliaPoint);
        _mat.SetShaderParameter("intColoring", intColor);
        _mat.SetShaderParameter("MAX_ITERS", maxIters);
    }
}
