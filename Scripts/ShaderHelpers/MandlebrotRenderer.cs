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

    public override void HandleInput(double delta)
    {
        if (TextEdit.HasFocus())
        {
            return;
        }
        if (Input.IsActionJustPressed("RightClick"))
        {
            julia = !julia;
        }
        Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-_w / 2, -_h / 2);
        Vector2 scale = (mouse / _w / zoom) + offset;
        if (Input.IsActionPressed("RightClick"))
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
                previous = compiler.function(previous, new Complex(scale.X, scale.Y));
            }
            plotter.SetPoints(points);
        }
        else
        {
            plotter.SetPoints(new List<Vector2>());
        }
        base.HandleInput(delta);
    }
    public override void PushUniforms()
    {
        base.PushUniforms();
        _mat.SetShaderParameter("julia", julia);
        _mat.SetShaderParameter("juliaPoint", juliaPoint);
    }
}
