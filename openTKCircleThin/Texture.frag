#version 330 core
in vec2 texCoord;
uniform sampler2D opacityMap;

uniform vec4 LayerColor;

out vec4 FragColor;

void main()
{
	FragColor = vec4(LayerColor.rgb, texture(opacityMap, texCoord).r * LayerColor.a);
}