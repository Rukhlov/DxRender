////////////////////////////////////////////////////
// ps 2.0
////////////////////////////////////////////////////
texture _texture;
////////////////////////////////////////////////////
sampler2D _sampler =
sampler_state
{
    Texture = <_texture>;
    AddressU = Clamp;
    AddressV = Clamp;
    MinFilter = Point;
    MagFilter = Linear;
    MipFilter = Linear;
};
////////////////////////////////////////////////////
const float4 g_cf4Luminance = { 0.2125f, 0.7154f, 0.0721f, 0.0f };
const float g_cfSepiaDepth = 0.15;
float cBrightness = 1.0;
////////////////////////////////////////////////////
float4 Simple_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	return tex2D( _sampler, _pos);
}
////////////////////////////////////////////////////
float4 GrayScale_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	return dot((float4)tex2D( _sampler, _pos), g_cf4Luminance);
}
////////////////////////////////////////////////////
float4 Sharp_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	float4 _color = tex2D( _sampler, _pos);
	_color -= tex2D( _sampler, _pos+0.001)*3.0f;
	_color += tex2D( _sampler, _pos-0.001)*3.0f;
	return _color;
}
////////////////////////////////////////////////////
float4 Sepia_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	float4 _color = dot( (float4)tex2D( _sampler, _pos ), g_cf4Luminance );
	_color.xyz += float3(g_cfSepiaDepth * 2,g_cfSepiaDepth,g_cfSepiaDepth / 2);
	return _color;
}
////////////////////////////////////////////////////
float4 Invert_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	return 1.0f - tex2D( _sampler, _pos);
}
////////////////////////////////////////////////////
float4 Emboss_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	float4 _color;
	_color.a = 1.0f;
	_color.rgb = 0.5f;
	_color -= tex2D( _sampler, _pos.xy-0.001)*2.0f;
	_color += tex2D( _sampler, _pos.xy+0.001)*2.0f;
	_color.rgb = (_color.r+_color.g+_color.b)/3.0f;
	return _color;
}
////////////////////////////////////////////////////
float4 Blur_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	const int nSamples = 13;
	const float2 cSamples[nSamples] = {
		 0.000000,  0.000000,
   		-0.326212, -0.405805,
   		-0.840144, -0.073580,
   		-0.695914,  0.457137,
   		-0.203345,  0.620716,
		 0.962340, -0.194983,
		 0.473434, -0.480026,
		 0.519456,  0.767022,
		 0.185461, -0.893124,
		 0.507431,  0.064425,
		 0.896420,  0.412458,
   		-0.321940, -0.932615,
   		-0.791559, -0.597705,
	};
	float4 sum = 0;
	for (int i = 0; i < nSamples - 1; i++)
	{
		sum += tex2D(_sampler, _pos + 0.025 * cSamples[i]);
	}
	return sum / nSamples;
}
////////////////////////////////////////////////////
float4 Posterize_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	const float cColors = 8.0f;
	const float cGamma = 0.6f;
	float4 _color = tex2D(_sampler, _pos);
	float3 tc = _color.xyz;
	tc = pow(tc, cGamma);
	tc = tc * cColors;
	tc = floor(tc);
	tc = tc / cColors;
	tc = pow(tc,1.0/cGamma);
	return float4(tc,_color.w);
}
////////////////////////////////////////////////////
float4 Brightness_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	//const float cBrightness = 2.0;
	float4 _color = tex2D( _sampler, _pos);
	_color.xyz *= cBrightness;
	return _color;
}
////////////////////////////////////////////////////
float4 Red_Proc(float2 _pos: TEXCOORD0) : COLOR0
{
	const float cRedCutOff = 0.5;
	const float cRedBrightness = 1.2;
	float4 _color = tex2D( _sampler, _pos);
	float4 _result = dot( _color, g_cf4Luminance );
	if (_color.r * 2 - cRedCutOff > _color.g + _color.b)
	{
		_result.r = _color.r * cRedBrightness;
	}
	return _result;
}
////////////////////////////////////////////////////
// Techniques
////////////////////////////////////////////////////
technique Simple_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Simple_Proc();	
	}
}
////////////////////////////////////////////////////
technique GrayScale_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 GrayScale_Proc();
	}
}
////////////////////////////////////////////////////
technique Sharp_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Sharp_Proc();
	}
}
////////////////////////////////////////////////////
technique Sepia_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Sepia_Proc();
	}
}
////////////////////////////////////////////////////
technique Invert_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Invert_Proc();
	}
}
////////////////////////////////////////////////////
technique Emboss_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Emboss_Proc();
	}
}
////////////////////////////////////////////////////
technique Blur_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Blur_Proc();
	}
}
////////////////////////////////////////////////////
technique Posterize_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Posterize_Proc();
	}
}
////////////////////////////////////////////////////
technique Brightness_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Brightness_Proc();
	}
}
////////////////////////////////////////////////////
technique Red_Technique
{
	pass p0
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Red_Proc();
	}
}
////////////////////////////////////////////////////