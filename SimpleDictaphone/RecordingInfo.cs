using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDictaphone
{
    public class RecordingInfo
    {
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreatedDate { get; set; }

        public RecordingInfo(string fileName, DateTime createdDate)
        {
            FileName = fileName;
            CreatedDate = createdDate;
            int lastUnderscoreIndex = fileName.LastIndexOf('_');
            int dotIndex = fileName.LastIndexOf('.');
            if (lastUnderscoreIndex > 0 && dotIndex > lastUnderscoreIndex)
            {
                DisplayName = fileName.Substring(lastUnderscoreIndex + 1, dotIndex - lastUnderscoreIndex - 1);
            }
            else
            {
                DisplayName = createdDate.ToString("yyyy-MM-dd HH:mm");
            }
        }
    }
}
