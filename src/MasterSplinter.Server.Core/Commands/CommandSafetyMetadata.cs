using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandSafetyMetadata
    {
        public CommandSafetyMetadata(
            CommandSafetyClass safetyClass,
            bool isReadOnly,
            bool requiresPermission,
            bool requiresConsent)
        {
            SafetyClass = safetyClass;
            IsReadOnly = isReadOnly;
            RequiresPermission = requiresPermission;
            RequiresConsent = requiresConsent;
        }

        public CommandSafetyClass SafetyClass { get; }

        public bool IsReadOnly { get; }

        public bool RequiresPermission { get; }

        public bool RequiresConsent { get; }

        public static CommandSafetyMetadata ReadOnly(CommandSafetyClass safetyClass)
        {
            if (safetyClass == CommandSafetyClass.Unknown)
                throw new ArgumentException("Read-only commands require a known safety class.", nameof(safetyClass));

            return new CommandSafetyMetadata(safetyClass, true, false, false);
        }

        public static CommandSafetyMetadata Controlled(
            CommandSafetyClass safetyClass,
            bool requiresConsent)
        {
            if (safetyClass == CommandSafetyClass.Unknown)
                throw new ArgumentException("Controlled commands require a known safety class.", nameof(safetyClass));

            return new CommandSafetyMetadata(safetyClass, false, true, requiresConsent);
        }

        public static CommandSafetyMetadata Unknown()
        {
            return new CommandSafetyMetadata(CommandSafetyClass.Unknown, false, true, true);
        }
    }
}
