﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Web.Plugins.PlgScheme.Model.DataTypes;
using Scada.Web.Plugins.PlgScheme.Model.PropertyGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;

namespace Scada.Web.Plugins.PlgScheme.Model
{
    /// <summary>
    /// Scheme component that represents dynamic picture
    /// <para>Компонент схемы, представляющий динамический рисунок</para>
    /// </summary>
    [Serializable]
    public class DynamicPicture : StaticPicture, IDynamicComponent
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public DynamicPicture()
            : base()
        {
            BackColorOnHover = "";
            BorderColorOnHover = "";
            ImageOnHoverName = "";
            Action = Actions.None;
            Conditions = new List<ImageCondition>();
            InCnlNum = 0;
            CtrlCnlNum = 0;
        }


        /// <summary>
        /// Получить или установить цвет фона при наведении указателя мыши
        /// </summary>
        #region Attributes
        [DisplayName("Back color on hover"), Category(Categories.Behavior)]
        [Description("The background color of the component when user rests the pointer on it.")]
        //[Editor(typeof(ColorEditor), typeof(UITypeEditor))]
        #endregion
        public string BackColorOnHover { get; set; }

        /// <summary>
        /// Получить или установить цвет рамки при наведении указателя мыши
        /// </summary>
        #region Attributes
        [DisplayName("Border color on hover"), Category(Categories.Behavior)]
        [Description("The border color of the component when user rests the pointer on it.")]
        //[Editor(typeof(ColorEditor), typeof(UITypeEditor))]
        #endregion
        public string BorderColorOnHover { get; set; }

        /// <summary>
        /// Получить или установить наименование изображения, отображаемого при наведении указателя мыши
        /// </summary>
        #region Attributes
        [DisplayName("Image on hover"), Category(Categories.Behavior)]
        [Description("The image shown when user rests the pointer on the component.")]
        //[TypeConverter(typeof(ImageConverter)), Editor(typeof(ImageEditor), typeof(UITypeEditor))]
        [DefaultValue("")]
        #endregion
        public string ImageOnHoverName { get; set; }

        /// <summary>
        /// Получить или установить действие
        /// </summary>
        #region Attributes
        [DisplayName("Action"), Category(Categories.Behavior)]
        [Description("The action executed by clicking the left mouse button on the component.")]
        [DefaultValue(Actions.None)]
        #endregion
        public Actions Action { get; set; }

        /// <summary>
        /// Получить условия вывода изображений
        /// </summary>
        #region Attributes
        [DisplayName("Conditions"), Category(Categories.Behavior)]
        [Description("The conditions for image output depending on the value of the input channel.")]
        [DefaultValue(null), TypeConverter(typeof(CollectionConverter))]
        //[Editor(typeof(CollectionEditor), typeof(UITypeEditor))]
        #endregion
        public List<ImageCondition> Conditions { get; protected set; }

        /// <summary>
        /// Получить или установить номер входного канала
        /// </summary>
        #region Attributes
        [DisplayName("Input channel"), Category(Categories.Data)]
        [Description("The input channel number associated with the component.")]
        [DefaultValue(0)]
        #endregion
        public int InCnlNum { get; set; }

        /// <summary>
        /// Получить или установить номер канала управления
        /// </summary>
        #region Attributes
        [DisplayName("Output channel"), Category(Categories.Data)]
        [Description("The output channel number associated with the component.")]
        [DefaultValue(0)]
        #endregion
        public int CtrlCnlNum { get; set; }


        /// <summary>
        /// Загрузить конфигурацию компонента из XML-узла
        /// </summary>
        public override void LoadFromXml(XmlNode xmlNode)
        {
            base.LoadFromXml(xmlNode);

            BackColorOnHover = xmlNode.GetChildAsString("BackColorOnHover");
            BorderColorOnHover = xmlNode.GetChildAsString("BorderColorOnHover");
            ImageOnHoverName = xmlNode.GetChildAsString("ImageOnHoverName");
            Action = xmlNode.GetChildAsEnum<Actions>("Action");

            XmlNode conditionsNode = xmlNode.SelectSingleNode("Conditions");
            if (conditionsNode != null)
            {
                Conditions = new List<ImageCondition>();
                XmlNodeList conditionNodes = conditionsNode.SelectNodes("Condition");
                foreach (XmlNode conditionNode in conditionNodes)
                {
                    ImageCondition condition = new() { SchemeView = SchemeView };
                    condition.LoadFromXml(conditionNode);
                    Conditions.Add(condition);
                }
            }

            InCnlNum = xmlNode.GetChildAsInt("InCnlNum");
            CtrlCnlNum = xmlNode.GetChildAsInt("CtrlCnlNum");
        }

        /// <summary>
        /// Сохранить конфигурацию компонента в XML-узле
        /// </summary>
        public override void SaveToXml(XmlElement xmlElem)
        {
            base.SaveToXml(xmlElem);

            xmlElem.AppendElem("BackColorOnHover", BackColorOnHover);
            xmlElem.AppendElem("BorderColorOnHover", BorderColorOnHover);
            xmlElem.AppendElem("ImageOnHoverName", ImageOnHoverName);
            xmlElem.AppendElem("Action", Action);

            XmlElement conditionsElem = xmlElem.AppendElem("Conditions");
            foreach (Condition condition in Conditions)
            {
                XmlElement conditionElem = conditionsElem.AppendElem("Condition");
                condition.SaveToXml(conditionElem);
            }

            xmlElem.AppendElem("InCnlNum", InCnlNum);
            xmlElem.AppendElem("CtrlCnlNum", CtrlCnlNum);
        }

        /// <summary>
        /// Клонировать объект
        /// </summary>
        public override BaseComponent Clone()
        {
            DynamicPicture clonedComponent = (DynamicPicture)base.Clone();

            foreach (Condition condition in clonedComponent.Conditions)
            {
                condition.SchemeView = SchemeView;
            }

            return clonedComponent;
        }
    }
}
