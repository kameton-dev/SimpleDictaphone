using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDictaphone
{
    public class RecordingsPageGroup : List<RecordingInfo>
    {
        public string Key { get; set; }

        public RecordingsPageGroup(string key, List<RecordingInfo> items)
            : base(items)
        {
            Key = key;
        }
    }
}
