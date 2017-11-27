Unity build obfuscator
=====
License: *GNU GPL v3*

Features:
* Protecting/encrypting int, string constants
* Renaming everything it can rename without affecting functionality
* Adding checksum library verification

### How to use

1. You need to locate two libraries *Assembly-CSharp.dll* and *UnityEngine.CoreModule.dll* within your build.
2. Run 

```
mono ./AsertNet.exe --filename="Assembly-CSharp.dll" \
    --unitylib="UnityEngine.CoreModule.dll" \
    --hideintegers --encryptstrings \
    --antitamper --renameall
```

3. Thats all

### For Android builds

First you need to unpack apk file with https://ibotpeaches.github.io/Apktool/
Then obfuscate libraries and pack it back

P.S. Don't forget to sign your APK again with a keystore

```
./apktool d apk.apk
mono ./AsertNet.exe --filename="./apk/assets/bin/Data/Managed/Assembly-CSharp.dll" \
    --unitylib="./apk/assets/bin/Data/Managed/UnityEngine.CoreModule.dll" \
    --hideintegers --encryptstrings \
    --antitamper --renameall
./apktool-mac b apk -o result.apk
jarsigner -keystore ./key.keystore -verbose result.apk "key name"
```
