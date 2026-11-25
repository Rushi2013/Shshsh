# UniStorm download

The original MediaFire ZIP is stored in two parts to stay below GitHub's 100 MiB per-file limit. These pieces are **not automatically imported by Unity**.

Reassemble the ZIP outside the Unity project:

- macOS/Linux: `cat UniStorm.zip.part00 UniStorm.zip.part01 > UniStorm.zip`
- Windows Command Prompt: `copy /b UniStorm.zip.part00+UniStorm.zip.part01 UniStorm.zip`

Run the command in this directory. The reconstructed ZIP is 145,328,613 bytes and has SHA-256 `a6e851eaefff4b2aa2f4b4e53eae807e8faf60eef212c35f9ca90d00f383aec8`.

Extract the ZIP. Its single extensionless file is a Unity package; rename it with the `.unitypackage` extension, then use Unity's Assets > Import Package > Custom Package to import it. Review the package contents and ensure you have the appropriate licence before importing or distributing it.
