#include "stdafx.h"
#include "AssemblySearchPath.h"

using namespace Itp::Win32::MdiHook;

Assembly^ AssemblySearchPath::ResolveAssembly(Object^ sender, ResolveEventArgs^ args)
{
	auto addlPaths = AssemblySearchPath::GetLoadLocations();
	for each(auto exten in addlPaths)
	{
		String^ possibleFileName = Path::Combine(exten, (gcnew AssemblyName(args->Name))->Name + ".dll");
		if (File::Exists(possibleFileName))
		{
			return Assembly::LoadFrom(possibleFileName);
		}
	}

	return nullptr;
}