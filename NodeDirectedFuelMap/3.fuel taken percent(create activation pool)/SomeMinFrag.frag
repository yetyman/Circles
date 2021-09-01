#version 330 core
in vec2 texCoord;
uniform sampler2D fromMap;
uniform sampler2D subtractMap;

out vec4 FragColor;

void main()
{
	//the purpose of this algorithm is to limit negative values in the fuel pool to what is over the available fuel. setting it negative in this way instead of zeroing it out simplifies some later math
	vec4 req = texture(subtractMap, texCoord);
	vec4 fuel = texture(fromMap, texCoord);
	vec4 clr = req;
	if(fuel.r<0 || fuel.g<0 || fuel.b<0)
	{
		clr = fuel+req;//will be less than req.
	}
	FragColor = clr/req;
}