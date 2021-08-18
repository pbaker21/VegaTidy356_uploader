using System;
using System.IO;



class LogClass
{

        public void Logs(string myfun, string details)
        {
            var thedate = DateTime.Now;

            var file_date = thedate.ToString("yyyy-MM-dd");

            string path = @"tidylogs_" + file_date + ".txt";
                    
            FileStream stream = null;
        
            try
            {
                stream = new FileStream(path, FileMode.Append, FileAccess.Write);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                 writer.WriteLine("[" + thedate + "] [" + myfun + "] " + details );
                }
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }

}

