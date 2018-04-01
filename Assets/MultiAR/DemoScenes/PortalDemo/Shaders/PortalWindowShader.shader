Shader "Portal/PortalWindowShader"
{
	SubShader
	{
		ZWrite off
		Colormask 0
		Cull off

		Stencil
		{
			Ref 1
			Pass replace
		}

		Pass
		{

		}
	}
}
