shader_type canvas_item;
#include "res://Scripts/Shaders/ComplexMath.gdshaderinc"
uniform vec2 offset = vec2(0.0,0.0);
uniform float zoomFactor = 0.1;

uniform vec2 juliaPoint = vec2(0.0, 0.0);
uniform bool julia = false;

uniform bool intColoring = false;


vec2 func(vec2 z, vec2 c) {
	vec2 result = vec2(0.00);
    return result;
}
vec3 palette(float t){
    vec3 a = vec3(0.5, 0.5, 0.5);
    vec3 b = vec3(0.5, 0.5, 0.5);
    vec3 c = vec3(1.3, 1.1, 0.9);
    vec3 d = vec3(0.75, 0.20, 0.25);   // phase
    return a + b * cos(6.28318 * (c * t + d));
}
vec3 blend_pal(float t, float trap){
    vec3 base = palette(t);
    vec3 hsv  = hsv_to_rgb(mod(t*360.0,360.0), 0.85, 1.0);
    vec3 mix1 = mix(base, hsv, 0.5);
    float glow = clamp(1.0 - pow(trap / 256.0, 0.25), 0.0, 1.0);
    return mix1 * (0.6 + 0.4*glow);
}
bool non_finite(vec2 v) {
    return any(isnan(v)) || any(isinf(v));
}

void fragment() {
    vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
    vec2 c   = transform(vec2(UV.x/SCREEN_PIXEL_SIZE.x,
                              UV.y/SCREEN_PIXEL_SIZE.y),
                         res, zoomFactor, offset);
	vec2 start = vec2(0.0, 0.0);
	if (julia) {
		start = c;
		c = juliaPoint;
	}
    vec2 z = func(start, c);

    float trap     = 1e20;
    float mu       = 0.0;
    bool  esc      = false;
    bool  cycle    = false;

    const int MAX_ITERS = 250;
    const int MIN_ITERS = 20;
	bool mu_lock = false;

    float bailout  = 256.0;
    float eps = 1e-5;   // tolerance for “same value”

    vec2 tortoise  = z;  // reference point
    int  power     = 1;  // when to move the reference
    int  lam       = 0;  // distance travelled since last reset
	int count;
	const int PERIOD = 20;
	vec2 z_prev = z;
	int pcount = 0;

    for (int i = 0; i < MAX_ITERS; ++i) {
		count = i;
        /* normal fractal iteration */
        z = func(z, c);

		if (length(z - z_prev) < eps) {
            cycle = true;
            break;
        }
        vec2 delta = z - c;
        float r2   = dot(delta, delta);
        trap       = min(trap, r2);

		if (non_finite(z)) {
	        esc = true;
	        if (!mu_lock){
				mu  = float(i);
			}
	        break;
    	}


        if (r2 > bailout) {
			if (!mu_lock){
				float logR_prev = log(length(z_prev));
				float logR_curr = log(length(z));
				float slope = logR_curr - logR_prev;
				slope = max(slope, 1e-7);
				float frac = (log(bailout) - logR_prev) / slope;
				mu = float(i - 1) + clamp(frac, 0.0, 1.0);
				mu_lock = true;
			}
			if (i > MIN_ITERS){
				esc = true;
           		break;
			}
        }


        ++lam;
        if (dot(z - tortoise, z - tortoise) < eps*eps) {
            cycle = true;
            break;              // found a repeat → interior / attractive cycle
        }

        if (lam == power) {     // time to shift the reference forward
            tortoise = z;
            power   <<= 1;      // power *= 2
            lam      = 0;
        }
		z_prev = z;
    }

    if (!esc) {
        COLOR = vec4(!intColoring ? 0.0 : float(count)/float(MAX_ITERS), 0.0, 0.0, 1.0);
		//if (cycle){
			//COLOR = vec4(1.0, 1.0, 1.0, 1.0);
		//}
    } else {
        float t = mu / float(MAX_ITERS);
        COLOR   = vec4(blend_pal(t, trap), 1.0);
    }
	//COLOR = vec4(float(count) / float(MAX_ITERS), 0.0, 0.0, 1.0);
}
