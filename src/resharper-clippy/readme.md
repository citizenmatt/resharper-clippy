# DoubleAgent and Side by Side

This project references `DoubleAgent.Control`, which is a PIA for the COM objects
defined in `DaControl.dll`, which in turn loads `DaCore.dll` to provide the implementation.

To enable us to ship `DaControl.dll` without installing it, we need to implement registration
free COM activation. To do this, we need to call some Win32 APIs, namely `CreateActCtx` to
create an Activation Context that points to a manifest file that contains the registration
details for the COM objects in `DaControl.dll`.