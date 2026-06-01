using MasterSplinter.Client.Core.Dispatch;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class GetDrivesHandler : IResponseMessageHandler<GetDrives>
    {
        private readonly IDriveProvider _provider;

        public GetDrivesHandler(IDriveProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetDrives message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DriveListResult result = _provider.GetDrives();
            if (!result.IsSuccess)
            {
                return Task.FromResult<IMessage>(new SetStatusFileManager
                {
                    Message = result.ErrorMessage,
                    SetLastDirectorySeen = false
                });
            }

            return Task.FromResult<IMessage>(new GetDrivesResponse
            {
                Drives = result.Drives
            });
        }
    }
}
