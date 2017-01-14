#pragma once

using namespace System;
using namespace System::Reflection;
using namespace System::IO;
using namespace System::Threading;
using namespace System::Collections::Generic;

namespace Itp::Win32::MdiHook
{
	public ref class AssemblySearchPath
	{
	public:

		static Assembly^ ResolveAssembly(Object^ sender, ResolveEventArgs^ args);

		static void AddLoadLocation(String^ location)
		{
			Monitor::Enter(SyncAdditionalLoadLocations);
			try
			{
				if (!AdditionalLoadLocations->Contains(location))
				{
					AdditionalLoadLocations->Add(location);
				}
			}
			finally
			{
				Monitor::Exit(SyncAdditionalLoadLocations);
			}
		}

		static array<String^>^ GetLoadLocations()
		{
			Monitor::Enter(SyncAdditionalLoadLocations);
			try
			{
				return AdditionalLoadLocations->ToArray();
			}
			finally
			{
				Monitor::Exit(SyncAdditionalLoadLocations);
			}
		}

	private:

		static Object^ SyncAdditionalLoadLocations;
		static List<String^>^ AdditionalLoadLocations;

		static AssemblySearchPath()
		{
			SyncAdditionalLoadLocations = gcnew Object();
			AdditionalLoadLocations = gcnew List<String^>();

			AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(AssemblySearchPath::ResolveAssembly);
		}
	};
}

