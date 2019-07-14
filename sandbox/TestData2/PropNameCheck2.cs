// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace TestData2
{
    [MessagePackObject(true)]
    public class PropNameCheck2 : PropNameCheck1
    {
        public override string MyProperty2
        {
            get => base.MyProperty2;
            set => base.MyProperty2 = value;
        }
    }
}
