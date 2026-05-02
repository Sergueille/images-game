shader_type canvas_item;

uniform int height = 30;
uniform int reso_x = 200;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 uv = UV;
	uv.y = float(int(uv.y * 0.5 * float(height) + 0.2)) / float(height) * 2.0;
	
  	vec4 color = texture(TEXTURE, uv);
  	vec4 color_l = texture(TEXTURE, uv + vec2(1.0 / float(reso_x), 0));
  	vec4 color_r = texture(TEXTURE, uv - vec2(1.0 / float(reso_x), 0));
  	vec4 color_ll = texture(TEXTURE, uv + vec2(2.0 / float(reso_x), 0));
  	vec4 color_rr = texture(TEXTURE, uv - vec2(2.0 / float(reso_x), 0));
	
	color = (color + color_l + color_r + color_ll + color_rr) * 0.2;
	
	float sval = sin(UV.y * 3.1416 * float(height));
	color.a *= 0.5 + 0.5 * sval;
	
	COLOR = color;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
