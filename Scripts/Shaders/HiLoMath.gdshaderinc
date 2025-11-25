#include "res://Scripts/Shaders/ComplexMath.gdshaderinc"
vec2 addHiLo(vec2 a, vec2 b){
	float s = a.x + b.x;
	float z = s - a.x;
	float e = (a.x-(s-z))+(b.x-z);
	float t = e + a.y + b.y;
	float res_hi = s + t;
	float res_lo = t - (res_hi - s);
	return vec2(res_hi, res_lo);
}
vec2 subHiLo(vec2 a, vec2 b) {
	return addHiLo(a, -b);
}
vec2 multHiLo(vec2 a, vec2 b){
	float p = a.x * b.x;
	float e = fma(a.x, b.x, -p);
	e += a.x * b.y + a.y * b.x;
	e += a.y * b.y;
	float res_hi = p + e;
	float res_lo = e - (res_hi - p);
	return vec2(res_hi, res_lo);
}
vec2 divHiLo(vec2 a, vec2 b){
	float q = a.x / b.x;
	vec2 qb = multHiLo(vec2(q, 0.0), b);
	vec2 r = subHiLo(a, qb);
	float dq = (r.x + r.y) / b.x;
	float res_hi = q + dq;
	float res_lo = dq - (res_hi - q);
	return vec2(res_hi, res_lo);
}
vec2 splitFloat(float a){
	float c = float(1 << 13) + 1.0;
    float t = c * a;
    float big = t - (t - a);
	return vec2 (big, a-big);
}
vec2 maxHiLo(vec2 a, vec2 b) {
    bool useA = (a.x > b.x) || (a.x == b.x && a.y >= b.y);
    return useA ? a : b;
}
vec4 toHiLoVec2(vec2 v) {
    vec2 x = splitFloat(v.x);
    vec2 y = splitFloat(v.y);
    return vec4(x, y);
}
vec2 fromHiLoVec4(vec4 v) {
    return vec2(v.x + v.y, v.z + v.w);
}
vec4 transformHiLo(vec4 a, vec4 res, vec2 zoom, vec4 x) {
    vec2 ax = a.xy;
    vec2 ay = a.zw;
    vec2 rx = res.xy;
    vec2 ry = res.zw;
    vec2 xx = x.xy;
    vec2 xy = x.zw;

    const vec2 half = vec2(0.5, 0.0);
    const vec2 one  = vec2(1.0, 0.0);

    vec2 center_x = multHiLo(rx, half);
    vec2 center_y = multHiLo(ry, half);

    vec2 shifted_x = subHiLo(ax, center_x);
    vec2 shifted_y = subHiLo(ay, center_y);

    vec2 denom = maxHiLo(rx, ry);

    vec2 norm_x = divHiLo(shifted_x, denom);
    vec2 norm_y = divHiLo(shifted_y, denom);

    vec2 invZoom = divHiLo(one, zoom);

    vec2 scaled_x = multHiLo(norm_x, invZoom);
    vec2 scaled_y = multHiLo(norm_y, invZoom);

    vec2 res_x = addHiLo(scaled_x, xx);
    vec2 res_y = addHiLo(scaled_y, xy);

    return vec4(res_x, res_y);
}
vec2 magSqHiLo(vec4 a) {
    vec2 ax = a.xy;
    vec2 ay = a.zw;

    vec2 ax2 = multHiLo(ax, ax);
    vec2 ay2 = multHiLo(ay, ay);

    return addHiLo(ax2, ay2);
}
vec4 subHiLo4(vec4 a, vec4 b) {
    vec2 ax = a.xy;
    vec2 ay = a.zw;
    vec2 bx = b.xy;
    vec2 by = b.zw;

    vec2 rx = subHiLo(ax, bx);
    vec2 ry = subHiLo(ay, by);

    return vec4(rx, ry);
}
vec2 minHiLo(vec2 a, vec2 b) {
    bool useA = (a.x < b.x) || (a.x == b.x && a.y <= b.y);
    return useA ? a : b;
}
bool gtHiLo(vec2 a, vec2 b) {
    return (a.x > b.x) || (a.x == b.x && a.y > b.y);
}
vec2 sqrtHiLo(vec2 a) {
    // Handle zero explicitly
    if (a.x == 0.0 && a.y == 0.0) {
        return vec2(0.0, 0.0);
    }

    float y0 = sqrt(a.x);
    vec2 y = vec2(y0, 0.0);

    vec2 y2 = multHiLo(y, y);
    vec2 r  = subHiLo(a, y2);

    vec2 two  = vec2(2.0, 0.0);
    vec2 denom = multHiLo(two, y);

    // If denom is numerically zero, just return the initial sqrt
    if (denom.x == 0.0 && denom.y == 0.0) {
        return y;
    }

    vec2 dy = divHiLo(r, denom);
    return addHiLo(y, dy);
}

vec2 logHiLo(vec2 a) {
    float x = a.x + a.y;

    // Clamp away from non-positive to avoid log(0) and log(<0)
    const float MIN_POS = 1e-38;
    x = max(x, MIN_POS);

    float ln_x = log(x);
    vec2 x_dd = splitFloat(x);
    vec2 dx = subHiLo(a, x_dd);
    vec2 corr = divHiLo(dx, x_dd);
    return addHiLo(splitFloat(ln_x), corr);
}


vec2 magHiLo(vec4 a) {
    vec2 ax = a.xy;
    vec2 ay = a.zw;

    vec2 ax2 = multHiLo(ax, ax);
    vec2 ay2 = multHiLo(ay, ay);
    vec2 s = addHiLo(ax2, ay2);

    return sqrtHiLo(s);
}


bool ltHiLo(vec2 a, vec2 b) {
    return (a.x < b.x) || (a.x == b.x && a.y < b.y);
}
vec2 clampHiLo(vec2 v, vec2 lo, vec2 hi) {
    vec2 m = maxHiLo(v, lo);
    return minHiLo(m, hi);
}
vec4 complexAdd(vec4 a, vec4 b) {
    vec2 rx = addHiLo(a.xy, b.xy);
    vec2 ry = addHiLo(a.zw, b.zw);
    return vec4(rx, ry);
}
vec4 complexSub(vec4 a, vec4 b) {
    vec2 rx = subHiLo(a.xy, b.xy);
    vec2 ry = subHiLo(a.zw, b.zw);
    return vec4(rx, ry);
}
vec4 complexMult(vec4 a, vec4 b) {
    vec2 ax = a.xy;
    vec2 ay = a.zw;
    vec2 bx = b.xy;
    vec2 by = b.zw;

    vec2 xu = multHiLo(ax, bx);
    vec2 yv = multHiLo(ay, by);
    vec2 xv = multHiLo(ax, by);
    vec2 yu = multHiLo(ay, bx);

    vec2 real = subHiLo(xu, yv);
    vec2 imag = addHiLo(xv, yu);

    return vec4(real, imag);
}
vec4 complexDivide(vec4 a, vec4 b) {
    vec2 bx = b.xy;
    vec2 by = b.zw;

    vec2 bx2 = multHiLo(bx, bx);
    vec2 by2 = multHiLo(by, by);
    vec2 denom = addHiLo(bx2, by2);

    vec2 real_num = addHiLo(multHiLo(a.xy, bx), multHiLo(a.zw, by));
    vec2 imag_num = subHiLo(multHiLo(a.zw, bx), multHiLo(a.xy, by));

    vec2 real = divHiLo(real_num, denom);
    vec2 imag = divHiLo(imag_num, denom);

    return vec4(real, imag);
}
vec2 expHiLo(vec2 a) {
    float ex = exp(a.x);
    vec2 ex_dd = splitFloat(ex);
    vec2 corr = multHiLo(ex_dd, vec2(a.y, 0.0));
    return addHiLo(ex_dd, corr);
}
vec4 complex_pow_complex(vec4 z, vec4 w) {
    vec2 zx = z.xy;
    vec2 zy = z.zw;
    vec2 wx = w.xy;
    vec2 wy = w.zw;
	
	bool z_is_zero = (zx.x == 0.0 && zx.y == 0.0 && zy.x == 0.0 && zy.y == 0.0);
    if (z_is_zero) {
        return vec4(0.0);
    }

    vec2 zx2 = multHiLo(zx, zx);
    vec2 zy2 = multHiLo(zy, zy);
    vec2 r2 = addHiLo(zx2, zy2);
    vec2 r = sqrtHiLo(r2);

    float x = zx.x + zx.y;
    float y = zy.x + zy.y;
    float theta = atan(y, x);

    vec2 ln_r = logHiLo(r);
    vec2 theta_dd = splitFloat(theta);

    vec2 a_ln_r = multHiLo(wx, ln_r);
    vec2 b_theta = multHiLo(wy, theta_dd);
    vec2 real_part = subHiLo(a_ln_r, b_theta);

    vec2 a_theta = multHiLo(wx, theta_dd);
    vec2 b_ln_r = multHiLo(wy, ln_r);
    vec2 imag_part = addHiLo(a_theta, b_ln_r);

    vec2 mag = expHiLo(real_part);

    float angle = imag_part.x + imag_part.y;
    vec2 cos_ang = splitFloat(cos(angle));
    vec2 sin_ang = splitFloat(sin(angle));

    vec2 real = multHiLo(mag, cos_ang);
    vec2 imag = multHiLo(mag, sin_ang);

    return vec4(real, imag);
}
