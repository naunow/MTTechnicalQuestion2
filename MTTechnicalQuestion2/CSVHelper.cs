using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MTTechnicalQuestion2
{
    internal class CSVHelper
    {
        private IEnumerable<object> data;

        public CSVHelper(IEnumerable<object> data)
        {
            this.data = data;
        }

        internal byte[] WriteRecords(bool outputHeader = false)
        {
            string csvData = string.Empty;
            if(outputHeader) csvData += OutputHeader();

            foreach (var item in this.data)
            {
                csvData += OutputLine(item);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(csvData);

            return bytes;
        }

        private string OutputHeader()
        {
            Type t = this.data.FirstOrDefault().GetType();
            PropertyInfo[] properties = t.GetProperties();
            string[] titles;

            titles = properties.Select(x => x.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>().DisplayName).ToArray();

            var sb = new StringBuilder();
            int i = 0;
            foreach (var title in titles)
            {
                if(i > 0) sb.Append(",");
                sb.Append(title);
                i++;
            }
            sb.Append('\n');
            return sb.ToString();
        }

        private string OutputLine(object obj)
        {
            var sb = new StringBuilder();
            var i = 0;
            PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                string str;
                if (prop.PropertyType.Equals(typeof(DateTime)))
                {
                    DateTime.TryParse(prop.GetValue(obj).ToString(), out DateTime d);
                    str = d.ToString("yyyy-MM-dd");
                    if (i++ > 0) sb.Append(",");
                    sb.Append(str);
                }
                else if (prop.PropertyType.Equals(typeof(string)))
                {
                    str = prop.GetValue(obj).ToString();
                    if (i++ > 0) sb.Append(",");
                    sb.Append("'" + str + "'");
                }
                else
                {
                    str = prop.GetValue(obj).ToString();
                    if (i++ > 0) sb.Append(",");
                    sb.Append(str);
                }
            }
            sb.Append('\n');

            return sb.ToString();
        }
    }
}