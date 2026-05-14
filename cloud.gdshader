shader_type canvas_item;

uniform float amplitude = 0.05;
uniform float spatial_frequency = 0.3;
uniform float time_frequency = 0.3;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 uv = UV + amplitude * vec2(
		0.0, 
		sin((UV.x * spatial_frequency + TIME * time_frequency) * 6.283)
	);
	COLOR = texture(TEXTURE, uv);
	COLOR.a *= 0.9;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
