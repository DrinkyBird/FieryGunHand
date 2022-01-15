#version 330 core

in vec2 s_texCoord;
in vec4 s_colour;

out vec4 o_fragColour;

uniform sampler2D u_texture;

void main() {
	o_fragColour = s_colour * texture(u_texture, s_texCoord);
}