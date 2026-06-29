using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothTest
{
    public class BluetoothDeviceInfo
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";

        public override string ToString() => Name;
    }
}
