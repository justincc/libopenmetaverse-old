
#ifndef LIBSL_H
#define LIBSL_H

#include "../libopenjpeg/openjpeg.h"

#ifdef WIN64
#	define LibomvFunc(name) name ## 64
#else
#	define LibomvFunc(name) name
#endif

struct MarshalledImage
{
	unsigned char* encoded;
	int length;
	int dummy; // padding for 64-bit alignment

	unsigned char* decoded;
	int width;
	int height;
	int layers;
	int resolutions;
	int components;
	int packet_count;
	opj_packet_info_t* packets;
};

#ifdef WIN32
#define DLLEXPORT extern "C" __declspec(dllexport)
#else
#define DLLEXPORT extern "C"
#endif

// uncompresed images are raw RGBA 8bit/channel
DLLEXPORT bool LibomvFunc(DotNetEncode)(MarshalledImage* image, bool lossless);
DLLEXPORT bool LibomvFunc(DotNetDecode)(MarshalledImage* image);
DLLEXPORT bool LibomvFunc(DotNetDecodeWithInfo)(MarshalledImage* image);
DLLEXPORT bool LibomvFunc(DotNetAllocEncoded)(MarshalledImage* image);
DLLEXPORT bool LibomvFunc(DotNetAllocDecoded)(MarshalledImage* image);
DLLEXPORT void LibomvFunc(DotNetFree)(MarshalledImage* image);

#endif
