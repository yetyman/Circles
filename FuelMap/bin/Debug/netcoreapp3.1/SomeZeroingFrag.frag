#version 330 core
in vec2 texCoord;
uniform sampler2D fromMap;

out vec4 FragColor;

void main()
{
	vec4 fuel = texture(fromMap, texCoord);
	if(fuel.r<0 || fuel.g<0 || fuel.b<0)
	{
		FragColor = vec4(0,0,0,fuel.a);
	}
	else FragColor = fuel;
}