shader_type canvas_item;

varying vec3 world_position;

uniform float brightness = 0.0;
uniform float saturation = 0.0;
uniform float hueShift = 0.0;
uniform sampler2D grunge: repeat_enable;

void vertex() {
	mat4 transMat = mat4(
		vec4(1.0, 0.0, 0.0, 0.0),
		vec4(0.0, 1.0, 0.0, 0.0),
		vec4(0.0, 0.0, 1.0, 0.0),
		vec4(0.0, 0.0, 0.0, 0.0)
	);
	
    world_position = (MODEL_MATRIX * transMat * vec4(VERTEX, 0.0, 1.0)).xyz;
}

void fragment() {
  	vec4 color = texture(TEXTURE, UV);
	
	vec3 gray = vec3(0.57735);
	vec3 projection = gray * dot(gray, color.rgb);
	vec3 U = color.rgb - projection;
	vec3 V = cross(gray, U);
	
	vec3 shifted = U * cos(hueShift * 2.0 * PI) + V * sin(hueShift * 2.0 * PI);
	
	vec3 res_color = shifted* (1.0 + saturation) + projection * (1.0 + brightness);
	vec2 grugeUV = vec2(world_position.x, world_position.y) / 600.0;
	
	float grungeVal = (0.8 - texture(grunge, grugeUV).r) * 1.0 * (1.6 - dot(res_color, res_color));
	
	COLOR = vec4(res_color + grungeVal, color.a);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
