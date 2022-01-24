Small Package for Unity that wraps WebSockets (System.Net.WebSockets and jslib-bridged WebSockets used in WebGL) in an Interface that returns [UniTasks](https://github.com/Cysharp/UniTask)


This code is based on https://github.com/jirihybek/unity-websocket-webgl and https://github.com/endel/NativeWebSocket

The difference is that it uses an Interface that returns UniTask.
Therefore this Interface allows you to use the async/await-Pattern, very similar to System.Net.WebSockets (replacing Task with UniTask).

Example on how to use this Library: https://github.com/Iblis/UniTaskWebSocketExample

This code was developed and tested with Unity 2021.2. It might work in previous versions, but there was an issue with jslib files beeing imported from packages in past Unity Versions. 
If you get errors about the jslib not beeing available during Runtime, you need to copy the content of this repos Runtime/Plugins folder into your projects Assets/Plugins folder.

Also, Unity 2021.2 updated their version of emscripten, which caused breaking changes to the jslib code. So if you want to use this library with an earlier Version of Unity, you might need to adapt the code (eg. use Pointer_stringify instead of UTF8ToString, see https://github.com/endel/NativeWebSocket/pull/54/files for the actual changes needed).
