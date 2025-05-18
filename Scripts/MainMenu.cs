using Godot;
using System;

public partial class MainMenu : Control
{
    public void Quit(){
        GetTree().Quit();
    }

    public void Switch(int id){
        SceneSwitcher.instance.SwitchScene(id);
    }
}
