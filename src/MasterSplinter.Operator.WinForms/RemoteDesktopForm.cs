using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Video;
using MasterSplinter.Server.Core.Authorization;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.RemoteDesktop;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasterSplinter.Operator.WinForms
{
#pragma warning disable CA1416
    public sealed class RemoteDesktopForm : Form
    {
        private const string OperatorId = "winforms-operator";

        private readonly Button _refreshClientsButton = new Button();
        private readonly Button _startListenerButton = new Button();
        private readonly Button _startStreamButton = new Button();
        private readonly Button _stopListenerButton = new Button();
        private readonly Button _stopStreamButton = new Button();
        private readonly CheckBox _grantConsentCheckBox = new CheckBox();
        private readonly CheckBox _grantPermissionCheckBox = new CheckBox();
        private readonly ComboBox _clientsComboBox = new ComboBox();
        private readonly Label _fpsLabel = new Label();
        private readonly Label _qualityLabel = new Label();
        private readonly Label _statusLabel = new Label();
        private readonly NumericUpDown _displayIndexInput = new NumericUpDown();
        private readonly NumericUpDown _portInput = new NumericUpDown();
        private readonly PictureBox _desktopPictureBox = new PictureBox();
        private readonly System.Windows.Forms.Timer _clientsTimer = new System.Windows.Forms.Timer();
        private readonly TrackBar _qualityTrackBar = new TrackBar();

        private AwaitableMessageSink _responseSink;
        private ClientSessionRegistry _registry;
        private ClientStatusRegistry _statusRegistry;
        private RemoteClientListenerOrchestrator _orchestrator;
        private ServerCommandDispatcher _dispatcher;
        private CancellationTokenSource _listenerCancellation;
        private CancellationTokenSource _streamCancellation;
        private int _frameCount;
        private Stopwatch _fpsStopwatch;
        private Resolution _lastRemoteResolution;

        public RemoteDesktopForm()
        {
            Text = "MasterSplinter Remote Desktop";
            Width = 1180;
            Height = 780;
            MinimumSize = new Size(900, 600);
            KeyPreview = true;

            BuildLayout();
            WireEvents();
            SetStreamingState(false);
            SetListeningState(false);
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            _streamCancellation?.Cancel();
            await StopListenerAsync();
            base.OnFormClosing(e);
        }

        private void BuildLayout()
        {
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 86,
                Padding = new Padding(8),
                WrapContents = true
            };

            _portInput.Minimum = 1;
            _portInput.Maximum = 65535;
            _portInput.Value = 4782;
            _portInput.Width = 72;

            _startListenerButton.Text = "Start Listener";
            _startListenerButton.AutoSize = true;
            _stopListenerButton.Text = "Stop Listener";
            _stopListenerButton.AutoSize = true;
            _refreshClientsButton.Text = "Refresh Clients";
            _refreshClientsButton.AutoSize = true;

            _grantPermissionCheckBox.Text = "Grant permission";
            _grantPermissionCheckBox.Checked = true;
            _grantPermissionCheckBox.AutoSize = true;
            _grantConsentCheckBox.Text = "Grant consent";
            _grantConsentCheckBox.Checked = true;
            _grantConsentCheckBox.AutoSize = true;

            _clientsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _clientsComboBox.Width = 330;

            _qualityTrackBar.Minimum = 1;
            _qualityTrackBar.Maximum = 100;
            _qualityTrackBar.Value = 75;
            _qualityTrackBar.TickFrequency = 10;
            _qualityTrackBar.Width = 120;
            _qualityLabel.AutoSize = true;
            _qualityLabel.Text = "Quality 75";

            _displayIndexInput.Minimum = 0;
            _displayIndexInput.Maximum = 16;
            _displayIndexInput.Width = 48;

            _startStreamButton.Text = "Start Stream";
            _startStreamButton.AutoSize = true;
            _stopStreamButton.Text = "Stop Stream";
            _stopStreamButton.AutoSize = true;

            _fpsLabel.AutoSize = true;
            _fpsLabel.Text = "FPS 0.00";

            topPanel.Controls.Add(new Label { Text = "Port", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            topPanel.Controls.Add(_portInput);
            topPanel.Controls.Add(_startListenerButton);
            topPanel.Controls.Add(_stopListenerButton);
            topPanel.Controls.Add(_refreshClientsButton);
            topPanel.Controls.Add(_grantPermissionCheckBox);
            topPanel.Controls.Add(_grantConsentCheckBox);
            topPanel.Controls.Add(new Label { Text = "Client", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
            topPanel.Controls.Add(_clientsComboBox);
            topPanel.Controls.Add(_qualityLabel);
            topPanel.Controls.Add(_qualityTrackBar);
            topPanel.Controls.Add(new Label { Text = "Display", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            topPanel.Controls.Add(_displayIndexInput);
            topPanel.Controls.Add(_startStreamButton);
            topPanel.Controls.Add(_stopStreamButton);
            topPanel.Controls.Add(_fpsLabel);

            _desktopPictureBox.BackColor = Color.Black;
            _desktopPictureBox.Dock = DockStyle.Fill;
            _desktopPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _desktopPictureBox.TabStop = true;

            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 24;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(_desktopPictureBox);
            Controls.Add(_statusLabel);
            Controls.Add(topPanel);
        }

        private void WireEvents()
        {
            _startListenerButton.Click += async (sender, args) => await StartListenerAsync().ConfigureAwait(false);
            _stopListenerButton.Click += async (sender, args) => await StopListenerAsync().ConfigureAwait(false);
            _refreshClientsButton.Click += (sender, args) => RefreshClients();
            _startStreamButton.Click += async (sender, args) => await StartStreamAsync().ConfigureAwait(false);
            _stopStreamButton.Click += (sender, args) => StopStream();
            _qualityTrackBar.Scroll += (sender, args) => _qualityLabel.Text = $"Quality {_qualityTrackBar.Value}";
            _desktopPictureBox.MouseDown += async (sender, args) => await SendMouseEventAsync(ToMouseDownAction(args.Button), true, args).ConfigureAwait(false);
            _desktopPictureBox.MouseUp += async (sender, args) => await SendMouseEventAsync(ToMouseUpAction(args.Button), false, args).ConfigureAwait(false);
            _desktopPictureBox.MouseMove += async (sender, args) => await SendMouseEventAsync(MouseAction.MoveCursor, false, args).ConfigureAwait(false);
            _desktopPictureBox.MouseWheel += async (sender, args) => await SendMouseEventAsync(args.Delta < 0 ? MouseAction.ScrollDown : MouseAction.ScrollUp, false, args).ConfigureAwait(false);
            _desktopPictureBox.MouseEnter += (sender, args) => _desktopPictureBox.Focus();
            KeyDown += async (sender, args) => await SendKeyboardEventAsync(args, true).ConfigureAwait(false);
            KeyUp += async (sender, args) => await SendKeyboardEventAsync(args, false).ConfigureAwait(false);
            _clientsTimer.Interval = 1000;
            _clientsTimer.Tick += (sender, args) => RefreshClients();
        }

        private async Task StartListenerAsync()
        {
            if (_orchestrator != null)
                return;

            _registry = new ClientSessionRegistry();
            _statusRegistry = new ClientStatusRegistry();
            _responseSink = new AwaitableMessageSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(
                _registry,
                new UiLifecycleSink(this, SetStatus));
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var messageSink = new ClientStatusMessageSink(_statusRegistry, _responseSink);
            _orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake, messageSink);
            _dispatcher = new ServerCommandDispatcher(_registry);
            _listenerCancellation = new CancellationTokenSource();

            await _orchestrator.StartAsync(
                new ServerListenOptions("127.0.0.1", (int)_portInput.Value),
                _listenerCancellation.Token).ConfigureAwait(false);

            BeginInvoke((Action)(() =>
            {
                SetListeningState(true);
                SetStatus($"Listening on 127.0.0.1:{_portInput.Value}.");
                _clientsTimer.Start();
            }));
        }

        private async Task StopListenerAsync()
        {
            StopStream();
            _clientsTimer.Stop();
            _listenerCancellation?.Cancel();

            if (_orchestrator != null)
                await _orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);

            _orchestrator = null;
            _dispatcher = null;
            _responseSink = null;
            _registry = null;
            _statusRegistry = null;
            _listenerCancellation?.Dispose();
            _listenerCancellation = null;

            if (!IsDisposed)
            {
                BeginInvoke((Action)(() =>
                {
                    _clientsComboBox.Items.Clear();
                    SetListeningState(false);
                    SetStatus("Listener stopped.");
                }));
            }
        }

        private async Task StartStreamAsync()
        {
            if (_streamCancellation != null)
                return;
            if (_registry == null || _dispatcher == null || _responseSink == null)
            {
                SetStatus("Start the listener first.");
                return;
            }

            string clientId = GetSelectedClientId();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                SetStatus("Select a connected client.");
                return;
            }

            _streamCancellation = new CancellationTokenSource();
            _fpsStopwatch = Stopwatch.StartNew();
            _frameCount = 0;
            _lastRemoteResolution = null;
            SetStreamingState(true);
            SetStatus("Remote desktop stream starting.");

            try
            {
                var options = new RemoteDesktopStreamOptions(
                    clientId,
                    _qualityTrackBar.Value,
                    (int)_displayIndexInput.Value,
                    int.MaxValue,
                    0,
                    TimeSpan.FromSeconds(10));
                var session = new RemoteDesktopStreamSession(
                    _dispatcher,
                    _responseSink,
                    (message, token) => CreateAuthorizedRequestAsync(clientId, message, token));

                RemoteDesktopStreamResult result = await session.RunAsync(
                    options,
                    RenderFrameAsync,
                    _streamCancellation.Token).ConfigureAwait(false);

                SetStatus(result.StoppedOnEmptyFrame
                    ? "Remote desktop stream stopped after empty frame."
                    : "Remote desktop stream stopped.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Remote desktop stream stopped.");
            }
            catch (Exception exception)
            {
                SetStatus($"Remote desktop stream failed: {exception.Message}");
            }
            finally
            {
                _streamCancellation?.Dispose();
                _streamCancellation = null;
                if (!IsDisposed)
                    BeginInvoke((Action)(() => SetStreamingState(false)));
            }
        }

        private void StopStream()
        {
            _streamCancellation?.Cancel();
        }

        private Task RenderFrameAsync(RemoteDesktopStreamFrame frame, CancellationToken cancellationToken)
        {
            if (frame.Response.Image == null || frame.Response.Image.Length == 0)
                return Task.CompletedTask;

            byte[] imageBytes = ExtractLegacyFirstFrameJpeg(frame.Response.Image);
            BeginInvoke((Action)(() =>
            {
                using (var stream = new MemoryStream(imageBytes))
                using (var image = Image.FromStream(stream))
                {
                    Image previous = _desktopPictureBox.Image;
                    _desktopPictureBox.Image = new Bitmap(image);
                    previous?.Dispose();
                }

                _lastRemoteResolution = frame.Response.Resolution;
                _frameCount++;
                double fps = _frameCount / Math.Max(0.001, _fpsStopwatch.Elapsed.TotalSeconds);
                _fpsLabel.Text = $"FPS {fps:0.00}";
                SetStatus($"Frame {frame.FrameNumber}: {frame.Response.Resolution}; quality {frame.Response.Quality}; monitor {frame.Response.Monitor}.");
            }));

            return Task.CompletedTask;
        }

        private async Task<CommandDispatchRequest> CreateAuthorizedRequestAsync(
            string clientId,
            IMessage command,
            CancellationToken cancellationToken)
        {
            var request = new CommandDispatchRequest(
                Guid.NewGuid(),
                clientId,
                command,
                OperatorId,
                "winforms");
            CommandSafetyMetadata safetyMetadata = DefaultCommandSafetyClassifier.Instance.Classify(command);
            var authorizationService = new CommandAuthorizationService(
                new StaticOperatorPermissionService(_grantPermissionCheckBox.Checked),
                new StaticClientConsentService(_grantConsentCheckBox.Checked));
            CommandDispatchAuthorization authorization = await authorizationService.AuthorizeAsync(
                new OperatorIdentity(OperatorId, "WinForms Operator"),
                request,
                safetyMetadata,
                cancellationToken).ConfigureAwait(false);

            return request.WithAuthorization(authorization);
        }

        private async Task SendMouseEventAsync(MouseAction action, bool isMouseDown, MouseEventArgs args)
        {
            if (action == MouseAction.None || _dispatcher == null || _streamCancellation == null)
                return;

            _desktopPictureBox.Focus();
            if (!TryMapMousePoint(args.X, args.Y, out RemoteDesktopPoint point))
                return;

            var message = new DoMouseEvent
            {
                Action = action,
                IsMouseDown = isMouseDown,
                X = point.X,
                Y = point.Y,
                MonitorIndex = (int)_displayIndexInput.Value
            };

            await DispatchInputAsync(message).ConfigureAwait(false);
        }

        private async Task SendKeyboardEventAsync(KeyEventArgs args, bool keyDown)
        {
            if (_dispatcher == null || _streamCancellation == null)
                return;
            if (args.KeyValue < 0 || args.KeyValue > byte.MaxValue)
                return;

            var message = new DoKeyboardEvent
            {
                Key = (byte)args.KeyValue,
                KeyDown = keyDown
            };

            args.Handled = true;
            await DispatchInputAsync(message).ConfigureAwait(false);
        }

        private async Task DispatchInputAsync(IMessage message)
        {
            string clientId = GetSelectedClientId();
            if (string.IsNullOrWhiteSpace(clientId))
                return;

            CommandDispatchRequest request = await CreateAuthorizedRequestAsync(
                clientId,
                message,
                CancellationToken.None).ConfigureAwait(false);
            CommandDispatchResult result = await _dispatcher.DispatchAsync(request, CancellationToken.None)
                .ConfigureAwait(false);

            if (result.Status != CommandDispatchStatus.Sent)
                SetStatus($"Input dispatch stopped: {result.Status}.");
        }

        private bool TryMapMousePoint(int x, int y, out RemoteDesktopPoint point)
        {
            point = default;
            Image image = _desktopPictureBox.Image;
            if (image == null || _lastRemoteResolution == null)
                return false;

            return RemoteDesktopCoordinateMapper.TryMapZoomedPoint(
                _desktopPictureBox.ClientSize.Width,
                _desktopPictureBox.ClientSize.Height,
                image.Width,
                image.Height,
                _lastRemoteResolution.Width,
                _lastRemoteResolution.Height,
                x,
                y,
                out point);
        }

        private static MouseAction ToMouseDownAction(MouseButtons button)
        {
            if (button == MouseButtons.Left)
                return MouseAction.LeftDown;
            if (button == MouseButtons.Right)
                return MouseAction.RightDown;

            return MouseAction.None;
        }

        private static MouseAction ToMouseUpAction(MouseButtons button)
        {
            if (button == MouseButtons.Left)
                return MouseAction.LeftUp;
            if (button == MouseButtons.Right)
                return MouseAction.RightUp;

            return MouseAction.None;
        }

        private void RefreshClients()
        {
            if (_registry == null)
                return;

            string selectedClientId = GetSelectedClientId();
            _clientsComboBox.BeginUpdate();
            try
            {
                _clientsComboBox.Items.Clear();
                foreach (ClientSessionSnapshot snapshot in _registry.GetSnapshots())
                {
                    _clientsComboBox.Items.Add(new ClientListItem(snapshot));
                }

                if (_clientsComboBox.Items.Count > 0)
                {
                    int selectedIndex = 0;
                    for (int index = 0; index < _clientsComboBox.Items.Count; index++)
                    {
                        if (_clientsComboBox.Items[index] is ClientListItem item &&
                            string.Equals(item.ClientId, selectedClientId, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedIndex = index;
                            break;
                        }
                    }

                    _clientsComboBox.SelectedIndex = selectedIndex;
                }
            }
            finally
            {
                _clientsComboBox.EndUpdate();
            }
        }

        private string GetSelectedClientId()
        {
            return _clientsComboBox.SelectedItem is ClientListItem item ? item.ClientId : null;
        }

        private void SetListeningState(bool listening)
        {
            _startListenerButton.Enabled = !listening;
            _stopListenerButton.Enabled = listening;
            _refreshClientsButton.Enabled = listening;
            _portInput.Enabled = !listening;
            _startStreamButton.Enabled = listening;
        }

        private void SetStreamingState(bool streaming)
        {
            _startStreamButton.Enabled = !streaming && _orchestrator != null;
            _stopStreamButton.Enabled = streaming;
            _qualityTrackBar.Enabled = !streaming;
            _displayIndexInput.Enabled = !streaming;
            _clientsComboBox.Enabled = !streaming;
        }

        private void SetStatus(string message)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => SetStatus(message)));
                return;
            }

            _statusLabel.Text = message;
        }

        private static byte[] ExtractLegacyFirstFrameJpeg(byte[] image)
        {
            if (image.Length < 4)
                return image;

            int length = BitConverter.ToInt32(image, 0);
            if (length <= 0 || length > image.Length - 4)
                return image;

            var jpeg = new byte[length];
            Buffer.BlockCopy(image, 4, jpeg, 0, length);
            return jpeg;
        }

        private sealed class ClientListItem
        {
            public ClientListItem(ClientSessionSnapshot snapshot)
            {
                ClientId = snapshot.ClientId;
                string user = snapshot.Identification == null ? "-" : snapshot.Identification.Username;
                string machine = snapshot.Identification == null ? "-" : snapshot.Identification.PcName;
                Text = $"{ClientId} | {user}@{machine}";
            }

            public string ClientId { get; }

            private string Text { get; }

            public override string ToString()
            {
                return Text;
            }
        }

        private sealed class StaticOperatorPermissionService : IOperatorPermissionService
        {
            private readonly bool _allowed;

            public StaticOperatorPermissionService(bool allowed)
            {
                _allowed = allowed;
            }

            public Task<bool> HasPermissionAsync(
                OperatorIdentity operatorIdentity,
                OperatorPermission permission,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_allowed);
            }
        }

        private sealed class StaticClientConsentService : IClientConsentService
        {
            private readonly bool _allowed;

            public StaticClientConsentService(bool allowed)
            {
                _allowed = allowed;
            }

            public Task<bool> HasConsentAsync(
                string clientId,
                OperatorIdentity operatorIdentity,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_allowed);
            }
        }

        private sealed class UiLifecycleSink : IClientConnectionLifecycleSink
        {
            private readonly Control _owner;
            private readonly Action<string> _write;

            public UiLifecycleSink(Control owner, Action<string> write)
            {
                _owner = owner;
                _write = write;
            }

            public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
            {
                if (!_owner.IsDisposed)
                {
                    _owner.BeginInvoke((Action)(() =>
                    {
                        _write($"{lifecycleEvent.Kind}: client {lifecycleEvent.ClientId ?? "-"}");
                    }));
                }

                return Task.CompletedTask;
            }
        }
    }
#pragma warning restore CA1416
}
