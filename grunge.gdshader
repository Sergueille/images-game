shader_type canvas_item;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec4 c = texture(TEXTURE, UV);
	COLOR = vec4(0, 0, 0, .2 - c.r * .2);
}
