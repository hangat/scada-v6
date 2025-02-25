﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml;

namespace Scada.Comm.Drivers.DrvOpcUa.Config
{
    /// <summary>
    /// Represents options for editing a device configuration.
    /// <para>Представляет параметры редактирования конфигурации КП.</para>
    /// </summary>
    public class EditingOptions
    {
        /// <summary>
        /// Gets or sets the rule to set tag codes by default.
        /// </summary>
        public DefaultTagCode DefaultTagCode { get; set; } = DefaultTagCode.NodeID;
        

        /// <summary>
        /// Loads the options from the XML node.
        /// </summary>
        public void LoadFromXml(XmlNode xmlNode)
        {
            if (xmlNode == null)
                throw new ArgumentNullException(nameof(xmlNode));

            DefaultTagCode = xmlNode.GetChildAsEnum<DefaultTagCode>("DefaultTagCode");
        }

        /// <summary>
        /// Saves the options into the XML node.
        /// </summary>
        public void SaveToXml(XmlElement xmlElem)
        {
            if (xmlElem == null)
                throw new ArgumentNullException(nameof(xmlElem));

            xmlElem.AppendElem("DefaultTagCode", DefaultTagCode);
        }
    }
}
