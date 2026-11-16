/* Minimal <stddef.h> for x86_64 Linux: the generator parses the Linux headers without a Linux sysroot. */
#pragma once
typedef unsigned long size_t;
typedef long ptrdiff_t;
typedef int wchar_t;
#define NULL ((void*)0)
#define offsetof(type, member) __builtin_offsetof(type, member)
