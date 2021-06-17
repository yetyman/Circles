#version 330 core
in vec2 texCoord;
uniform sampler2D fromMap;
uniform sampler2D subtractMap;
uniform float addValue;

out vec4 FragColor;

void main()
{
	FragColor = texture(subtractMap, texCoord) - vec4(addValue, addValue, addValue, 0);
}