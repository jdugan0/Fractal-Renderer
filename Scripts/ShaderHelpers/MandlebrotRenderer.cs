using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class MandlebrotRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;

    public Complex juliaPoint;

    public bool julia = false;
    [Export] public int plotterIterations = 250;
    [Export] public Plotter plotter;
    [Export] public RecompileComplexRenderer compiler;
    [Export] public Button juliaBox;
    [Export] public Button intColorBox;
    bool intColor = false;
    bool juliaFromBox;

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
        Complex mouse = vecToComplex(GetViewport().GetMousePosition()) + new Complex(-_w / 2, -_h / 2);
        Complex scale = (mouse / _w / zoom) + offset;
        if (Input.IsActionPressed("RightClick") || juliaFromBox)
        {
            juliaPoint = scale;
        }

        if (Input.IsActionPressed("Click"))
        {
            List<Vector2> points = new List<Vector2>();
            Complex start = new Complex();
            Complex c = scale;
            if (julia)
            {
                start = scale;
                c = juliaPoint;
            }
            Complex previous = compiler.function(start, c);
            for (int i = 0; i < plotterIterations; i++)
            {
                Complex pointPixel = (previous - offset) * zoom * _w;
                Vector2 point = complexToVec(pointPixel);
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
        _mat.SetShaderParameter("juliaPoint", complexToVec(juliaPoint));
        _mat.SetShaderParameter("intColoring", intColor);
    }
}
