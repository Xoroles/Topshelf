#region Header

// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

#endregion

namespace Topshelf.WinForms
{
    #region Imports

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Logging;
    using Microsoft.Win32;
    using Runtime;

    #endregion

    internal class WinFormsRunHost<T> : Host, HostControl where T : Form, new()
    {
        #region Fields

        readonly HostEnvironment _environment;
        readonly LogWriter _log = HostLogger.Get<WinFormsRunHost<T>>();
        readonly ServiceHandle _serviceHandle;
        private readonly T _form;
        readonly HostSettings _settings;
        int _deadThread;

        TopshelfExitCode _exitCode;
        volatile bool _hasCancelled;

        #endregion

        #region Ctor

        public WinFormsRunHost(HostSettings settings, HostEnvironment environment, ServiceHandle serviceHandle, T form = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            _settings = settings;
            _environment = environment;
            _serviceHandle = serviceHandle;
            _form = form ?? new T();

            if (settings.CanSessionChanged)
            {
                SystemEvents.SessionSwitch += OnSessionChanged;
            }
        }

        private void OnSessionChanged(object sender, SessionSwitchEventArgs e)
        {
            var arguments = new WinFormsSessionChangedArguments(e.Reason);

            _serviceHandle.SessionChanged(this, arguments);
        }

        #endregion

        /// <summary>
        ///   Runs the configured host
        /// </summary>
        public TopshelfExitCode Run()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            AppDomain.CurrentDomain.UnhandledException += CatchUnhandledException;

            if (_environment.IsServiceInstalled(_settings.ServiceName))
            {
                if (!_environment.IsServiceStopped(_settings.ServiceName))
                {
                    _log.ErrorFormat("The {0} service is running and must be stopped before running via WinForms",
                        _settings.ServiceName);

                    return TopshelfExitCode.ServiceAlreadyRunning;
                }
            }

            bool started = false;
            try
            {
                _log.Debug("Starting up as a WinForms application");

                _exitCode = TopshelfExitCode.Ok;

                _form.Text = _settings.DisplayName;
                if (!_serviceHandle.Start(this))
                    throw new TopshelfException("The service failed to start (return false).");

                started = true;

                Application.Run(_form);
                _hasCancelled = true;
            }
            catch (Exception ex)
            {
                _log.Error("An exception occurred", ex);

                return TopshelfExitCode.AbnormalExit;
            }
            finally
            {
                if (started)
                    StopService();

                (_form as IDisposable)?.Dispose();

                HostLogger.Shutdown();
            }

            return _exitCode;
        }

        private void StopService()
        {
            try
            {
                if (_hasCancelled == false)
                {
                    _log.InfoFormat("Stopping the {0} service", _settings.ServiceName);

                    if (!_serviceHandle.Stop(this))
                        throw new TopshelfException("The service failed to stop (returned false).");
                }
            }
            catch (Exception ex)
            {
                _log.Error("The service did not shut down gracefully", ex);
            }
            finally
            {
                _serviceHandle.Dispose();

                _log.InfoFormat("The {0} service has stopped.", _settings.ServiceName);
            }
        }

        private void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Fatal("The service threw an unhandled exception", (Exception)e.ExceptionObject);

            HostLogger.Shutdown();

            if (e.IsTerminating)
            {
                _exitCode = TopshelfExitCode.UnhandledServiceException;
                _form.Close();

#if !NET35
                // it isn't likely that a TPL thread should land here, but if it does let's no block it
                if (Task.CurrentId.HasValue)
                {
                    return;
                }
#endif

                // this is evil, but perhaps a good thing to let us clean up properly.
                int deadThreadId = Interlocked.Increment(ref _deadThread);
                Thread.CurrentThread.IsBackground = true;
                Thread.CurrentThread.Name = "Unhandled Exception " + deadThreadId.ToString();
                while (true)
                    Thread.Sleep(TimeSpan.FromHours(1));
            }
        }

        /// <summary>
        /// Tells the Host that the service is still starting, which resets the
        /// timeout.
        /// </summary>
        public void RequestAdditionalTime(TimeSpan timeRemaining)
        {
            // good for you, maybe we'll use a timer for startup at some point but for debugging
            // it's a pain in the ass
        }

        /// <summary>
        /// Stops the Host
        /// </summary>
        public void Stop()
        {
            _log.Info("Service Stop requested, exiting.");
            _form.Close();
        }

        /// <summary>
        /// Restarts the Host
        /// </summary>
        public void Restart()
        {
            _log.Info("Service Restart requested, but we don't support that here, so we are exiting.");
            _form.Close();
        }

        private class WinFormsSessionChangedArguments : SessionChangedArguments
        {
            public WinFormsSessionChangedArguments(SessionSwitchReason reason)
            {
                ReasonCode = (SessionChangeReasonCode)Enum.ToObject(typeof(SessionChangeReasonCode), (int)reason);
                SessionId = Process.GetCurrentProcess().SessionId;
            }

            public SessionChangeReasonCode ReasonCode { get; }

            public int SessionId { get; }
        }
    }
}