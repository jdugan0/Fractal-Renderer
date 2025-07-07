// res://Scripts/ComplexViewBase.cs
using Godot;
using System;
using System.Numerics;

public partial class ViewBase : Sprite2D
{
    [Export] public double speed = 1f;
    public Complex offset = Complex.Zero;
    public double zoom = 0.1f;

    [Export] public PauseMenu pauseMenu;

    public int _w, _h;
    ImageTexture _tex = new ImageTexture();
    public ShaderMaterial _mat;
    [Export] Label coordLabel;

    public override void _Ready()
    {
        _w = GetViewport().GetWindow().Size.X;
        _h = GetViewport().GetWindow().Size.Y;

        // one-pixel-per-fragment backing texture
        var img = Image.CreateEmpty(_w, _h, false, Image.Format.Rgba8);
        _tex.SetImage(img);
        Texture = _tex;

        _mat = (ShaderMaterial)Material;
        PushUniforms();            // first push
    }

    public override void _Process(double delta)
    {
        _mat = (ShaderMaterial)Material;
        HandleInput(delta);
        PushUniforms();
        // Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-_w / 2, -_h / 2);
        // Vector2 scale = (mouse / _w / zoom) + offset;
        // coordLabel.Position = mouse;
        // double xPos = Math.Round(scale.X * Math.Clamp(zoom, 1, 1e99)) / Math.Clamp(zoom, 1, 1e99);
        // double yPos = Math.Round(scale.Y * Math.Clamp(zoom, 1, 1e99)) / Math.Clamp(zoom, 1, 1e99);
        // coordLabel.Text = String.Format("{0}, {1}i", xPos, yPos);
    }
    public virtual void HandleInput(double delta)
    {
        if (pauseMenu.paused)
        {
            return;
        }
        Complex v = Complex.Zero;
        if (Input.IsActionPressed("UP")) v -= Complex.ImaginaryOne;
        if (Input.IsActionPressed("DOWN")) v += Complex.ImaginaryOne;
        if (Input.IsActionPressed("LEFT")) v -= Complex.One;
        if (Input.IsActionPressed("RIGHT")) v += Complex.One;

        if (Input.IsActionPressed("ZoomIn")) zoom += (delta) * zoom;
        if (Input.IsActionPressed("ZoomOut")) zoom -= (delta) * zoom;
        if (Input.IsActionJustPressed("Home")) { offset = Complex.Zero; zoom = 0.1f; }

        zoom = Mathf.Clamp(zoom, 1e-32f, 1e9f);
        if (v.Magnitude > 0)
        {
            offset += v / v.Magnitude * delta / zoom * speed;
        }
    }

    public virtual void PushUniforms()
    {
        if (_mat == null) return;
        GD.Print(offset);
        _mat.SetShaderParameter("offset", new Godot.Vector2((float)offset.Real, (float)offset.Imaginary));
        _mat.SetShaderParameter("zoomFactor", zoom);
    }
}
