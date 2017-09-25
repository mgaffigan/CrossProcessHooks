Cross Process Hooks
===========

Framework to allow cross process hooks in managed code.  Uses Rx.Net as the primary API surface.  All filtering occurs in target process to reduce cross procses calls.

## Projects

* Itp.Win32.MdiHook.Injector<br>C++/CLI code to act as the entrypoint to our code in the target process
* Itp.Win32.MdiHook<br>Primary library
* Itp.Win32.MdiHook.DemoClient<br>Demo App which acts as a client to the API
* Itp.Win32.DemoWpfTarget<br>Demo target for the WPF injector
* Itp.Win32.DemoMdiApplication<br>Demo target for the Win32 and MDI injectors
