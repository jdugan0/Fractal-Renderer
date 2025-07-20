using ExpressionToGLSL;
using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class MandlebrotRenderer : ViewBase
{
    [Export] public LineEdit TextEdit;

    public Complex juliaPoint;

    public bool julia = false;
    [Export] public int plotterIterations = 250;
    [Export] public Plotter plotter;
    [Export] public RecompileComplexRenderer compiler; // Assumes this generates the function
    [Export] public Button juliaBox;
    [Export] public Button intColorBox;
    bool intColor = false;
    bool juliaFromBox;
    int maxIters = 250;

    public void ComputeOrbital()
    {
        Complex c_ref_for_orbit;  // The 'c' constant for the reference orbit
        Complex z0_ref_for_orbit; // The 'z_0' starting point for the reference orbit

        if (!julia) // Mandelbrot mode
        {
            // For Mandelbrot, reference c is the screen's center (offset), z_0 is 0.
            c_ref_for_orbit = offset;
            z0_ref_for_orbit = Complex.Zero;
        }
        else // Julia mode
        {
            // For Julia, reference c is the juliaPoint.
            // z_0 for the reference orbit can be 0 or some other fixed point in the Julia region.
            // Using Complex.Zero is a common choice for simplicity.
            c_ref_for_orbit = juliaPoint;
            z0_ref_for_orbit = Complex.Zero;
        }

        Complex current_z_for_orbit = z0_ref_for_orbit; // Start of the reference orbit for CPU calculation
        Complex dfdz_val;
        Complex dfdc_val;

        // Arrays to hold hi/lo parts of reference orbit and derivatives
        Vector2[] J_hi = new Vector2[maxIters];
        Vector2[] K_hi = new Vector2[maxIters];
        Vector2[] F_hi = new Vector2[maxIters]; // Reference z_n values
        Vector2[] J_lo = new Vector2[maxIters];
        Vector2[] K_lo = new Vector2[maxIters];
        Vector2[] F_lo = new Vector2[maxIters];

        for (int i = 0; i < maxIters; i++)
        {
            // Store F[i] (z_ref(i)) BEFORE computing the next iteration.
            // This ensures F[i] corresponds to the z_ref_i used in the shader for Delta_z calculation at step i.
            (F_hi[i], F_lo[i]) = ComplexMathHelp.SplitComplex(current_z_for_orbit);

            // Calculate derivatives at (current_z_for_orbit, c_ref_for_orbit)
            dfdz_val = ComplexMathHelp.DfDz(compiler.function, current_z_for_orbit, c_ref_for_orbit);
            dfdc_val = ComplexMathHelp.DfDc(compiler.function, current_z_for_orbit, c_ref_for_orbit);
            
            (J_hi[i], J_lo[i]) = ComplexMathHelp.SplitComplex(dfdz_val);
            (K_hi[i], K_lo[i]) = ComplexMathHelp.SplitComplex(dfdc_val);

            // Iterate the reference orbit for the next step
            current_z_for_orbit = compiler.function(current_z_for_orbit, c_ref_for_orbit);
        }

        double pixelSize = 2.0 / (zoom * Math.Max(_w, _h));
        bool usePerturb = pixelSize < 1e-6; // Only enable perturbation at high zoom

        _mat.SetShaderParameter("usePerturb", usePerturb);
        _mat.SetShaderParameter("ref_Jhi", J_hi);
        _mat.SetShaderParameter("ref_Khi", K_hi);
        _mat.SetShaderParameter("ref_Fhi", F_hi); // This now contains z_ref_0, z_ref_1, ...
        _mat.SetShaderParameter("ref_Jlo", J_lo);
        _mat.SetShaderParameter("ref_Klo", K_lo);
        _mat.SetShaderParameter("ref_Flo", F_lo);

        // Pass the chosen reference constant for c (c_ref_for_orbit)
        _mat.SetShaderParameter("c_ref_hi", new Vector2(ComplexMathHelp.SplitDouble(c_ref_for_orbit.Real).hi, ComplexMathHelp.SplitDouble(c_ref_for_orbit.Imaginary).hi));
        _mat.SetShaderParameter("c_ref_lo", new Vector2(ComplexMathHelp.SplitDouble(c_ref_for_orbit.Real).lo, ComplexMathHelp.SplitDouble(c_ref_for_orbit.Imaginary).lo));
        
        // Pass the initial reference z (z0_ref_for_orbit)
        _mat.SetShaderParameter("z0_ref_hi", new Vector2(ComplexMathHelp.SplitDouble(z0_ref_for_orbit.Real).hi, ComplexMathHelp.SplitDouble(z0_ref_for_orbit.Imaginary).hi));
        _mat.SetShaderParameter("z0_ref_lo", new Vector2(ComplexMathHelp.SplitDouble(z0_ref_for_orbit.Real).lo, ComplexMathHelp.SplitDouble(z0_ref_for_orbit.Imaginary).lo));
    }

    // ... HandleInput and ToggleJulia/ToggleIntColor unchanged ...

    public override void PushUniforms()
    {


        ComputeOrbital(); // Calculate and push perturbation uniforms
        base.PushUniforms(); // Push zoom, offset etc.
        _mat.SetShaderParameter("julia", julia);
        _mat.SetShaderParameter("juliaPoint", new Godot.Vector2((float)juliaPoint.Real, (float)juliaPoint.Imaginary));
        _mat.SetShaderParameter("intColoring", intColor);
    }
}