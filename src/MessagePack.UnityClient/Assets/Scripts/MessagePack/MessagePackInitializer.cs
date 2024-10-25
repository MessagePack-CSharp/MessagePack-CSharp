// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace MessagePack.Unity
{
    public static class MessagePackInitializer
    {
        private static bool serializerRegistered = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            if (!serializerRegistered)
            {
                MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(UnityResolver.InstanceWithStandardResolver);
                serializerRegistered = true;
            }
        }

#if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            Init();
        }

#endif
    }
}
