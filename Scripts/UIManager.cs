using Godot;
using System;

public partial class UIManager : CanvasLayer
{
    [Export] public PauseMenu pauseMenu;
    public override void _Process(double delta)
    {
        Visible = !pauseMenu.UI;
    }
}
