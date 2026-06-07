using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoShowMessageBoxHandler : IResponseMessageHandler<DoShowMessageBox>
    {
        private readonly IMessageBoxProvider _provider;

        public DoShowMessageBoxHandler(IMessageBoxProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoShowMessageBox message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _provider.Show(message.Text, message.Caption, message.Button, message.Icon);
                return Task.FromResult<IMessage>(new SetStatus { Message = "Successfully displayed MessageBox" });
            }
            catch (Exception exception)
            {
                return Task.FromResult<IMessage>(new SetStatus { Message = $"MessageBox failed: {exception.Message}" });
            }
        }
    }
}
