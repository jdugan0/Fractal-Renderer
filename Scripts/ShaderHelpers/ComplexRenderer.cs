using Godot;
using System;

public partial class ComplexRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;

    [Export] public Button magnitudeBox;

    [Export] public Button derivativeBox;


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
