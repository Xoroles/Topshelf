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

namespace Topshelf
{
    using System.Windows.Forms;
    using Builders;
    using HostConfigurators;
    using Runtime;
    using WinForms;

    public static class NLogConfiguratorExtensions
    {
        /// <summary>
        ///     Specify as Generic the Type of the Form
        /// </summary>
        /// <param name="configurator"> Optional service bus configurator </param>
        public static void UseWinFormsConsoleHost<TForm>(this HostConfigurator configurator) where TForm : Form, new()
        {
            configurator.UseHostBuilder(HostBuilderFactory<TForm>);
        }


        /// <summary>
        ///     Specify as set the Form
        /// </summary>
        /// <param name="configurator"> Optional service bus configurator </param>
        /// <param name="form">A Instance of the Form to Show</param>
        public static void UseWinFormsConsoleHost<TForm>(this HostConfigurator configurator, TForm form) where TForm : Form, new()
        {
            configurator.UseHostBuilder((x, y) => HostBuilderFactory(x, y, form));
        }

        private static HostBuilder HostBuilderFactory<TForm>(HostEnvironment environment, HostSettings settings, TForm form) where TForm : Form, new()
        {
            return new WinFormsRunBuilder<TForm>(environment, settings, form);
        }

        private static HostBuilder HostBuilderFactory<TForm>(HostEnvironment environment, HostSettings settings) where TForm : Form, new()
        {
            return new WinFormsRunBuilder<TForm>(environment, settings, null);
        }
    }
}