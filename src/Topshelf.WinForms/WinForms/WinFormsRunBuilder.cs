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
    using System;
    using System.Windows.Forms;
    using Builders;
    using Logging;
    using Runtime;

    internal class WinFormsRunBuilder<TForm> : HostBuilder where TForm : Form, new()
    {
        private readonly TForm _form;

        #region Statics

        private static readonly LogWriter _log = HostLogger.Get<WinFormsRunBuilder<TForm>>();

        #endregion

        #region Ctor

        public WinFormsRunBuilder(HostEnvironment environment, HostSettings settings, TForm form)
        {
            _form = form;
            Environment = environment;
            Settings = settings;
        }

        #endregion

        #region Properties

        public HostEnvironment Environment { get; }

        public HostSettings Settings { get; }

        #endregion

        #region Interface Impl

        public virtual Host Build(ServiceBuilder serviceBuilder)
        {
            ServiceHandle serviceHandle = serviceBuilder.Build(Settings);

            return CreateHost(serviceHandle);
        }

        public void Match<T>(Action<T> callback)
            where T : class, HostBuilder
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            T self = this as T;
            if (self != null)
                callback(self);
        }

        #endregion

        private Host CreateHost(ServiceHandle serviceHandle)
        {
            if (Environment.IsRunningAsAService)
            {
                _log.Debug("Running as a service, creating service host.");
                return Environment.CreateServiceHost(Settings, serviceHandle);
            }

            _log.Debug("Running as a console application, creating the console host.");
            return new WinFormsRunHost<TForm>(Settings, Environment, serviceHandle, _form);
        }
    }
}