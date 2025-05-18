using Godot;
using System;

public partial class MandlebrotRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;


    public override void HandleInput(double delta)
    {
        if (TextEdit.HasFocus())
        {
            return;
        }
        base.HandleInput(delta);
    }
}
