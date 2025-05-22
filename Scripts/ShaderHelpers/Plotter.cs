using Godot;
using System.Collections.Generic;

public partial class Plotter : Node2D
{
    private List<Vector2> points = new List<Vector2>();

    public void SetPoints(List<Vector2> newPoints)
    {
        points = newPoints;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (points.Count < 2)
            return;

        // GD.Print(points[0]);
        for (int i = 0; i < points.Count - 1; i++)
        {
            DrawLine(points[i], points[i + 1], Colors.White, 2.0f);
        }

        foreach (var pt in points)
        {
            DrawCircle(pt, 3.0f, Colors.Red);
        }
    }
}
