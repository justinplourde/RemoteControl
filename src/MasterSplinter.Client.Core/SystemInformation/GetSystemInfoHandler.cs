using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.SystemInformation
{
    public sealed class GetSystemInfoHandler : IResponseMessageHandler<GetSystemInfo>
    {
        private readonly ISystemInfoProvider _provider;

        public GetSystemInfoHandler(ISystemInfoProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetSystemInfo message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IMessage>(new GetSystemInfoResponse
            {
                SystemInfos = new List<Tuple<string, string>>(_provider.GetSystemInfo())
            });
        }
    }
}
