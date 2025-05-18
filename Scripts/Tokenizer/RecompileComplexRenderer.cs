using Godot;
using System;

public partial class RecompileComplexRenderer : TextEdit
{
    [Export] Sprite2D sprite;
    [Export] Godot.ShaderMaterial shader;
    String oldCode;
    bool outside = true;
    public override void _Ready()
    {
        oldCode = shader.Shader.Code;
        MouseEntered += () => { outside = false; };
        MouseExited += () => { outside = true; };
        recompile();
    }
    public override void _Process(double delta)
    {

        if ((Input.IsActionPressed("Click") && HasFocus() && outside) || Input.IsActionPressed("Escape"))
        {
            ReleaseFocus();
        }
    }

    public void recompile()
    {
        try
        {
            String glsl = ExpressionToGLSL.ExpressionParser.ConvertExpressionToGlsl(Text);
            String code = oldCode;
            code = code.Replace("vec2(0.00)", glsl);
            // GD.Print(glsl);
            var shaderNew = new Godot.Shader();
            shaderNew.Code = code;
            var mat = new ShaderMaterial();
            mat.Shader = shaderNew;
            sprite.Material = mat;
        }
        catch
        {
            GD.Print("parse failed :(");
        }
    }
}
