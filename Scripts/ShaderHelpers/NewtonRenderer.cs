using Godot;
using System;
using System.Collections.Generic;

public partial class NewtonRenderer : ViewBase
{
    List<Vector2> roots = new List<Vector2>();
    int color = 0;
    bool hover = false;
    public override void _UnhandledInput(InputEvent @event)
    {
        if (pauseMenu.paused)
        {
            return;
        }
        Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-_w / 2, -_h / 2);
        Vector2 scale = (mouse / _w / zoom) + offset;
        if (@event.IsActionPressed("Click"))
        {
            roots.Add(scale);
        }
        if (@event.IsActionPressed("MMB"))
        {

            int id = findClosest();
            if (id != -1)
            {
                roots.RemoveAt(id);
            }
        }
        if (@event.IsActionPressed("RightClick"))
        {
            int id = findClosest();
            if (id != -1)
            {
                roots[id] = scale;

            }

        }
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
            list[i] = roots[i];
        }
        _mat.SetShaderParameter("roots", list);
        _mat.SetShaderParameter("idClose", id);
        _mat.SetShaderParameter("rootCount", Math.Min(roots.Count, 100));
        _mat.SetShaderParameter("color", color);
    }
    public int findClosest()
    {
        Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-_w / 2, -_h / 2);
        Vector2 scale = (mouse / _w / zoom) + offset;
        float best = float.MaxValue;
        int id = -1;
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i].DistanceTo(scale) < best)
            {
                best = roots[i].DistanceTo(scale);
                id = i;
            }
        }
        return id;
    }
}
