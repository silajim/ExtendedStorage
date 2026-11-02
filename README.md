> Work in progress: this RimWorld 1.6 port has a reported unresolved bug. It is published for development and is not ready for an upstream pull request or release.

# Local RimWorld 1.6 port

This working copy adds RimWorld 1.6 support to the archived upstream mod.
Install the entire mod folder and use Harmony, Extended Storage, then Extended Storage Extended in that order.
Use one copy of each mod; package IDs and definition names are unchanged for save continuity.
The original 1.5 and earlier content is retained in its version folders.

The source project targets .NET Framework 4.8 and RimWorld 1.6.4871.
Build with `dotnet build Source/ExtendedStorage/ExtendedStorage.csproj -c Release`.
The resulting DLL is written to `1.6/Assemblies`; the Harmony DLL is supplied by the Harmony mod.

See the workspace `PORT-1.6.md` for changes, runtime checks, and known validation limits.

---

# [[Abandoned] Extended Storage (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2566120724)


![Image](https://img.litet.net/logos/Info.png)

Update of Skullywag, Fluffy, spdskatr, DoctorVanGogh, Terraggs mod
https://steamcommunity.com/sharedfiles/filedetails/?id=731732064

![Image](https://img.litet.net/logos/Abandoned.png)
	
![Image](https://img.litet.net/logos/OriginalDescription.png)

Extended Storage

Adds additional storage buildings that multi stack items.

This works by having an input cell which the storage cell grabs from until it reaches its own internal limit, for example the wooden pallet can hold 600 steel in its storage slot and 75 will sit on the input slot, giving it total storage of 675, other buildings have smaller and larger limits for certain resources, all controlled via thing categories ala stockpiles, pawns are aware of contents and will take from them as per normal.

No requirements just subscribe and go.

![Image](https://img.litet.net/logos/ReportingIssues.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Always post your log using the [Log Uploader](https://steamcommunity.com/sharedfiles/filedetails/?id=2873415404)
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.
-  Use [RimSort](https://github.com/RimSort/RimSort/releases/latest) to sort your mods

 

[![Image](https://img.shields.io/github/v/release/emipa606/ExtendedStorage?label=latest%20version&style=plastic&color=9f1111&labelColor=black)](https://steamcommunity.com/sharedfiles/filedetails/changelog/2566120724)
