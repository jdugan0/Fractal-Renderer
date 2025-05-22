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
    [Export] public MandlebrotRenderer mandlebrotRenderer;

    [Export] public string[] starterFunctions;

    Func<Complex, Complex, Complex> function;

    [Export] Plotter plotter;

    [Export] int plotterIterations = 50;
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
        if (mandlebrotRenderer == null || plotter == null)
        {
            return;
        }
            Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-mandlebrotRenderer._w / 2, -mandlebrotRenderer._h / 2);
            Vector2 scale = (mouse / mandlebrotRenderer._w / mandlebrotRenderer.zoom) + mandlebrotRenderer.offset;
            if (Input.IsActionPressed("Click") && useC && !HasFocus())
            {
                List<Vector2> points = new List<Vector2>();
                Complex previous = function(new Complex(), new Complex(scale.X, scale.Y));
                for (int i = 0; i < plotterIterations; i++)
                {
                    Complex pointPixel = previous - new Complex(mandlebrotRenderer.offset.X, mandlebrotRenderer.offset.Y) * mandlebrotRenderer.zoom * mandlebrotRenderer._w - new Complex(-mandlebrotRenderer._w / 2, -mandlebrotRenderer._h / 2);
                    Vector2 point = new Vector2((float)pointPixel.Real, (float)pointPixel.Imaginary);
                    points.Add(point);
                    GD.Print(point);
                    previous = function(previous, new Complex(scale.X, scale.Y));
                }
                plotter.SetPoints(points);
            }
            else
            {
                plotter.SetPoints(new List<Vector2>());
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
