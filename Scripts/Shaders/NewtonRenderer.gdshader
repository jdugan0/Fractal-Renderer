	shader_type canvas_item;
	#include "res://Scripts/Shaders/ComplexMath.gdshaderinc"
	uniform vec2 offset = vec2(0.0,0.0);
	uniform float zoomFactor = 0.1;
	uniform vec2[100] roots;
	uniform int idClose = -1;
	uniform int rootCount = 0;
	uniform int color = 0;
	uniform bool fancy_shading = false;
	uniform int max_iters = 600;
	uniform float saturation_factor = 1.0;
	uniform float julia_highlight   = 0.0;
	void fragment(){
	    vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	    vec2 c = transform(vec2(UV.x/SCREEN_PIXEL_SIZE.x, UV.y/SCREEN_PIXEL_SIZE.y), res, zoomFactor, offset);
		vec2 newC = c;
		float tint = 0.0;
		float shade = 1.0;
		float residual = 1.0;
		for (int j = 0; j < max_iters; j++){
			vec2 result = vec2(1,0);

			vec2 prime = vec2(0);

			for (int i = 0; i < rootCount; i++){
				vec2 root = roots[i];
				result = complexMult(result, (newC - root));
				prime += complexDivide(vec2(1,0), (newC - root));
			}
			result = (1.0/float(rootCount)) * result;
			prime = complexMult(result,prime);
			newC = newC - complexDivide(result, prime);
			if (length(result) < 1e-3){
				shade  = float(j) / float(max_iters);
				residual = clamp(log(length(result)+1e-9), -10.0, 0.0);
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
			vec3 base;
			shade = pow(shade, 0.6);
			float rshade = 1.0 + residual / 10.0;
			float shade_new = 0.6 * shade + 0.4 * rshade;
			switch (color){
				case 0:
					base = vec3(float(id) / float(rootCount), 0.0, 0.0);
					break;
				case 1:
					base = vec3(0.0, 0.0, float(id) / float(rootCount));
					break;
				case 2:
					base = vec3(1.0-float(id) / float(rootCount), 0.0, float(id) / float(rootCount));
					break;
				case 3:
					base = vec3(hsv_to_rgb(float(id) * (360.0 / float(rootCount)),0.9,0.7));
					break;
				default:
					base = vec3(0.0, float(id) / float(rootCount), 0.0);
					break;
			}
			vec3 final_rgb = mix(base, mix(vec3(0.15, 0.15, 0.15), base, 0.5), rshade);
			if (fancy_shading){
				COLOR = vec4(final_rgb,1.0);
			}
			else{
				COLOR = vec4(base, 1.0);
			}
		}
		for (int i = 0; i < rootCount; i++){
			float d = distance(roots[i], c);

			if (d < 0.002 / zoomFactor){
				COLOR = vec4(0.0,1.0,0.0,idClose == i?1.0:0.4);
			}
		}
	}