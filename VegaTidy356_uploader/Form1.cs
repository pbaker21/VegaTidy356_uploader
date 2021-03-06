using System;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Data;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Renci.SshNet;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VegaTidy356_uploader
{

    public partial class Form1 : Form
    {
        private DBConnection db;
      
        public string xml_path = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "/";

        DataTable table = new DataTable();

        XmlNodeList elemList;
      
        string scheduled_time;

        XmlDocument doc;

        string tagfiles_path;
        bool backup_to_server = false;

        LogClass mylogs = new LogClass();

        private string database_host;
        private string database_port;
        private string database_name;
        private string database_user;
        private string database_pword;


      //  private Aes256CbcEncrypter En;


        public Form1()
        {
            InitializeComponent();


            /* Useful sites :
             * 
             * https://cloudconvert.com/png-to-ico
            */


            /*  remotely accessing sql example:
             * 
                <database_remote>
                <host>217.172.143.116</host>
                <port>3306</port>
                <name>vegastats_test</name>
                <user>vegatest</user>
                <pword>1964Pib3</pword>
                </database_remote>
            */

            /*
             * Version 1.0.1 -- Added backedup csv tag file
             * Version 1.0.2 -- Added xml path for backedup csv tag file
             * Version 1.0.3 -- Updated xml file
             * Version 1.0.4 -- Updated remove functions
             * Version 1.0.5 -- Swapped tag log file save with the clearIncomingPhotos function
             * Version 1.0.6 -- Updated TaskScheduler
             * Version 1.0.7 -- tiding and adding comments
             * Version 1.0.8 -- Added new function IsDigitsOnly for foldername check + added new route for folder datename check
             * Version 1.0.9 -- Added Sleep 3 seconds, before the next clearout function i.e db.clearIncomingPhotos(datecomma);
             * Version 1.1.0 -- ?????
             * Version 1.1.1 -- tags files exist check!
             * Version 1.1.2 -- save tags locally and remotely
             * Version 1.1.3 -- re check db connections scheduler. Added `sweeping` ico to Form.
             #
             * Version 1.1.5 -- Now changed to "VegaTidy356_uploader" from "VegaTidy356_xml". Now as a new project!
             * Version 1.1.6 -- testing to see if this loads on another Wondows 10 PC
             * Version 1.1.7 -- updated config xml with new instructions to handle tags. updated logs to produce daily logs (rather than one lump of logs)
             * Version 1.1.8 -- updated mysql delete for function 'clearTagsWaiting'
             * Version 1.1.9 -- message box for `test_local_connection`
             * Version 1.2.0 -- made a small change to the saveTagsCSV SQL
             * Version 1.2.1 -- added site_id to the `tag_logs_data` table
             * Version 1.2.2 -- make sure there is an SQL connection upon actioning an event
             * Version 1.2.3 -- moved the saveTagsCSV function, to ensure it is triggered first before DELETING  the incomming files
             * Version 1.2.4 -- added 'limit 1' to the saveTagsCSV db function
             * Version 1.2.5 -- added FTP for tag log file. This will upload to our server after the  tag log file is saved.
             * Version 1.2.6 -- disabled remote DB. Enabled FTPs uploading. Added site name prefix to the upload file name and added extra site name field to CSV
             * Version 1.2.7 -- On startup it creates a log "folder" for the log files to go in
             * Version 1.2.8 -- added IsFileReady check when uploading files to server. This is to ensure the tags log file has been fully created & available before attempting to upload!
            */

            string version = "1.2.8";

            this.Text = "VegaTidy Uploader v" + version;
            
            mylogs.Logs("VegaTidy", "version: " + version);




            if (File.Exists(xml_path + "tidyconfig.xml"))
            {
                doc = new XmlDocument();

                doc.Load(xml_path + "tidyconfig.xml");
                
                //====================

                string database_root = "database_local/";

                database_host  = doc.DocumentElement.SelectSingleNode(database_root + "host").InnerText.Trim();
                database_port  = doc.DocumentElement.SelectSingleNode(database_root + "port").InnerText.Trim();
                database_name  = doc.DocumentElement.SelectSingleNode(database_root + "name").InnerText.Trim();
                database_user  = doc.DocumentElement.SelectSingleNode(database_root + "user").InnerText.Trim();
                database_pword = doc.DocumentElement.SelectSingleNode(database_root + "pword").InnerText.Trim();

               //database_pword = Aes256CbcEncrypter.Decrypt(database_pword, Key);
               //Console.WriteLine("^^ " + database_pword);


                db = new DBConnection(database_host, database_port, database_name, database_user, database_pword);

                string source_result1 = db.test_local_connection();
                
                mysql_connection_msg.Text = source_result1;

                //=======================================================
                
                tagfiles_path = checkForSlash(doc.DocumentElement.SelectSingleNode("tags_path").InnerText.Trim());

                string tags_root = "tags_action/";
                               
                backup_to_server  = Convert.ToBoolean(doc.DocumentElement.SelectSingleNode(tags_root + "backup_to_server").InnerText);

                mylogs.Logs("Backup tags to server", "Status: " + backup_to_server.ToString());
                

                //=======================================================

                System.DateTime moment = DateTime.Now;

                int hour   = moment.Hour;
                int minute = moment.Minute;

                //====================

                string trigger_root = "trigger_event/";

                string event_status = doc.DocumentElement.SelectSingleNode(trigger_root + "status").InnerText.Trim();
                scheduled_time = doc.DocumentElement.SelectSingleNode(trigger_root + "scheduled_time").InnerText.Trim();


                if (!IsValidTimeFormat(scheduled_time)) // is the given time format correct?
                {
                    MessageBox.Show("XML Error:\n`scheduled_time` time format in incorrect = '" + scheduled_time + "'");
                    System.Environment.Exit(1);
                }

                scheduled.Text = scheduled_time + "  " + event_status;
                
                XmlNode node6 = doc.DocumentElement.SelectSingleNode("photo_root_dir");
                XmlNode node7 = doc.DocumentElement.SelectSingleNode("show_results");
                                
                //==========================================================================================================================
                // Set up a table to display the list of folders to be processed.

                dataGridView.ColumnCount = 4;

                // Set the column header names.
                dataGridView.Columns[0].Name = "Label";
                dataGridView.Columns[1].Name = "Folder Path";
                dataGridView.Columns[2].Name = "Days Past";
                dataGridView.Columns[3].Name = "Actioned";

                dataGridView.Columns[0].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dataGridView.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                dataGridView.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;


                dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                dataGridView.Columns["Label"].Width = 60;
                dataGridView.Columns["Folder Path"].Width = 280;

                dataGridView.Columns["Days Past"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dataGridView.Columns["Actioned"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                //==========================================================================================================================
                
                //clear folders
                elemList = doc.GetElementsByTagName("clear_item");

                string lb = "";
                string fn = "";
                string tg = "";

                foreach (XmlNode mydata in elemList)
                {
                    int dp = Convert.ToInt32(mydata["days_past"].InnerText);
                   
                    if (mydata["folder_name"] != null) {

                        fn = checkForSlash(mydata["folder_name"].InnerText.Replace("\\", "/"));

                        lb = "folder";
                    }

                    if (mydata["files"] != null) // if within the `clear_item` tag it finds the xml tag `files` 
                    {
                        tg = mydata["files"].Attributes["location"].InnerText;

                        if (doc.DocumentElement.SelectSingleNode(tg) != null) {

                            fn = checkForSlash(doc.DocumentElement.SelectSingleNode(tg).InnerText);

                            lb = "file";
                        }
                    }

                    dataGridView.Rows.Add(lb, fn, dp, "--");
                }

                //==========================================================================================================================
                
                if (IsMatch(event_status, "^(daily)|(once)+$"))
                {
                    mylogs.Logs("Trigger event type", "Status: " + event_status);


                    if (event_status == "daily") // trigger everyday at `scheduled_time`
                    {                      
                        DateTime alarm = DateTime.ParseExact(scheduled_time, "H:m:s", null);

                        mylogs.Logs("Process Start Time", alarm.ToString());
                                               

                        TaskScheduler.Instance.ScheduleTask(alarm.Hour, alarm.Minute, 24, () => {

                                mylogs.Logs("triggerMyEvents!!!", "DateTime: " + DateTime.Now);
                            
                                // Reset/clear the last update info (ie. any checks/ticks) ready for any new `checks`
                                for (int i = 0; i < dataGridView.Rows.Count; i++)
                                {
                                    dataGridView.Rows[i].Cells["Actioned"].Value = "--";
                                }

                                ClearOutTasks(table, elemList); // action this function
                        });

                    }


                    // trigger just the once when program executed
                    if (event_status == "once")
                    {
                        ClearOutTasks(table, elemList);

                        System.Environment.Exit(1);
                    }
                }
                else
                {
                    MessageBox.Show("XML Error:\n`status` in 'trigger_event' format in incorrect = '" + event_status + "'");
                    System.Environment.Exit(1);
                }

            }
            else
            {
                MessageBox.Show("Could not find '" + xml_path + "tidyconfig.xml' file.");

                System.Environment.Exit(1);
            }
            
        }




        public bool IsConnectedLocally() {

            if (db.test_local_connection() != "OK") {
                db = new DBConnection(database_host, database_port, database_name, database_user, database_pword);
            }

            return true;
        }




        public static bool IsMatch(string input, string pattern)
        {
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(input);
        }

        


        public bool IsValidTimeFormat(string input)
        {
            TimeSpan dummyOutput;
            return TimeSpan.TryParse(input, out dummyOutput);
        }




      

        private void ClearOutTasks(DataTable table, XmlNodeList elemList)
        {
            string datecomma;

            int days_past = 0;

            // make sure there is a connection (just incase there wasn't one when this program started or dropped out at some point)
            db = new DBConnection(database_host, database_port, database_name, database_user, database_pword);

            string source_result1 = db.test_local_connection();
            
            Invoke(new Action(() =>
            {
                mysql_connection_msg.Text = source_result1;
            }));



            /// deal with tags
            var fn = db.saveTagsCSV(tagfiles_path); // save the tag CSV files to `tagfiles_location`

            int attempts = 0;


            if (backup_to_server) // we'll back tags up to the server if this is TRUE
            {
                mylogs.Logs("#UploadFileToFTP = ", "upload file : " + fn + " ~~ " + tagfiles_path);

                while (!IsFileReady(tagfiles_path + fn) && attempts < 5)
                {
                    attempts++;

                    mylogs.Logs("Waiting to upload file (" + attempts + ") : ", tagfiles_path + fn);

                    Thread.Sleep(1000);
                }

                UploadFileToFTP(tagfiles_path, fn);                
            }



            for (int i = 0; i < dataGridView.Rows.Count; i++) // step through each table row for its settings
            {
                DataGridViewRow row = dataGridView.Rows[i];
                string type_item    = row.Cells[0].Value.ToString();
                string folder_name  = row.Cells[1].Value.ToString();
                days_past           = Convert.ToInt32(row.Cells[2].Value);
         

                if (type_item == "folder" && IsDirectoryEmpty(folder_name))  // if the `type_item` is a folder and is not empty, then continue.
                { 
                
                    if (containsDatedFolder(folder_name)) // if the folder is date formatted, process it.
                    {
                        datecomma = clearDateSpecificFolders(folder_name, days_past); // remove folders based on their dates, return these dates

                        if (datecomma != "")
                        {
                            db.clearTaggedPhotos(datecomma);

                            db.clearIncomingPhotos(datecomma);

                            mylogs.Logs("ClearOutTasks", "datecomma: " + datecomma);

                            dataGridView.Rows[i].Cells["Actioned"].Value = "✔"; // display a tick/check after each folder process actioned
                        }
                                               
                    } else {
                       // log("This is a folder but a normal type! : ", folder_name + " ==== " + containsDatedFolder(folder_name));

                        datecomma = clearDateSpecificFolders(folder_name, days_past);

                        dataGridView.Rows[i].Cells["Actioned"].Value = "✔";
                    }
                }


                if (type_item == "file")
                { // remove these files after `days_past` amount of days

                    DeleteDaysPastFolders(folder_name, days_past, i);
                }

               // mylogs.Logs("#type_item = ", type_item);

            }// end for

          
        }






        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }





        public string clearDateSpecificFolders(string mypath, int days_past)
        {
            DateTime expiration_date = DateTime.Now.AddDays(-days_past);
            
            string[] fileArray = Directory.GetDirectories(mypath);

            string datecomma = "";


            for (int i = fileArray.Length; i-- > 0;)
            {
                string[] dateonly = fileArray[i].Split(new char[] { '/' });  // create an array split by '/' from the found directory path
                                
                string dir_date = dateonly.Last(); // get the last item from the array, which is the date

                mylogs.Logs("clearDateSpecificFolders =~=~>", mypath + " -- " + days_past);


                if (IsDigitsOnly(dir_date))
                {
                    DateTime dirDate = DateTime.ParseExact(dir_date, "yyyyMMdd", CultureInfo.InvariantCulture);

                    mylogs.Logs("clearDateSpecificFolders", expiration_date + " -- " + dirDate + " -- " + fileArray[i]);


                    if (expiration_date > dirDate && Directory.Exists(fileArray[i]))    // check date range and that the directory exists
                    {
                        // Console.WriteLine(expiration_date + " >  " + dirDate + " == " + fileArray[i]);

                        mylogs.Logs("expiration_date", dirDate + " -- " + fileArray[i]);

                        DeleteDirectory(fileArray[i], true);

                        string to_sql_date = DateTime.ParseExact(dir_date, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd"); // use the last date item, so as to be converted to an sql date format

                        datecomma += "'" + to_sql_date + "',";  // build a list of directories being deleted
                    }
                }
            }

            datecomma = datecomma.TrimEnd(','); // remove the last comma from the newly built list of removed directories

            return datecomma;
        }







        private void DeleteDaysPastFolders(string mypath, int days_past, int inx)
        {
            string[] files = Directory.GetFiles(mypath);


            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);

                bool date_pass_flag = (fi.LastWriteTime < DateTime.Now.AddDays(-days_past));

                if (date_pass_flag)
                {
                    mylogs.Logs("Deleting File", file + " -- " + fi.LastAccessTime + " == " + DateTime.Now.AddDays(-days_past));

                    dataGridView.Rows[inx].Cells["Actioned"].Value = "✔"; // display a tick/check after each file process actioned

                    fi.Delete();
                }
            }

        }





        public string checkForSlash(string str)
        {
            char lastChar = System.Convert.ToChar(str.Substring(str.Length - 1));

            if (lastChar == '\\' || lastChar == '/')
            {
                return str;
            }
            else
            {
                return str + "/";
            }
        }





        private bool IsDirectoryEmpty(string path)
        {
            try
            {
                string[] dirs  = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path);

                if (dirs.Length == 0 && files.Length == 0) { 
                        return false;
                }else{ 
                        return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("IsDirectoryEmpty: {0}", e.ToString());
                return false;
            }
        }



               


        private bool containsDatedFolder(string myfolder) // checks to see if any of the path folders contains a date - example "C:\xampp\htdocs\VEGA_ongoing\vega\archive\20181116"
        {
            List<string> destDir = Directory.GetDirectories(myfolder, "*", SearchOption.AllDirectories).Where(f => Regex.IsMatch(f, @"[\\/]\d+[\\/$]")).ToList();

            if (destDir.Count() > 0)
            {
                return true;
            }
            else {
                return false;
            }

        }






        private void DeleteDirectory(string path, bool recursive)
        {
            if (recursive)
            {
                var subfolders = Directory.GetDirectories(path);
                foreach (var s in subfolders)
                {
                    DeleteDirectory(s, recursive);
                    mylogs.Logs("DeleteDirectory", "path:" + s);
                }
            }

            var files = Directory.GetFiles(path);

            foreach (var f in files)
            {
                try
                {
                    var attr = File.GetAttributes(f);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(f, attr ^ FileAttributes.ReadOnly);
                    }
                    File.Delete(f);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("^ " + ex);
                }
            }

            //Console.WriteLine("^ " + path);

            Thread.Sleep(450);

            Directory.Delete(path, true);
        }




        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        private static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }
        



        
        private void changeFileCreationDate(string y, string m, string d)
        {
            string filedate = y + m + d;

            int yy = Convert.ToInt32(y);
            int mm = Convert.ToInt32(m);
            int dd = Convert.ToInt32(d);
            
            string path = @"C:\xampp\htdocs\vega_356\archives\error_logs_" + filedate + ".txt";

            if (!File.Exists(path))
            {
                File.Create(path);
            }
            else
            {
                // Take an action that will affect the write time.
                File.SetCreationTime(path, new DateTime(yy, mm, dd));
            }

            //Console.WriteLine("^ " + path + " = " + yy+"-"+ mm + "-" + dd);
        }



        


        //
        // To be used inconjunction with - C:\xampp\htdocs\process_tag_logs\
        //
        private void UploadFileToFTP(string filePath, string fileName)
        {/*
            SftpClient sftp = new SftpClient("crazygeek.co.uk", "crazygee", "Morning~Summer2");
            var directory = "public_html/csv_backups/"; // ftp upload path
            */

            SftpClient sftp = new SftpClient("photo.imageinsight.com", "iicom", "Pc5%ij69");

            var directory = "/var/www/vhosts/imageinsight.com/photo.imageinsight.com/tag_logs_backup_files/incoming_tag_files/";
           
            sftp.KeepAliveInterval = TimeSpan.FromSeconds(60);
            //sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(180);
            sftp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(120);
            sftp.OperationTimeout = TimeSpan.FromMinutes(180);

            try
            {
                sftp.Connect(); // lets try and connect
            
                if (sftp.IsConnected) // we have a connection
                {
                    sftp.ChangeDirectory(directory); // ChangeDirectory to our server path, where we're uploading the files to.

                    try
                    {
                        using (FileStream filestream = File.OpenRead(filePath + fileName))
                        {
                            sftp.UploadFile(filestream, fileName, null); 
                            sftp.Disconnect();
                            sftp.Dispose();
                        }
                        mylogs.Logs("UploadFileToFTP:", fileName);
                    }
                    catch (FileNotFoundException ex)
                    {
                        mylogs.Logs("UploadFileToFTP: FileNotFoundException", ex.ToString());
                        sftp.Disconnect();
                    }
                }
                else
                {
                    mylogs.Logs("UploadFileToFTP: Connect", "Did not connect.");
                    sftp.Disconnect();
                }            
            }
            catch (Exception ex)
            {
                sftp.Disconnect();

                mylogs.Logs("UploadFileToFTP: Could not connect to host: ", ex.Message.ToString());
            }
            
        }





        private bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                return inputStream.Length > 0;
            }
            catch (Exception ex)
            {
                mylogs.Logs("IsFileReady: Err ", ex.Message.ToString());

                return false;
            }
        }

        




        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView.ClearSelection();
        }

    }
}
