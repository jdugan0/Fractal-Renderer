using Godot;
using System;

public partial class ComplexRenderer : Sprite2D
{
    int pixelHeight;
    int pixelWidth;
    public ShaderMaterial material;
    ImageTexture texture = new ImageTexture();
    [Export] public TextEdit textEdit;
    public float zoom = 0.1f;
    [Export] public float speed;
    public Vector2 offset = new Vector2(0, 0);

    [Export] public PauseMenu pause;
    public override void _Ready()
    {
        material = (ShaderMaterial)Material;
        pixelHeight = GetViewport().GetWindow().Size.Y;
        pixelWidth = GetViewport().GetWindow().Size.X;
        Image image = Image.CreateEmpty(pixelWidth, pixelHeight, false, Image.Format.Rgba8);
        texture.SetImage(image);
        Texture = texture;
        SendData(material);
    }

    public override void _Process(double delta)
    {
        material = (ShaderMaterial)Material;
        Vector2 velocity = new Vector2();
        bool doStuff = true;
        if (textEdit != null)
        {
            if (textEdit.HasFocus())
            {
                doStuff = false;
            }
        }
        if (doStuff && !pause.paused)
        {
            if (Input.IsActionPressed("UP"))
            {
                velocity += Vector2.Up;
            }
            if (Input.IsActionPressed("DOWN"))
            {
                velocity += Vector2.Down;
            }
            if (Input.IsActionPressed("LEFT"))
            {
                velocity += Vector2.Left;
            }
            if (Input.IsActionPressed("RIGHT"))
            {
                velocity += Vector2.Right;
            }
            if (Input.IsActionPressed("ZoomIn"))
            {
                zoom += (float)delta * 1f * zoom;
            }
            if (Input.IsActionPressed("ZoomOut"))
            {
                zoom -= (float)delta * 1f * zoom;
            }
            if (Input.IsActionJustPressed("Home"))
            {
                offset = new Vector2(0, 0);
                zoom = 0.100f;
            }
        }
        zoom = Mathf.Clamp(zoom, (float)1e-32, 999999);
        velocity = velocity.Normalized() * (float)delta / zoom * speed;
        offset += velocity;
        SendData(material);
    }

    public void SendData(ShaderMaterial m)
    {
        m.SetShaderParameter("offset", offset);
        m.SetShaderParameter("zoomFactor", zoom);
    }
}
