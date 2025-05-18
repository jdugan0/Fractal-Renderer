using Godot;
using System;

public partial class ComplexRenderer : ViewBase
{
    [Export] public TextEdit TextEdit;

    public override void HandleInput(double delta)
    {
        if (TextEdit.HasFocus())
        {
            return;
        }
        base.HandleInput(delta);
    }
}
