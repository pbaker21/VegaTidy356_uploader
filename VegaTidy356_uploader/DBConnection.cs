using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;

/*
 * C:\Users\Paul_\source\repos\VegaTidy356_uploader\VegaTidy356_uploader\bin\Debug
 */



class DBConnection
{
    private MySqlConnection connection;

    //public LogClass mylogs;
    LogClass mylogs = new LogClass();




    //Constructor
    public DBConnection(string server, string port, string database, string user, string password)
    {
        string connectionString = @"SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + user + ";" + "PASSWORD=" + password + ";SslMode=none;";

        connection = new MySqlConnection(connectionString);
    }

    



    public string test_local_connection()
    {
        string result = "OK";

        try
        {
            connection.Open();
            
            connection.Close();
        }
        catch (MySqlException ex)
        {
            // Console.WriteLine("SQL No. " + ex.Number + " = " + ex.Message);

            mylogs.Logs("SQL Local Connection Error: ", ex.Number + " = " + ex.Message);

            return ex.Message;
        }

        return result;
    }







    private bool OpenConnection()
    {
        try
        {
            connection.Open();
            return true;
        }
        catch (MySqlException ex)
        {
            //When handling errors, your application's response based on the error number.
            //
            //The two most common error numbers when connecting are as follows:
            //   0: Cannot connect to server.
            //1045: Invalid user name and/or password.

            Console.WriteLine("Local - SQL No. " + ex.Number);

            switch (ex.Number)
            {
                case 0:
                    Console.WriteLine("Local - SQL Connection : Cannot connect to server.");
                    break;

                case 1045:
                    Console.WriteLine("Local - SQL Connection : Invalid username/password, please try again");
                    break;
            }
            return false;
        }
    }






    public void clearIncomingPhotos(string datelist)
    {
        string query = @"DELETE FROM `incoming_photos` WHERE DATE(incoming_photos.stamp) IN(" + datelist + ")";

          mylogs.Logs("clearIncomingPhotos query: ", query);

        //open connection
        if (this.OpenConnection() == true)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);

                cmd.ExecuteNonQuery();

              //  mylogs.Logs("Local - clearIncomingPhotos: ", cmd.ToString());
            }
            catch (Exception ex)
            {
                mylogs.Logs("Local - clearIncomingPhotos - Err: ", ex.ToString());
            }
            //close connection
            this.CloseConnection();
        }

        clearTagsWaiting(datelist);
    }

    





    public void clearTagsWaiting(string datelist)
    {
        string query = "DELETE FROM `tags_waiting` WHERE DATE(tags_waiting.stamp) IN(" + datelist + ")";

        //open connection
        if (this.OpenConnection() == true)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

                mylogs.Logs("Local - clearTagsWaiting - Err: ", ex.ToString());
            }
            //close connection
            this.CloseConnection();
        }

    }



    

    public void saveTagsCSV(string tagspath) // save (to file) a date range of tags with file name dated as todays date.
    {
        var thedate = DateTime.Now.ToString("yyyy-MM-dd");

        if (!File.Exists(tagspath.Replace("\\", "/") + "backedup_tags_" + thedate + ".csv"))
        {
            // tags are backed up per todays date. This means if DATE(incoming_photos.stamp) = DATE(NOW()) then save all findings
            string query_tags = @"SELECT pk_id, tag_user_id, (SELECT DISTINCT purchase_order.purchase_order_number FROM `purchased_items` JOIN purchase_order ON purchase_order.order_id = purchased_items.order_id WHERE photo_names = incoming_photos.photocode) AS purchase_order_number, SUBSTRING(photocode FROM 1 FOR CHAR_LENGTH(photocode) - 4) AS photo, prefix, photo_number, tag_id, tag_location, stamp INTO OUTFILE '" + tagspath.Replace("\\", "/") + "backedup_tags_" + thedate + ".csv' FIELDS TERMINATED BY ',' ENCLOSED BY '\"' LINES TERMINATED BY '\n' FROM `incoming_photos` WHERE tag_id<> '' AND DATE(incoming_photos.stamp) = DATE(NOW()); ";

            mylogs.Logs("Local - saveTagsCSV", query_tags);


            //open connection
            if (this.OpenConnection() == true)
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(query_tags, connection);

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                 
                    mylogs.Logs("Local - saveTagsCSV - Err: ", ex.ToString());
                }
                //close connection
                this.CloseConnection();
            }
        }
        else {
            mylogs.Logs("saveTagsCSV:Error file exists", thedate);
        }
    }





    //tags_backup


    public void clearTaggedPhotos(string datelist)
    {
        string query = "UPDATE `incoming_photos` SET tag_id = '', tag_location = '', tag_user_id = '' WHERE tag_id <> '' AND DATE(incoming_photos.photo_date) IN(" + datelist + ")";

        mylogs.Logs("clear Incoming_photos Tagged Photos", query);


        //open connection
        if (this.OpenConnection() == true)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                mylogs.Logs("clearTaggedPhotos - Err: ", ex.ToString());
            }
            //close connection
            this.CloseConnection();
        }

    }



    

    public List<DataInserts> GetPhotoTagsList()    // create or update todays new list of photos
    {
        List<DataInserts> data = new List<DataInserts>();
        int days = 0;


        if (this.OpenConnection() == true)
        {
            var myquery = "SELECT pk_id, (SELECT general_settings.current_site_id FROM `general_settings`) AS site_id, (SELECT DISTINCT purchase_order.purchase_order_number FROM `purchased_items` JOIN purchase_order ON purchase_order.order_id = purchased_items.order_id WHERE photo_names = incoming_photos.photocode) AS purchase_order_number, tag_user_id, SUBSTRING(photocode FROM 1 FOR CHAR_LENGTH(photocode) - 4) AS photo, prefix, photo_number, tag_id, tag_location, DATE_FORMAT(stamp,\"%Y-%m-%d %H:%i:%s\") AS mystamp FROM `incoming_photos` WHERE tag_id<> '' AND DATE(incoming_photos.stamp) <= DATE_ADD(CURDATE(), INTERVAL - " + days + "  DAY);";

            mylogs.Logs("SQL GetPhotoTagsList", myquery);

                using (MySqlCommand cmd = new MySqlCommand(myquery, connection))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                data.Add(new DataInserts
                                {
                                    fk_id        = reader["pk_id"].ToString(),
                                    site_id      = reader["site_id"].ToString(),
                                    purchase_order_number = reader["purchase_order_number"].ToString(),
                                    tag_user_id  = reader["tag_user_id"].ToString(),
                                    photocode    = reader["photo"].ToString(),
                                    prefix       = reader["prefix"].ToString(),
                                    photo_number = reader["photo_number"].ToString(),
                                    tag_id       = reader["tag_id"].ToString(),
                                    tag_location = reader["tag_location"].ToString(),
                                    mystamp      = reader["mystamp"].ToString()
                                });
                        
                            }//while
                        }// if
                    }// using
                }// using                
            }//if
            
            this.CloseConnection();

            return data;
        }

  







    //Close connection
    private bool CloseConnection()
    {
        try
        {
            connection.Close();
            return true;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }




}
