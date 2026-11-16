/* Minimal <stdint.h> for x86_64 Linux (LP64): the generator parses the Linux headers without a Linux sysroot. */
#pragma once
typedef signed char int8_t;
typedef short int16_t;
typedef int int32_t;
typedef long int64_t;
typedef unsigned char uint8_t;
typedef unsigned short uint16_t;
typedef unsigned int uint32_t;
typedef unsigned long uint64_t;
typedef unsigned short uint_least16_t;
typedef unsigned int uint_least32_t;
typedef long intptr_t;
typedef unsigned long uintptr_t;
#define INT32_MAX 2147483647
#define INT64_MAX 9223372036854775807L
#define UINT32_MAX 4294967295U
#define UINT64_MAX 18446744073709551615UL
