﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Scada.Server.Modules.ModArcPostgreSql
{
    /// <summary>
    /// Specifies the partition sizes.
    /// <para>Задает размеры секций.</para>
    /// </summary>
    public enum PartitionSize
    {
        OneMonth,
        OneYear
    }
}
