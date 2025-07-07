using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class ComplexRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;

    [Export] public Button magnitudeBox;

    [Export] public Button derivativeBox;

    [Export] public RecompileComplexRenderer compiler;
    [Export] public Plotter plotter;


    public bool magnitude;
    public bool derivative;

    public override void HandleInput(double delta)
    {
        if (TextEdit.HasFocus())
        {
            return;
        }
        base.HandleInput(delta);
        if (Input.IsActionJustPressed("Space"))
        {
            magnitude = !magnitude;
            magnitudeBox.SetPressedNoSignal(magnitude);
        }
        if (Input.IsActionJustPressed("Prime"))
        {
            derivative = !derivative;
            derivativeBox.SetPressedNoSignal(derivative);
        }
        if (Input.IsActionPressed("Click"))
        {
            Vector2 mouseV = GetViewport().GetMousePosition();
            Complex mouse = new Complex(mouseV.X, mouseV.Y) + new Complex(-_w / 2, -_h / 2);
            Complex scale = (mouse / _w / zoom) + offset;
            Complex start = scale;
            List<Vector2> vector2List = new List<Vector2>();

            Complex newNumber = compiler.function(start, 0);
            Complex pointPixel = ((newNumber - offset) * zoom * _w);
            Complex startPoint = ((start - offset) * zoom * _w);
            vector2List.Add(new Vector2((float)startPoint.Real, (float)startPoint.Imaginary));
            vector2List.Add(new Vector2((float)pointPixel.Real, (float)pointPixel.Imaginary));
            plotter.SetPoints(vector2List);
        }
        else
        {
            plotter.SetPoints(new List<Vector2>());
        }
    }

    public void toggleDerivative(bool toggeled)
    {
        derivative = toggeled;
    }
    public void toggleMag(bool toggeled)
    {
        magnitude = toggeled;
    }

    public override void PushUniforms()
    {
        base.PushUniforms();
        _mat.SetShaderParameter("render", magnitude);
        _mat.SetShaderParameter("prime", derivative);

    }
}
