using System;
using Godot;

public partial class ButtonSceneSwitcher : Node
{
    public void Switch(int id)
    {
        SceneSwitcher.instance.SwitchScene(id);
    }
}
