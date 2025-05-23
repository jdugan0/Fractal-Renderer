using Godot;
using System;

public partial class MandlebrotRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;

    public Vector2 juliaPoint;

    public bool julia = false;

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

        if (Input.IsActionPressed("RightClick"))
        {
            Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-_w / 2, -_h / 2);
            Vector2 scale = (mouse / _w / zoom) + offset;
            juliaPoint = scale;
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
