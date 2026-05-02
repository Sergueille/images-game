shader_type canvas_item;

uniform float brightness = 0.0;
uniform float saturation = 0.0;
uniform float hueShift = 0.0;

void vertex() {
}

void fragment() {
  	vec4 color = texture(TEXTURE, UV);
	
	vec3 gray = vec3(0.57735);
	vec3 projection = gray * dot(gray, color.rgb);
	vec3 U = color.rgb - projection;
	vec3 V = cross(gray, U);
	
	vec3 shifted = U * cos(hueShift * 2.0 * PI) + V * sin(hueShift * 2.0 * PI);
	
	COLOR = vec4(shifted* (1.0 + saturation) + projection * (1.0 + brightness), color.a);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
