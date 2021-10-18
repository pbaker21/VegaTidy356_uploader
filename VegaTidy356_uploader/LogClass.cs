using System;
using System.IO;
using System.Reflection;

class LogClass
{

        private string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("\\", "/") + "/logs/";


        public LogClass() { 
            CreateDirectory();
        }



        public void CreateDirectory()
        {
            // Specify the directory you want to manipulate.
            
      //   Console.WriteLine("Thee path: " + path);

            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                  //  Console.WriteLine("That path exists already.");
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                //Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(path));

                // Delete the directory.
                //di.Delete();
                //Console.WriteLine("The directory was deleted successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            finally { }
        }




        public void Logs(string myfun, string details)
        {
            var thedate = DateTime.Now;

            var file_date = thedate.ToString("yyyy-MM-dd");

            string lpath = path + "/tidylogs_" + file_date + ".txt";
                    
            FileStream stream = null;
        
            try
            {
                stream = new FileStream(lpath, FileMode.Append, FileAccess.Write);
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

