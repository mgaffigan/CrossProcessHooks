// This is the main DLL file.

#include "stdafx.h"

#include "Injector.h"

using namespace Itp::Win32::MdiHook;

static unsigned int WM_GOBABYGO = ::RegisterWindowMessage(L"ItpWin32WindowsClrHook_GOBABYGO");
static HHOOK _messageHookHandle;

//-----------------------------------------------------------------------------
//Spying Process functions follow
//-----------------------------------------------------------------------------
void Injector::InstallHook(IntPtr targetHWnd, Type^ hookInitializer, String ^addlSearchPath)
{
	HWND hWndTarget = (HWND)targetHWnd.ToPointer();
	DWORD processID = 0;
	DWORD threadID = ::GetWindowThreadProcessId(hWndTarget, &processID);
	if (processID == 0)
	{
		throw gcnew InvalidOperationException("Cannot open window");
	}

	HINSTANCE hinstDLL = nullptr;
	if (!::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hinstDLL))
	{
		throw gcnew InvalidOperationException("Cannot get dll instance");
	}

	try
	{
		HANDLE hProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
		if (!hProcess)
		{
			throw gcnew InvalidOperationException("Cannot open process");
		}
		try
		{
			// allocate type information
			HookParams typeInfo;
			typeInfo.TypeName = hookInitializer->FullName;
			typeInfo.Codebase = hookInitializer->Assembly->Location;
			typeInfo.AdditionalSearchPath = addlSearchPath;
			auto cbParams = Marshal::SizeOf(typeInfo);
			IntPtr pLocalStruct = Marshal::AllocHGlobal(cbParams);
			try
			{
				Marshal::StructureToPtr(typeInfo, pLocalStruct, false);

				// copy to the remote process
				void* acmRemote = ::VirtualAllocEx(hProcess, NULL, cbParams, MEM_COMMIT, PAGE_READWRITE);
				try
				{
					::WriteProcessMemory(hProcess, acmRemote, pLocalStruct.ToPointer(), cbParams, NULL);

					// install the hook
					_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
					if (!_messageHookHandle)
					{
						throw gcnew InvalidOperationException("Cannot hook window");
					}

					::SendMessage(hWndTarget, WM_GOBABYGO, (WPARAM)acmRemote, NULL);
					::UnhookWindowsHookEx(_messageHookHandle);
				}
				finally
				{
					::VirtualFreeEx(hProcess, acmRemote, 0, MEM_RELEASE);
				}
			}
			finally
			{
				Marshal::FreeHGlobal(pLocalStruct);
			}
		}
		finally
		{
			::CloseHandle(hProcess);
		}
	}
	finally
	{
		::FreeLibrary(hinstDLL);
	}
}

__declspec(dllexport)
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam)
{
	if (nCode == HC_ACTION || lparam == NULL)
	{
		CWPSTRUCT* msg = (CWPSTRUCT*)lparam;
		if (msg->message == WM_GOBABYGO)
		{
			auto params = Marshal::PtrToStructure<HookParams>((IntPtr)(void*)msg->wParam);

			if (params.AdditionalSearchPath != nullptr)
			{
				AssemblySearchPath::AddLoadLocation(params.AdditionalSearchPath);
			}

			// load assm
			Assembly^ assm = nullptr;
			try
			{
				assm = Assembly::LoadFrom(params.Codebase);
			}
			catch (Exception^ ex)
			{
				Debug::WriteLine("Failed to load assembly '{0}':\r\n\r\n{1}", params.Codebase, ex);
				goto nextHook;
			}

			// load type
			Type^ tHook = nullptr;
			try
			{
				tHook = assm->GetType(params.TypeName, true);
			}
			catch (Exception^ ex)
			{
				Debug::WriteLine("Failed to load type '{0}' from assembly '{2}':\r\n\r\n{1}", params.TypeName, ex, assm->FullName);
				goto nextHook;
			}

			// initialize
			try
			{
				Activator::CreateInstance(tHook);
			}
			catch (Exception^ ex)
			{
				Debug::WriteLine("Failed to instantiate '{0}' from assembly '{2}':\r\n\r\n{1}", params.TypeName, ex, assm->FullName);
				goto nextHook;
			}
		}
	}

nextHook:
	return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
}
