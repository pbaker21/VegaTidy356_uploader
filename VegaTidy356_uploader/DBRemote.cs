using MySql.Data.MySqlClient;
using System;



class DBRemote
{
    private MySqlConnection connection;

    LogClass mylogs = new LogClass();



    //Constructor
    public DBRemote(string server, string port, string database, string user, string password)
    {
        string connectionString = @"SERVER=" + server + ";" + "PORT=" + port + ";" + "DATABASE=" + database + ";" + "UID=" + user + ";" + "PASSWORD=" + password + ";SslMode=none;";

        connection = new MySqlConnection(connectionString);
    }




    public string test_remote_connection()
    {
        string result = "OK";

        try
        {
            connection.Open();

            connection.Close();
        }
        catch (MySqlException ex)
        {

            mylogs.Logs("SQL Remote Connection Error: ", ex.Number + " = " + ex.Message);

            return ex.Message;
        }

        return result;
    }





    
    public bool OpenConnection()
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

            mylogs.Logs("SQL Err No.", ex.Number.ToString());

            switch (ex.Number)
            {
                case 0:
                    mylogs.Logs("SQL Connection", "Cannot connect to server.");
                    break;

                case 1045:
                    mylogs.Logs("SQL Connection", "Invalid username/password, please try again.");
                    break;
            }
            return false;
        }
    }
    





    public void SaveRecord(string id, string purchase_order_number, string tag_user_id, string photocode, string prefix, string photo_number, string tag_id, string tag_location, string mystamp)
    {
        //Open connection

        purchase_order_number = (purchase_order_number == "\\N") ? "" : purchase_order_number;

        if (id != "")
        {
                string query = @"INSERT INTO `tag_logs_data` (`fk_id`, `purchase_order_number`, `tag_user_id`, `photocode`, `prefix`, `photo_number`, `tag_id`, `tag_location`, `mystamp`, `actioned_datetime`) VALUES (@param_val_1, @param_val_2, @param_val_3, @param_val_4, @param_val_5, @param_val_6, @param_val_7, @param_val_8, @param_val_9, NOW())";

           //     mylogs.Logs("SQL SaveRecord", query); 

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    try
                    {                    
                        cmd.Parameters.AddWithValue("@param_val_1", id);
                        cmd.Parameters.AddWithValue("@param_val_2", purchase_order_number);
                        cmd.Parameters.AddWithValue("@param_val_3", tag_user_id);
                        cmd.Parameters.AddWithValue("@param_val_4", photocode);
                        cmd.Parameters.AddWithValue("@param_val_5", prefix);
                        cmd.Parameters.AddWithValue("@param_val_6", photo_number);
                        cmd.Parameters.AddWithValue("@param_val_7", tag_id);
                        cmd.Parameters.AddWithValue("@param_val_8", tag_location);
                        cmd.Parameters.AddWithValue("@param_val_9", mystamp);

                        cmd.ExecuteScalar();
                    }
                    catch (MySqlException e)
                    {
                        mylogs.Logs("SQL SaveRecord Error", e.Number.ToString() + " -> " + e.Message.ToString());
                    }
                }            
        }
    }





    public void Close()
    {
        connection.Close();
    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

}