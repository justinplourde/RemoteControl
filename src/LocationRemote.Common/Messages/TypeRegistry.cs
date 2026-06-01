using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quasar.Common.Messages
{
    public static class TypeRegistry
    {
        private static readonly object SyncRoot = new object();
        private static bool _packetTypesRegistered;
        private static int _typeIndex;

        public static void AddTypeToSerializer(Type parent, Type type)
        {
            if (type == null || parent == null)
                throw new ArgumentNullException();

            bool isAlreadyAdded = RuntimeTypeModel.Default[parent].GetSubtypes().Any(subType => subType.DerivedType.Type == type);

            if (!isAlreadyAdded)
                RuntimeTypeModel.Default[parent].AddSubType(++_typeIndex, type);
        }

        public static void AddTypesToSerializer(Type parent, params Type[] types)
        {
            foreach (Type type in types)
                AddTypeToSerializer(parent, type);
        }

        public static IEnumerable<Type> GetPacketTypes(Type type)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
        }

        public static void EnsurePacketTypesRegistered()
        {
            lock (SyncRoot)
            {
                if (_packetTypesRegistered)
                    return;

                AddTypesToSerializer(typeof(IMessage), GetPacketTypes(typeof(IMessage)).ToArray());
                _packetTypesRegistered = true;
            }
        }
    }
}
