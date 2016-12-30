// Itp.Win32.MdiHook.Injector.h

#pragma once

#include "AssemblySearchPath.h"

__declspec(dllexport)
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;

namespace Itp::Win32::MdiHook 
{
	private value struct HookParams
	{
		[MarshalAs(UnmanagedType::ByValTStr, SizeConst = 512)]
		String^ TypeName;
		[MarshalAs(UnmanagedType::ByValTStr, SizeConst = MAX_PATH + 1)]
		String^ Codebase;
		[MarshalAs(UnmanagedType::ByValTStr, SizeConst = MAX_PATH + 1)]
		String^ AdditionalSearchPath;
	};

	// abstract sealed == static
	public ref class Injector abstract sealed
	{
	public:
		static void InstallHook(IntPtr targetHWnd, Type^ hookInitializer, String ^addlSearchPath);
	};
}
