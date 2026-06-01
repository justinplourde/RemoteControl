using System;

namespace MasterSplinter.Server.Core.Authorization
{
    public sealed class OperatorIdentity
    {
        public OperatorIdentity(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Operator id is required.", nameof(id));

            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }

        public string DisplayName { get; }
    }
}
