shader_type canvas_item;
#include "res://Scripts/Shaders/ComplexMath.gdshaderinc"
uniform vec2 offset;
uniform float zoomFactor;
uniform bool render = false;
uniform bool prime = false;


vec2 func(vec2 z) {
	vec2 result = vec2(0.00);
    return result;
}
vec2 funcPrime(vec2 z){
	float EPSILON = 1e-3;
	vec2 new = complexDivide(func(z + EPSILON) - func(z), vec2(EPSILON));
	return new;
}

void fragment() {
	vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	vec2 scale = transform(vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y), res, zoomFactor, offset);
	//- vec2(16.0/9.0,1);

	vec2 z = func(scale);
	if (prime){
		z = funcPrime(scale);
	}
	float saturation = 1.0;
	float value = 1.0;
	if (render){
		float radius = length(z);
		//saturation = 1.0 - (1.0 / (1.0 + 0.3 * radius));
		value = 1.0 - exp(-0.5 * radius);
	}
	COLOR = vec4(hsv_to_rgb((atan(z.y,z.x) + PI) * (180.0 / PI),saturation,value),1.0);
}
