// res://Scripts/ComplexViewBase.cs
using Godot;
using System;

public partial class ViewBase : Sprite2D
{
    [Export] public float speed = 1f;
    public Vector2 offset = Vector2.Zero;
    public float zoom = 0.1f;

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
        Vector2 v = Vector2.Zero;
        if (Input.IsActionPressed("UP")) v += Vector2.Up;
        if (Input.IsActionPressed("DOWN")) v += Vector2.Down;
        if (Input.IsActionPressed("LEFT")) v += Vector2.Left;
        if (Input.IsActionPressed("RIGHT")) v += Vector2.Right;

        if (Input.IsActionPressed("ZoomIn")) zoom += (float)(delta) * zoom;
        if (Input.IsActionPressed("ZoomOut")) zoom -= (float)(delta) * zoom;
        if (Input.IsActionJustPressed("Home")) { offset = Vector2.Zero; zoom = 0.1f; }

        zoom = Mathf.Clamp(zoom, 1e-32f, 1e9f);
        offset += v.Normalized() * (float)delta / zoom * speed;
    }

    public virtual void PushUniforms()
    {
        if (_mat == null) return;
        _mat.SetShaderParameter("offset", offset);
        _mat.SetShaderParameter("zoomFactor", zoom);
    }
}
