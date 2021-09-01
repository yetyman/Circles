#version 330 core
in vec2 texCoord;
uniform sampler2D fromMap;

out vec4 FragColor;

void main()
{
	//just cutting off values above and below zero
	vec4 fuel = texture(fromMap, texCoord);
	if(fuel.r<0 || fuel.g<0 || fuel.b<0)
	{
		fuel = vec4(0,0,0,1);
	}
	else if(fuel.r>1 || fuel.g>1 || fuel.b>1)
	{
		fuel = vec4(1,1,1,1);
	}
	FragColor = fuel;
}