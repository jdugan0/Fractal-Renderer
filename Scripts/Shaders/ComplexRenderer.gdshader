shader_type canvas_item;
uniform vec2 offset;
uniform float zoomFactor;
uniform bool render = false;
uniform bool prime = false;

vec2 complexMult(vec2 one, vec2 two){
	return vec2(one.x * two.x - one.y * two.y, one.x * two.y + one.y * two.x);
}
vec2 complexLn(vec2 a){
	return vec2(log(length(a)),atan(a.y,a.x));
}


// z  => (x, y)
// w  => (a, b)
vec2 complex_pow_complex(vec2 z, vec2 w) {
    // Convert z to polar form: z = r e^{iθ}
    float r = length(z);              // sqrt(x^2 + y^2)
    float theta = atan(z.y, z.x); // or built-in atan2 if available

    // w = a + i b
    float a = w.x;
    float b = w.y;

    // We'll compute z^w = e^{w * ln(z)} = e^{(a + i b) * (ln(r) + iθ)}

    // Real part of (a + i b) * (ln(r) + i θ) = a ln(r) - b θ
    float real_part = a * log(r + 1e-8) - b * theta;

    // Imag part of (a + i b) * (ln(r) + i θ) = a θ + b ln(r)
    float imag_part = a * theta + b * log(r + 1e-8);

    // e^{ real_part } is the magnitude
    float magnitude = exp(real_part);

    // imag_part is the new angle
    float angle = imag_part;

    // Finally convert from polar back to cartesian
    return vec2(
        magnitude * cos(angle),
        magnitude * sin(angle)
    );
}
vec2 complexSin(vec2 a){
	vec2 e_iz = complex_pow_complex(vec2(E,0),vec2(-a.y,a.x));
	vec2 e_izN = complex_pow_complex(vec2(E,0),complexMult(vec2(-1.0,0),vec2(-a.y,a.x)));
	vec2 z = (e_iz - e_izN) / 2.0;
	return vec2(z.y, -z.x);
}
vec2 complexCos(vec2 a){
	vec2 e_iz = complex_pow_complex(vec2(E,0),vec2(-a.y,a.x));
	vec2 e_izN = complex_pow_complex(vec2(E,0),complexMult(vec2(-1.0,0),vec2(-a.y,a.x)));
	vec2 z = (e_iz + e_izN) / 2.0;
	return z;
}
vec2 complexDivide(vec2 a, vec2 b) {
    float denom = b.x * b.x + b.y * b.y + 1e-8;
    return vec2(
        (a.x * b.x + a.y * b.y) / denom,
        (a.y * b.x - a.x * b.y) / denom
    );
}
vec2 complexTan(vec2 a){
	return (complexDivide(complexSin(a), complexCos(a)));
}

// This function approximates Gamma(z) for Re(z) >= 0.5
// using the Lanczos method (no reflection).
vec2 complexGammaLanczos(vec2 z) {
    // Lanczos coefficients
    // p[0], p[1], ..., p[8]  (9 coefficients)
    float p[9] = float[](
        0.99999999999980993,
        676.5203681218851,
        -1259.1392167224028,
        771.32342877765313,
        -176.61502916214059,
        12.507343278686905,
        -0.13857109526572012,
        9.9843695780195716e-6,
        1.5056327351493116e-7
    );

    float g = 7.0; // Lanczos shift
    float twoPi = 2.0 * PI;
    float sqrt2Pi = sqrt(twoPi);

    // Summation part: x = p[0] + p[1]/(z+1) + p[2]/(z+2) + ...
    vec2 x = vec2(p[0], 0.0);

    // We'll do i from 1..8
    // NOTE: In Godot 4's shading language, for-loops with non-constant bounds
    // might need special handling. If it doesn't compile, unroll it manually.
    for (int i = 1; i < 9; i++) {
        vec2 term = complexDivide(vec2(p[i], 0.0), z + vec2(float(i), 0.0));
        x = x+ term;
    }

    // t = z + g + 0.5
    vec2 t = z+vec2(g + 0.5, 0.0);

    // We do (t)^(z + 0.5) * e^(-t) * x * sqrt(2π)
    // basePow = t^(z + 0.5)
    vec2 exponent = (z+ vec2(0.5, 0.0));
    vec2 basePow  = complex_pow_complex(t, exponent);

    // e^(-t)
    vec2 negT = vec2(-t.x, -t.y);
    vec2 expTerm = complex_pow_complex(vec2(E,0.0), negT);

    // Multiply all
    vec2 y = vec2(sqrt2Pi, 0.0);       // sqrt(2π)
    y = complexMult(y, basePow);
    y = complexMult(y, expTerm);
    y = complexMult(y, x);

    return y;
}

//------------------------------------------------------------------------------
// 4) Shifting Approach: "complexGamma"
//
//    If Re(z) >= 0.5, use Lanczos directly.
//    Else shift z in integer steps so that Re(z + n) >= 0.5.
//    Then use the identity:
//       Γ(z) = Γ(z + n) / [z*(z+1)*...*(z + n - 1)]
//------------------------------------------------------------------------------
vec2 complexProductRange(vec2 z, int n) {
    // multiply (z + 0)*(z + 1)*...*(z + n -1)
    vec2 result = vec2(1.0, 0.0);
    for (int i = 0; i < n; i++) {
        vec2 term = (z+ vec2(float(i), 0.0));
        result = complexMult(result, term);
    }
    return result;
}

// The main function that the user calls
vec2 complexGamma(vec2 z) {
    float re_z = z.x;  // real part of z

    if (re_z >= 0.5) {
        // no shifting needed
        return complexGammaLanczos(z);
    } else {
        // shift up so that Re(z + n) >= 0.5
        float shiftNeeded = 0.5 - re_z;
        float shiftFloat = ceil(shiftNeeded);  // e.g. if z.x=0.2 => shiftNeeded=0.3 => shift=1
        int n = int(shiftFloat);

        // compute Gamma(z + n) with Lanczos
        vec2 zPlusN = (z + vec2(float(n), 0.0));
        vec2 gammaZN = complexGammaLanczos(zPlusN);

        // denominator = z*(z+1)*...*(z + n-1)
        vec2 denom = complexProductRange(z, n);

        return complexDivide(gammaZN, denom);
    }
}

vec2 complexSub(vec2 a, vec2 b){
	return a - b;
}
vec2 complexAdd(vec2 a, vec2 b){
	return a + b;
}

vec2 complexAcos(vec2 z){
	vec2 zSq = complex_pow_complex(z, vec2(2.0,0.0));
	vec2 inner = complexAdd(z, complex_pow_complex(complexAdd(zSq, vec2(-1.0,0.0)),vec2(0.5, 0.0)));
	vec2 ln = complexLn(inner);
	return complexMult(vec2(0, -1), ln);
}
vec2 transform(vec2 a, vec2 res){

    // Instead of hard-coded 1920/1080
    vec2 center = vec2(res.x * 0.5, res.y * 0.5);
    // Subtract center
    vec2 shifted = a - center;
    // Divide by max dimension (or some other measure)
    vec2 normalized = shifted / max(res.x, res.y);
    // Then scale by 1/zoomFactor + offset
    return normalized / zoomFactor + offset;
}
vec3 hsv_to_rgb(float hue_degrees, float saturation, float value)
{
    // Normalize hue to [0, 360)
    float h = mod(hue_degrees, 360.0);
    float s = clamp(saturation, 0.0, 1.0);
    float v = clamp(value, 0.0, 1.0);

    // "c" is chroma, "x" is the offset, "m" is to shift back into [0,1]
    float c = v * s;
    float x = c * (1.0 - abs(mod(h / 60.0, 2.0) - 1.0));
    float m = v - c;

    vec3 rgb;

    if (h < 60.0) {
        rgb = vec3(c, x, 0.0);
    } else if (h < 120.0) {
        rgb = vec3(x, c, 0.0);
    } else if (h < 180.0) {
        rgb = vec3(0.0, c, x);
    } else if (h < 240.0) {
        rgb = vec3(0.0, x, c);
    } else if (h < 300.0) {
        rgb = vec3(x, 0.0, c);
    } else {
        // 300 <= h < 360
        rgb = vec3(c, 0.0, x);
    }

    return rgb + vec3(m, m, m);
}

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
	bool early = false;
	vec2 res = vec2(1.0/SCREEN_PIXEL_SIZE.x, 1.0/SCREEN_PIXEL_SIZE.y);
	vec2 scale = transform(vec2((UV.x) / SCREEN_PIXEL_SIZE.x, (UV.y) /SCREEN_PIXEL_SIZE.y), res);
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
