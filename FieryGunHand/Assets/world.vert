#version 330 core

layout (location = 0) in vec3 i_position;
layout (location = 1) in vec2 i_texCoord;
layout (location = 2) in vec4 i_colour;

out vec2 s_texCoord;
out vec4 s_colour;

uniform mat4 u_projection;
uniform mat4 u_modelView;

void main() {
    gl_Position = u_projection * u_modelView * vec4(i_position, 1);
    
    s_texCoord = i_texCoord;
	s_colour = i_colour;
}