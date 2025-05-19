using Godot;
using System;

public partial class MenuImage : Panel
{
    [Export] public Texture2D[] images;
    public void SwitchImage(int id)
    {
        var stylebox = new StyleBoxTexture();
        stylebox.Texture = images[id];
        AddThemeStyleboxOverride("panel", stylebox);
    }
}
