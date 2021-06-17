#version 330 core
in vec2 texCoord;
uniform sampler2D fromMap;
uniform sampler2D subtractMap;

out vec4 FragColor;

void main()
{
	vec4 req = texture(subtractMap, texCoord);
	vec4 fuel = texture(fromMap, texCoord);
	vec4 clr = req;
	if(fuel.r<0 || fuel.g<0 || fuel.b<0)
	{
		clr = fuel+req;//will be less than req.
	}
	FragColor = clr/req;
}