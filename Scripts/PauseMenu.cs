using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
    [Export] public LineEdit textEdit;
    public bool paused = false;
    [Export] Panel mainPause;
    [Export] Panel options;
    public bool UI = false;

    public override void _Ready()
    {
        Visible = false;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (Input.IsActionPressed("Escape"))
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (textEdit != null)
        {
            if (textEdit.HasFocus())
                return;
        }
        paused = !paused;
        Visible = !Visible;
        HideOptions();
    }

    public void ShowOptions(){
        options.Show();
        mainPause.Hide();
    }

    public void HideOptions(){
        options.Hide();
        mainPause.Show();
    }

    public void toggleUI(bool toggle){
        UI = toggle;
    }
    public void Menu()
    {
        SceneSwitcher.instance.SwitchScene(0);
    }
}
