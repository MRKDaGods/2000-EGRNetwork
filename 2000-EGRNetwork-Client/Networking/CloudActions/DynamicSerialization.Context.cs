using System.Collections.Generic;

namespace MRK.Networking.CloudActions
{
    public partial class DynamicSerialization
    {
        class Context
        {
            private readonly bool _array;
            private object _value;

            public object Value
            {
                get { return _array ? ((List<object>)_value).ToArray() : _value; }
                set { SetValue(value); }
            }

            public Context(bool isArray)
            {
                _array = isArray;

                if (_array)
                {
                    _value = new List<object>();
                }
            }

            private void SetValue(object val)
            {
                if (_array)
                {
                    //add val to array
                    ((List<object>)_value).Add(val);
                }
                else
                {
                    _value = val;
                }
            }
        }
    }
}
