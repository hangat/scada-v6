﻿/*
 * Copyright 2022 Rapid Software LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : Server Service
 * Summary  : Implements the Server service
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2020
 */

using Scada.Server.Engine;
using System.ServiceProcess;

namespace Scada.Server.Svc
{
    /// <summary>
    /// Implements the Server service.
    /// <para>Реализует службу Сервера.</para>
    /// </summary>
    public partial class SvcMain : ServiceBase
    {
        private readonly Manager manager;

        public SvcMain()
        {
            InitializeComponent();
            manager = new Manager();
        }

        protected override void OnStart(string[] args)
        {
            manager.StartService();
        }

        protected override void OnStop()
        {
            manager.StopService();
        }

        protected override void OnShutdown()
        {
            manager.StopService();
        }
    }
}
