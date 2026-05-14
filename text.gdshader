shader_type canvas_item;

varying vec3 world_position;

uniform float amplitude;
uniform float spatial_freq;
uniform float time_freq;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xyz;
	VERTEX.y += amplitude * sin(
		(UV.x * spatial_freq + mod(TIME, 1.0/time_freq) * time_freq) * 6.283
	);
}

void fragment() {
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
