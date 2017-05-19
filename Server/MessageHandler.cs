using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Collections.ObjectModel;
using MySql.Data.MySqlClient;

using NetworkCommsDotNet;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;
using NetworkCommsDotNet.Connections;
using System.Data;
using System.Web.Script.Serialization;

namespace Server
{
    class MessageHandler
    {
        #region Fields
        private List<DataTable> tablesToSend;
        static string toSendObject;
        private MySqlConnection sqlConnection;
        private IPEndPoint adress;
        private CommandRecognizer commandRecognizer;
        private ServerStatus serverStatus;
        private ServerDataStatus serverDataStatus;
        private Connection connection;
        private string standartFileStorageLocation;

        ObservableCollection<ReceivedFile> receivedFiles = new ObservableCollection<ReceivedFile>();
        Dictionary<ConnectionInfo, Dictionary<string, ReceivedFile>> receivedFilesDict = new Dictionary<ConnectionInfo, Dictionary<string, ReceivedFile>>();
        Dictionary<ConnectionInfo, Dictionary<long, byte[]>> incomingDataCache = new Dictionary<ConnectionInfo, Dictionary<long, byte[]>>();
        Dictionary<ConnectionInfo, Dictionary<long, SendInfo>> incomingDataInfoCache = new Dictionary<ConnectionInfo, Dictionary<long, SendInfo>>();
        SendReceiveOptions customOptions = new SendReceiveOptions<ProtobufSerializer>();
        object syncRoot = new object();
        #endregion

        public void StartServer()
        {
            adress = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 8080);
            standartFileStorageLocation = @"C:\Users\Infamous\Desktop\Projects\OnlineJournal\Server\ReceivedFiles";
            ConfigureDataBaseConnection();
        }

        private void ConfigureDataBaseConnection()
        {
            string serverName = "localhost";
            string userName = "root";
            string dbName = "universitydb";
            string port = "3306";
            string password = "IwtFbhGh_71";
            string connection = "server=" + serverName +
                ";user=" + userName +
                ";database=" + dbName +
                ";port=" + port +
                ";password=" + password + ";";
            sqlConnection = new MySqlConnection(connection);
        }

        public void StartListening()
        {
            Connection.StartListening(ConnectionType.TCP, adress);
            NetworkComms.AppendGlobalIncomingPacketHandler<byte[]>("PartialFileData", IncomingPartialFileData);
            NetworkComms.AppendGlobalIncomingPacketHandler<SendInfo>("PartialFileDataInfo", IncomingPartialFileDataInfo);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("Message", ReadIncommingCommand);

            Console.WriteLine("Server listening for TCP connection on:");
            foreach (System.Net.IPEndPoint localEndPoint in Connection.ExistingLocalListenEndPoints(ConnectionType.TCP))
            {
                Console.WriteLine("{0}:{1}", localEndPoint.Address, localEndPoint.Port);
            }
        }

        private void ReadIncommingCommand(PacketHeader header, Connection connection, string message)
        {
            Console.WriteLine("\nA message was received from " + connection.ToString() + " which said '" + message + "'.");
            GenerateDataFromRequest(message);
            GetCommandStatus();
            RunCommand(connection, message);
        }

        private void GenerateDataFromRequest(string message)
        {
            commandRecognizer = new CommandRecognizer(sqlConnection);
            commandRecognizer.GenerateDataFromRequest(message.Split(';'));
        }

        private void GetCommandStatus()
        {
            serverStatus = commandRecognizer.serverStatus;
            serverDataStatus = commandRecognizer.serverDataStatus;
        }

        private void RunCommand(Connection connection, string message)
        {
            switch (serverStatus)
            {
                case ServerStatus.SEND_DB_INFO:
                    {
                        GetTablesToSend();
                        toSendObject = ConvertToJSONStringTablesForSending(tablesToSend);
                        connection.SendObject("Message", toSendObject);
                        Console.WriteLine("File was sended");
                        break;
                    }
                case ServerStatus.SEND_FILE:
                    {
                        SendFile(connection, commandRecognizer.fileToSend);
                        break;
                    }
                case ServerStatus.NOTHING_TO_SEND:
                    {
                        ReadServerDataStatus();
                        connection.SendObject("Message", toSendObject);
                        break;
                    }
                case ServerStatus.WRONG_COMMAND:
                    {
                        toSendObject = "Wrong command";
                        connection.SendObject("Message", toSendObject);
                        break;
                    }
            }
        }

        private void GetTablesToSend()
        {
            tablesToSend = new List<DataTable>();
            tablesToSend.AddRange(commandRecognizer.tablesToSend);
        }

        private string ConvertToJSONStringTablesForSending(List<DataTable> tables)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string result = "";
            for (int i = 0; i < tables.Count; i++)
            {
                List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                Dictionary<string, object> row;
                DataTable table = tables[i];
                foreach (DataRow dr in table.Rows)
                {
                    row = new Dictionary<string, object>();
                    foreach (DataColumn col in table.Columns)
                    {
                        row.Add(col.ColumnName, dr[col]);
                    }
                    rows.Add(row);
                }
                result += serializer.Serialize(rows) + "|";
            }
            return result;
        }

        private void ReadServerDataStatus()
        {
            switch (serverDataStatus)
            {
                case ServerDataStatus.EMPTY_RESULT:
                    {
                        toSendObject = "Empty result!";
                        break;
                    }
                case ServerDataStatus.DB_ERROR_IN_DATA:
                    {
                        toSendObject = "Error in some data";
                        break;
                    }
                case ServerDataStatus.INSERT_DATA_FAILED:
                    {
                        toSendObject = "Data wasn't inserted in DataBase";
                        break;
                    }
                case ServerDataStatus.INSERT_DATA_SUCCESSFUL:
                    {
                        toSendObject = "Data was updated!";
                        break;
                    }
                case ServerDataStatus.LOGIN_FAILED:
                    {
                        toSendObject = "Login failed, wrong name or password!";
                        break;
                    }
                case ServerDataStatus.LOGIN_SUCCESSFULL:
                    {
                        break;
                    }
            }
        }

        private void IncomingPartialFileData(PacketHeader header, Connection connection, byte[] data)
        {
            AddFileData(header, connection, data);
            SaveFile();
        }

        private void IncomingPartialFileDataInfo(PacketHeader header, Connection connection, SendInfo info)
        {
            AddFileInfo(header, connection, info);
            SaveFile();
        }

        private void AddFileData(PacketHeader header, Connection connection, byte[] data)
        {
            try
            {
                SendInfo info = null;
                ReceivedFile file = null;

                lock (syncRoot)
                {
                    long sequenceNumber = header.GetOption(PacketHeaderLongItems.PacketSequenceNumber);

                    if (incomingDataInfoCache.ContainsKey(connection.ConnectionInfo) && incomingDataInfoCache[connection.ConnectionInfo].ContainsKey(sequenceNumber))
                    {

                        info = incomingDataInfoCache[connection.ConnectionInfo][sequenceNumber];
                        incomingDataInfoCache[connection.ConnectionInfo].Remove(sequenceNumber);

                        if (!receivedFilesDict.ContainsKey(connection.ConnectionInfo))
                            receivedFilesDict.Add(connection.ConnectionInfo, new Dictionary<string, ReceivedFile>());

                        if (!receivedFilesDict[connection.ConnectionInfo].ContainsKey(info.Filename))
                        {
                            receivedFilesDict[connection.ConnectionInfo].Add(info.Filename, new ReceivedFile(info.Filename, connection.ConnectionInfo, info.TotalBytes));
                            AddNewReceivedItem(receivedFilesDict[connection.ConnectionInfo][info.Filename]);
                        }

                        file = receivedFilesDict[connection.ConnectionInfo][info.Filename];

                        Console.WriteLine("File was received " + info.Filename);
                    }
                    else
                    {
                        if (!incomingDataCache.ContainsKey(connection.ConnectionInfo))
                            incomingDataCache.Add(connection.ConnectionInfo, new Dictionary<long, byte[]>());

                        incomingDataCache[connection.ConnectionInfo].Add(sequenceNumber, data);
                    }
                }

                //If we have everything we need we can add data to the ReceivedFile
                if (info != null && file != null && !file.IsCompleted)
                {
                    file.AddData(info.BytesStart, 0, data.Length, data);
                    Console.WriteLine("Received: " + file.CompletedPercent + "%");
                    //Perform a little clean-up
                    file = null;
                    data = null;
                    GC.Collect();
                }
                else if (info == null ^ file == null)
                    throw new Exception("Either both are null or both are set. Info is " + (info == null ? "null." : "set.") + " File is " + (file == null ? "null." : "set.") + " File is " + (file.IsCompleted ? "completed." : "not completed."));
            }
            catch (Exception ex)
            {
                //If an exception occurs we write to the log window and also create an error file
                LogTools.LogException(ex, "IncomingPartialFileDataError");
            }

        }

        private void AddFileInfo(PacketHeader header, Connection connection, SendInfo info)
        {
            try
            {
                byte[] data = null;
                ReceivedFile file = null;

                lock (syncRoot)
                {
                    long sequenceNumber = info.PacketSequenceNumber;

                    if (incomingDataCache.ContainsKey(connection.ConnectionInfo) && incomingDataCache[connection.ConnectionInfo].ContainsKey(sequenceNumber))
                    {
                        data = incomingDataCache[connection.ConnectionInfo][sequenceNumber];
                        incomingDataCache[connection.ConnectionInfo].Remove(sequenceNumber);

                        if (!receivedFilesDict.ContainsKey(connection.ConnectionInfo))
                            receivedFilesDict.Add(connection.ConnectionInfo, new Dictionary<string, ReceivedFile>());

                        if (!receivedFilesDict[connection.ConnectionInfo].ContainsKey(info.Filename))
                        {
                            receivedFilesDict[connection.ConnectionInfo].Add(info.Filename, new ReceivedFile(info.Filename, connection.ConnectionInfo, info.TotalBytes));
                            AddNewReceivedItem(receivedFilesDict[connection.ConnectionInfo][info.Filename]);
                        }

                        file = receivedFilesDict[connection.ConnectionInfo][info.Filename];
                        Console.WriteLine("Info was received " + info.Filename);
                    }
                    else
                    {
                        if (!incomingDataInfoCache.ContainsKey(connection.ConnectionInfo))
                            incomingDataInfoCache.Add(connection.ConnectionInfo, new Dictionary<long, SendInfo>());

                        incomingDataInfoCache[connection.ConnectionInfo].Add(sequenceNumber, info);
                    }
                }

                if (data != null && file != null && !file.IsCompleted)
                {
                    file.AddData(info.BytesStart, 0, data.Length, data);

                    file = null;
                    data = null;
                    GC.Collect();
                }
                else if (data == null ^ file == null)
                    throw new Exception("Either both are null or both are set. Data is " + (data == null ? "null." : "set.") + " File is " + (file == null ? "null." : "set.") + " File is " + (file.IsCompleted ? "completed." : "not completed."));
            }
            catch (Exception ex)
            {
                LogTools.LogException(ex, "IncomingPartialFileDataInfo");
            }
        }

        private void SaveFile()
        {
            foreach (var k in receivedFiles)
            {
                if (k.IsCompleted)
                {
                    try
                    {
                        k.SaveFileToDisk(standartFileStorageLocation + "\\" + k.Filename);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error was occured: " + e.ToString());
                    }
                }

            }

        }

        private void AddNewReceivedItem(ReceivedFile data)
        {
            receivedFiles.Add(data);
        }

        private void SendFile(Connection connection, string fileName)
        {
            try
            {
                FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                StreamTools.ThreadSafeStream safeStream = new StreamTools.ThreadSafeStream(stream);
                string shortFileName = System.IO.Path.GetFileName(fileName);

                long sendChunkSizeBytes = (long)(stream.Length / 20.0) + 1;
                long maxChunkSizeBytes = 500L * 1024L * 1024L;
                if (sendChunkSizeBytes > maxChunkSizeBytes) sendChunkSizeBytes = maxChunkSizeBytes;

                long totalBytesSent = 0;
                do
                {
                    long bytesToSend = (totalBytesSent + sendChunkSizeBytes < stream.Length ? sendChunkSizeBytes : stream.Length - totalBytesSent);
                    StreamTools.StreamSendWrapper streamWrapper = new StreamTools.StreamSendWrapper(safeStream, totalBytesSent, bytesToSend);
                    long packetSequenceNumber;

                    connection.SendObject("PartialFileData", streamWrapper, customOptions, out packetSequenceNumber);
                    connection.SendObject("PartialFileDataInfo", new SendInfo(shortFileName, stream.Length, totalBytesSent, packetSequenceNumber), customOptions);

                    totalBytesSent += bytesToSend;

                } while (totalBytesSent < stream.Length);

                GC.Collect();

            }
            catch (CommunicationException)
            {

            }
            catch (Exception ex)
            {

            }
        }

        public void WaitForClosing()
        {
            if (Console.ReadKey(true).Key == ConsoleKey.C) CloseServer();
        }

        private void CloseServer()
        {
            sqlConnection.Close();
            NetworkComms.Shutdown();
        }

    }

}
