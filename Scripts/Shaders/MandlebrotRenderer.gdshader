shader_type canvas_item;
#include "res://Scripts/Shaders/HiLoMath.gdshaderinc"
uniform vec2 offset_hi = vec2(0.0,0.0);
uniform vec2 offset_lo = vec2(0.0,0.0);
uniform float zoomFactor_hi = 0.1;
uniform float zoomFactor_lo = 0;

uniform vec2 juliaPoint_hi = vec2(0.0, 0.0);
uniform vec2 juliaPoint_lo = vec2(0.0, 0.0);
uniform bool julia = false;

uniform bool intColoring = false;


vec4 func(vec4 z, vec4 c) {
	vec4 result = vec4(0.00);
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

bool non_finite4(vec4 v) {
    return any(isnan(v)) || any(isinf(v));
}


void fragment() {
	vec2 zoomFactor = vec2(zoomFactor_hi, zoomFactor_lo);
	vec4 offset = vec4(offset_hi.x, offset_lo.x, offset_hi.y, offset_lo.y);
	vec4 juliaPoint = vec4(juliaPoint_hi.x, juliaPoint_lo.x, juliaPoint_hi.y, juliaPoint_lo.y);
    vec4 res = toHiLoVec2(vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y));
    vec4 c = transformHiLo(toHiLoVec2(vec2(UV.x/SCREEN_PIXEL_SIZE.x,
                              UV.y/SCREEN_PIXEL_SIZE.y)),
                         res, zoomFactor, offset);
	vec4 start = vec4(0.0);
	if (julia) {
		start = c;
		c = juliaPoint;
	}
    vec4 z = func(start, c);

    vec2 trap = splitFloat(1e20);
    vec2 mu = vec2(0.0);
    bool esc = false;
    bool cycle = false;

    const int MAX_ITERS = 250;
    const int MIN_ITERS = 20;
	bool mu_lock = false;

    vec2 bailout = splitFloat(256.0);
    vec2 eps = splitFloat(1e-5);   // tolerance for “same value”

    vec4 tortoise = z;  // reference point
    int power = 1;  // when to move the reference
    int lam = 0;  // distance travelled since last reset
	int count;
	const int PERIOD = 20;
	vec4 z_prev = z;
	int pcount = 0;

    for (int i = 0; i < MAX_ITERS; ++i) {
		count = i;
        /* normal fractal iteration */
        z = func(z, c);

		//if (ltHiLo(magHiLo(subHiLo4(z, z_prev)), eps)) {
            //cycle = true;
            //break;
        //}
        vec4 delta = subHiLo4(z, c);
        vec2 r2 = magSqHiLo(delta);
        trap = minHiLo(trap, r2);

		if (non_finite4(z)) {
	        esc = true;
	        if (!mu_lock){
				mu = splitFloat(float(i));
			}
	        break;
    	}


        if (gtHiLo(r2, bailout)) {
			if (!mu_lock){
				vec2 logR_prev = logHiLo(magHiLo(z_prev));
				vec2 logR_curr = logHiLo(magHiLo(z));
				vec2 slope = subHiLo(logR_curr, logR_prev);
				slope = maxHiLo(slope, splitFloat(1e-7));
				vec2 frac = divHiLo((subHiLo(logHiLo(bailout), logR_prev)), slope);
				mu = addHiLo(splitFloat(float(i - 1)), clampHiLo(frac, splitFloat(0.0), splitFloat(1.0)));
				mu_lock = true;
			}
			if (i > MIN_ITERS){
				esc = true;
           		break;
			}
        }
        ++lam;
		vec4 tortoiseDelta = subHiLo4(z, tortoise);
        if (ltHiLo(magSqHiLo(tortoiseDelta), multHiLo(eps, eps))) {
            cycle = true;
            break;
        }
        if (lam == power) {     // time to shift the reference forward
            tortoise = z;
            power <<= 1;      // power *= 2
            lam  = 0;
        }
		z_prev = z;
    }

    if (!esc) {
		vec2 logR_prev = logHiLo(magHiLo(z_prev));
		vec2 logR_curr = logHiLo(magHiLo(z));
		vec2 slope = subHiLo(logR_curr, logR_prev);
		slope = maxHiLo(slope, splitFloat(1e-7));
		vec2 frac = divHiLo((subHiLo(logHiLo(bailout), logR_prev)), slope);
		vec2 mu1 = addHiLo(splitFloat(float(count - 1)), clampHiLo(frac, splitFloat(0.0), splitFloat(1.0)));
        COLOR = vec4(!intColoring ? 0.0 : (mu1.x + mu1.y)/float(MAX_ITERS), 0.0, 0.0, 1.0);
		//if (cycle){
			//COLOR = vec4(1.0, 1.0, 1.0, 1.0);
		//}
    } else {
        float t = (mu.x + mu.y) / float(MAX_ITERS);
        COLOR = vec4(blend_pal(t, trap.x + trap.y), 1.0);
    }
	//COLOR = vec4(float(count) / float(MAX_ITERS), 0.0, 0.0, 1.0);
}