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
