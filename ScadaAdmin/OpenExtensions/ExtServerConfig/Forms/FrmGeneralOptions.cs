﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Admin.Project;
using Scada.Forms;
using Scada.Lang;
using Scada.Log;
using Scada.Server;
using Scada.Server.Config;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinControl;

namespace Scada.Admin.Extensions.ExtServerConfig.Forms
{
    /// <summary>
    /// Represents a form for editing general options.
    /// <para>Форма для редактирования основных параметров.</para>
    /// </summary>
    public partial class FrmGeneralOptions : Form, IChildForm
    {
        private readonly ILog log;                  // the application log
        private readonly ServerApp serverApp;       // the Server application in a project
        private readonly ServerConfig serverConfig; // the Server configuration
        private bool changing;                      // controls are being changed programmatically


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        private FrmGeneralOptions()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public FrmGeneralOptions(ILog log, ServerApp serverApp)
            : this()
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.serverApp = serverApp ?? throw new ArgumentNullException(nameof(serverApp));
            serverConfig = serverApp.AppConfig;
            changing = false;
        }
        
        
        /// <summary>
        /// Gets or sets the object associated with the form.
        /// </summary>
        public ChildFormTag ChildFormTag { get; set; }


        /// <summary>
        /// Sets the controls according to the configuration.
        /// </summary>
        private void ConfigToControls()
        {
            changing = true;

            // general options
            GeneralOptions generalOptions = serverConfig.GeneralOptions;
            numUnrelIfInactive.SetValue(generalOptions.UnrelIfInactive);
            numMaxLogSize.SetValue(generalOptions.MaxLogSize);

            // listener options
            ListenerOptions listenerOptions = serverConfig.ListenerOptions;
            numPort.SetValue(listenerOptions.Port);
            numTimeout.SetValue(listenerOptions.Timeout);
            txtSecretKey.Text = ScadaUtils.BytesToHex(listenerOptions.SecretKey);

            changing = false;
        }

        /// <summary>
        /// Sets the configuration according to the controls.
        /// </summary>
        private void ControlsToConfing()
        {
            // general options
            GeneralOptions generalOptions = serverConfig.GeneralOptions;
            generalOptions.UnrelIfInactive = decimal.ToInt32(numUnrelIfInactive.Value);
            generalOptions.MaxLogSize = decimal.ToInt32(numMaxLogSize.Value);

            // listener options
            ListenerOptions listenerOptions = serverConfig.ListenerOptions;
            listenerOptions.Port = decimal.ToInt32(numPort.Value);
            listenerOptions.Timeout = decimal.ToInt32(numTimeout.Value);
            listenerOptions.SecretKey = ScadaUtils.HexToBytes(txtSecretKey.Text.Trim());
        }

        /// <summary>
        /// Validates the form controls.
        /// </summary>
        private bool ValidateControls()
        {
            if (!ScadaUtils.HexToBytes(txtSecretKey.Text.Trim(), out _))
            {
                ScadaUiUtils.ShowError(CommonPhrases.InvalidSecretKey);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the changes of the child form data.
        /// </summary>
        public void Save()
        {
            if (ValidateControls())
            {
                ControlsToConfing();

                if (serverApp.SaveConfig(out string errMsg))
                    ChildFormTag.Modified = false;
                else
                    log.HandleError(errMsg);
            }
        }


        private void FrmCommonParams_Load(object sender, System.EventArgs e)
        {
            FormTranslator.Translate(this, GetType().FullName);
            ConfigToControls();
        }

        private void control_Changed(object sender, EventArgs e)
        {
            if (!changing)
                ChildFormTag.Modified = true;
        }

        private void txtSecretKey_Enter(object sender, EventArgs e)
        {
            txtSecretKey.UseSystemPasswordChar = false;
        }

        private void txtSecretKey_Leave(object sender, EventArgs e)
        {
            // otherwise the Tab key does not work
            Action action = () => { txtSecretKey.UseSystemPasswordChar = true; };
            Task.Run(() => { Invoke(action); });
        }

        private void btnGenerateKey_Click(object sender, EventArgs e)
        {
            txtSecretKey.Text = ScadaUtils.BytesToHex(ScadaUtils.GetRandomBytes(ScadaUtils.SecretKeySize));
            txtSecretKey.Focus();
        }

        private void btnCopyKey_Click(object sender, EventArgs e)
        {
            if (txtSecretKey.Text != "")
                Clipboard.SetText(txtSecretKey.Text);
        }
    }
}
