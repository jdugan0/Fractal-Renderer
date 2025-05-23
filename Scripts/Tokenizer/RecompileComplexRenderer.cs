using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using Vector2 = Godot.Vector2;

public partial class RecompileComplexRenderer : LineEdit
{
    [Export] Sprite2D sprite;
    [Export] Godot.ShaderMaterial shader;
    [Export] bool useC;
    [Export] PauseMenu pauseMenu;
    String oldCode;
    bool outside = true;

    [Export] public string[] starterFunctions;

    public Func<Complex, Complex, Complex> function;
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
        if (pauseMenu.paused) return;
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

            function = ExpressionToGLSL.ExpressionParser.CompileToFunc(Text, useC);


            AddThemeStyleboxOverride("normal", styleBox);
        }
        catch (Exception e)
        {

            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.7f, 0.1f, 0.1f);

            AddThemeStyleboxOverride("normal", styleBox);
            // GD.PushError(e);
        }

    }
    public void recompile(String newText)
    {
        recompile();
    }
}
