using ExpressionToGLSL;
using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class NewtonRenderer : ViewBase
{
    List<Complex> roots = new List<Complex>();
    int color = 0;
    bool hover = false;
    [Export] public int plotterIterations = 250;
    [Export] public Plotter plotter;
    bool fancy = true;
    public override void _Ready()
    {
        base._Ready();
        roots.Add(new Complex(0, 0));
        roots.Add(new Complex(0, 1));
        roots.Add(new Complex(1, 0));
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (pauseMenu.paused)
        {
            return;
        }
        Complex mouse = HelperMath.VecToComplex(GetViewport().GetMousePosition()) + new Complex(-_w / 2, -_h / 2);
        Complex scale = (mouse / _w / zoom) + offset;
        if (@event.IsActionPressed("Click"))
        {
            roots.Add(scale);
        }
        if (@event.IsActionPressed("MMB"))
        {

            int id = findClosest();
            if (id != -1 && roots.Count > 1)
            {
                roots.RemoveAt(id);
            }
        }

    }

    public void ToggleFancy(bool toggle)
    {
        fancy = toggle;
    }

    public override void HandleInput(double delta)
    {
        Complex mouse = HelperMath.VecToComplex(GetViewport().GetMousePosition()) + new Complex(-_w / 2, -_h / 2);
        Complex scale = (mouse / _w / zoom) + offset;
        base.HandleInput(delta);
        if (pauseMenu.paused)
        {
            return;
        }
        if (Input.IsActionPressed("RightClick"))
        {
            int id = findClosest();
            if (id != -1)
            {
                roots[id] = scale;

            }

        }
        if (Input.IsActionPressed("X") && !hover)
        {
            List<Vector2> points = new List<Vector2>();

            Complex z = scale;
            for (int i = 0; i < plotterIterations; i++)
            {
                points.Add(ComplexToScreen(z));
                Complex f = Complex.One;
                Complex sum = Complex.Zero;
                foreach (Complex r in roots)
                {
                    Complex diff = z - r;
                    f *= diff;
                    sum += Complex.One / diff;
                }
                Complex fp = f * sum;

                if (f.Magnitude < 1e-3 || fp == Complex.Zero)
                    break;

                z -= f / fp;
            }
            plotter.SetPoints(points);
        }
        else
        {
            plotter.SetPoints(new List<Vector2>());
        }
    }

    private Vector2 ComplexToScreen(Complex z)
    {
        Complex zp = (z - offset) * zoom * _w;
        return new Vector2((float)zp.Real, (float)zp.Imaginary);
    }

    public void setHover(bool hover)
    {
        this.hover = hover;
    }

    public void setColor(int color)
    {
        this.color = color;
    }

    public override void PushUniforms()
    {
        base.PushUniforms();
        int id = findClosest();
        Vector2[] list = new Vector2[100];
        for (int i = 0; i < roots.Count && i < 100; i++)
        {
            list[i] = HelperMath.ComplexToVec(roots[i]);
        }
        _mat.SetShaderParameter("roots", list);
        _mat.SetShaderParameter("idClose", id);
        _mat.SetShaderParameter("rootCount", Math.Min(roots.Count, 100));
        _mat.SetShaderParameter("color", color);
        _mat.SetShaderParameter("fancy_shading", fancy);
    }
    public int findClosest()
    {
        Complex mouse = HelperMath.VecToComplex(GetViewport().GetMousePosition()) + new Complex(-_w / 2, -_h / 2);
        Complex scale = (mouse / _w / zoom) + offset;
        double best = double.MaxValue;
        int id = -1;
        for (int i = 0; i < roots.Count; i++)
        {
            if ((roots[i] - scale).Magnitude < best)
            {
                best = (roots[i] - scale).Magnitude;
                id = i;
            }
        }
        return id;
    }
}
