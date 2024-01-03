// Modified for Unity
#if UNITY_ANDROID && !UNITY_EDITOR
#define __ANDROID__
#define __MOBILE__
#elif UNITY_IOS && !UNITY_EDITOR
#define __IOS__
#define __ENABLE_COREML__
#define __MOBILE__
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
#define __ENABLE_COREML__
#endif

