using Godot;
using System;

public partial class RecompileComplexRenderer : LineEdit
{
    [Export] Sprite2D sprite;
    [Export] Godot.ShaderMaterial shader;
    [Export] bool useC;
    String oldCode;
    bool outside = true;

    [Export] public string[] starterFunctions;
    public override void _Ready()
    {
        oldCode = shader.Shader.Code;
        MouseEntered += () => { outside = false; };
        MouseExited += () => { outside = true; };
        string func = starterFunctions[GD.Randi() % starterFunctions.Length];
        Text = func;
        recompile();
    }
    public override void _Process(double delta)
    {

        if ((Input.IsActionPressed("Click") && HasFocus() && outside) || Input.IsActionPressed("Escape"))
        {
            ReleaseFocus();
        }
        if (Input.IsActionJustPressed("R") && !HasFocus())
        {
            string func = starterFunctions[GD.Randi() % starterFunctions.Length];
            Text = func;
            recompile();
        }
    }

    public void recompile()
    {
        try
        {
            String glsl = ExpressionToGLSL.ExpressionParser.ConvertExpressionToGlsl(Text, useC);
            String code = oldCode;
            code = code.Replace("vec2(0.00)", glsl);

            var shaderNew = new Godot.Shader();
            shaderNew.Code = code;
            var mat = new ShaderMaterial();
            mat.Shader = shaderNew;
            sprite.Material = mat;
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.1f, 0.1f, 0.1f);

            AddThemeStyleboxOverride("normal", styleBox);
        }
        catch
        {
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.7f, 0.1f, 0.1f);

            AddThemeStyleboxOverride("normal", styleBox);
        }
    }
    public void recompile(String newText)
    {
        recompile();
    }
}
