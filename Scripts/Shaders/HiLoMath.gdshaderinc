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