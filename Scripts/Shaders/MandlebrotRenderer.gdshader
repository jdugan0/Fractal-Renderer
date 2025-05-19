shader_type canvas_item;
#include "res://Scripts/Shaders/ComplexMath.gdshaderinc"
uniform vec2 offset = vec2(0.0,0.0);
uniform float zoomFactor = 0.1;


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

void fragment(){
    vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
    vec2 c = transform(vec2(UV.x/SCREEN_PIXEL_SIZE.x, UV.y/SCREEN_PIXEL_SIZE.y), res, zoomFactor, offset);

    vec2 z = func(vec2(0.0),c);
    float trap = 1e10;
    float mu   = 0.0;
    bool esc   = false;
    const int MAX_ITERS = 250;
	const int MIN_ITERS = 25;
	vec2 last = z;
	vec2 epsilon = vec2(1e-6);
	vec2 lastPrime = (func(vec2(0.0) + epsilon,c) - func(vec2(0.0),c)) / epsilon;
	float bailout = 256.0;
    for(int i=0;i<MAX_ITERS;i++){
        z = func(z, c);
		vec2 d1 = z - last;
		vec2 prime = (func(z + epsilon, c) - func(z,c)) / epsilon;
		vec2 dprime = prime - lastPrime;
		//if ((dot(d1,d1) < 1e-15) && i > MIN_ITERS){
			//esc = false;
			//break;
		//}
		vec2 delta = z - c;
        float r2 = dot(delta,delta);
        trap = min(trap, r2);
        if(r2 > bailout){
			float safe_r2 = max(r2, 1.000001); 
            mu = float(i) - log2(log2(safe_r2)) + log2(log2(bailout));
            esc = true;
            break;
        }
		last = z;
		lastPrime = prime;
    }

    if(!esc){ COLOR = vec4(0.0,0.0,0.0,1.0); }
    else{
        float t = mu / float(MAX_ITERS);
        COLOR = vec4(blend_pal(t, trap), 1.0);
    }
}