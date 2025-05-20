shader_type canvas_item;
#include "res://Scripts/Shaders/ComplexMath.gdshaderinc"
uniform vec2 offset = vec2(0.0,0.0);
uniform float zoomFactor = 0.1;
uniform vec2[100] roots;
uniform int idClose = -1;
uniform int rootCount = 0;
uniform int color = 0;

void fragment(){
    vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
    vec2 c = transform(vec2(UV.x/SCREEN_PIXEL_SIZE.x, UV.y/SCREEN_PIXEL_SIZE.y), res, zoomFactor, offset);
	vec2 newC = c;
	for (int j = 0; j < 600; j++){
		vec2 result = vec2(1,0);

		vec2 prime = vec2(0);

		for (int i = 0; i < rootCount; i++){
			vec2 root = roots[i];
			result = complexMult(result, (newC - root));
			prime += complexDivide(vec2(1,0), (newC - root));
		}
		prime = complexMult(result,prime);
		newC = newC - complexDivide(result, prime);
		if (length(result) < 1e-4){
			break;
		}
		 // using analytical derivative NOT numerical
	}
	float best = 1.0 / 0.0;
	int id = -1;
	for (int i = 0; i < rootCount; i++){
		float d = distance(roots[i], newC);
		if (d < best){
			best = d;
			id = i;
		}
	}
	if (id == -1){
		COLOR = vec4(1.0,1.0,1.0,1.0);
	}
	else{
		switch (color){
			case 0:
				COLOR = vec4(float(id) / float(rootCount), 0.0, 0.0, 1.0);
				break;
			case 1:
				COLOR = vec4(0.0, 0.0, float(id) / float(rootCount), 1.0);
				break;
			case 2:
				COLOR = vec4(1.0-float(id) / float(rootCount), 0.0, float(id) / float(rootCount), 1.0);
				break;
			case 3:
				COLOR = vec4(hsv_to_rgb(float(id) * (360.0 / float(rootCount)),1.0,1.0), 1.0);
				break;
			default:
				COLOR = vec4(0.0, float(id) / float(rootCount), 0.0, 1.0);
				break;
		}
	}
	for (int i = 0; i < rootCount; i++){
		float d = distance(roots[i], c);

		if (d < 0.002 / zoomFactor){
			COLOR = vec4(0.0,1.0,0.0,idClose == i?1.0:0.4);
		}
	}
}