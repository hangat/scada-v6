﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Admin.Extensions.ExtProjectTools.Code;
using Scada.Forms;
using System;
using System.IO;
using System.Windows.Forms;

namespace Scada.Admin.Extensions.ExtProjectTools.Controls
{
    /// <summary>
    /// Represents a control that contains a main menu and a toolbar.
    /// <para>Представляет элемент управления, содержащий главное меню и панель инструментов.</para>
    /// </summary>
    public partial class CtrlMainMenu : UserControl
    {
        private readonly IAdminContext adminContext; // the Administrator context


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        private CtrlMainMenu()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CtrlMainMenu(IAdminContext adminContext)
            : this()
        {
            this.adminContext = adminContext ?? throw new ArgumentNullException(nameof(adminContext));
        }


        /// <summary>
        /// Gets menu items to add to the main menu.
        /// </summary>
        public ToolStripMenuItem[] GetMainMenuItems()
        {
            return new ToolStripMenuItem[] { miProjectTools };
        }

        /// <summary>
        /// Gets tool buttons to add to the toolbar.
        /// </summary>
        public ToolStripButton[] GetToobarButtons()
        {
            return new ToolStripButton[] { btnAddLine, btnAddDevice, btnCreateChannels };
        }

        /// <summary>
        /// Enables or disables main menu items and toolbar buttons.
        /// </summary>
        public void SetMenuItemsEnabled()
        {
            bool projectIsOpen = adminContext.CurrentProject != null;
            miAddLine.Enabled = btnAddLine.Enabled = projectIsOpen;
            miAddDevice.Enabled = btnAddDevice.Enabled = projectIsOpen;
            miCreateChannels.Enabled = btnCreateChannels.Enabled = projectIsOpen;
            miCloneChannels.Enabled = projectIsOpen;
            miChannelMap.Enabled = projectIsOpen;
            miCheckIntegrity.Enabled = projectIsOpen;
            miImportTable.Enabled = projectIsOpen;
            miExportTable.Enabled = projectIsOpen;
        }


        private void miAddLine_Click(object sender, EventArgs e)
        {
            ScadaUiUtils.ShowInfo("Test");
        }

        private void miAddDevice_Click(object sender, EventArgs e)
        {

        }

        private void miCreateChannels_Click(object sender, EventArgs e)
        {

        }

        private void miCloneChannels_Click(object sender, EventArgs e)
        {

        }

        private void miChannelMap_Click(object sender, EventArgs e)
        {

        }

        private void miCheckIntegrity_Click(object sender, EventArgs e)
        {
            // check integrity
            if (adminContext.CurrentProject != null)
            {
                new IntegrityCheck(adminContext.ErrLog, adminContext.CurrentProject.ConfigBase)
                    .Execute(Path.Combine(adminContext.AppDirs.LogDir, IntegrityCheck.OutputFileName));
            }
        }

        private void miImportTable_Click(object sender, EventArgs e)
        {

        }

        private void miExportTable_Click(object sender, EventArgs e)
        {

        }
    }
}
