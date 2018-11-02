using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace VibeDrinkotechSever
{
    public class HartNineSix
    {
        private static HartNineSix instance;

        public Log Logger { get; set; }
        public string SpoolPath { get; set; } // set from config spool
        public int WaitTime { get; set; } // set from config "timer"
        public bool IsDebug { get; set; } // set from config "isDebug"
        bool _continue;
        SerialPort _serialPort;
        private const int K_DIV_SIGN = 1;
        private const int K_START_TELEG = 2;
        private const int K_PLUS_CHAR = 10;
        private const int K_END_CHAR = 15;
        private const int K_CHECKSUM = 16;

        private HartNineSix()
        {
            _serialPort = new SerialPort();
        }

        public static HartNineSix Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HartNineSix();
                }

                return instance;
            }
        }

        /// <summary>
        /// Return is if port opened
        /// </summary>
        public bool IsOpened
        {
            get { return _serialPort.IsOpen; }
        }

        /// <summary>
        /// Open com port
        /// </summary>
        /// <param name="port">port</param>
        /// <returns>true on success</returns>
        public bool OpenComPort(string port)
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.PortName = port;
                _serialPort.BaudRate = 9600;
                _serialPort.Parity = Parity.Odd;
                _serialPort.DataBits = 7;
                _serialPort.StopBits = StopBits.One;

                // Set the read/write timeouts  
                _serialPort.ReadTimeout = 5000;
                _serialPort.WriteTimeout = 5000;

                try
                {
                    _serialPort.Open();
                    Logger("info::serial", "Serial port opened");

                    return true;
                }
                catch (Exception e)
                {
                    Logger("error::serial", e.Message.FormatErrorForLog());
                    return false;
                }
            }
            else
            {
                Logger("error::serial", "Serial port already opened");
                return false;
            }
        }

        /// <summary>
        /// Close com port
        /// </summary>
        /// <returns>true on success</returns>
        public bool CloseComPort()
        {
            if (_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Close();
                    Logger("info::serial", "Serial port closed");

                    return true;
                }
                catch (Exception e)
                {
                    Logger("error::serial", e.Message.FormatErrorForLog());
                    return false;
                }
            }
            else
            {
                Logger("error::serial", "Serial port already closed");
                return false;
            }
        }

        public void RequestToSend()
        {
            if (IsDebug) Logger("info::link", "Begin request to send message '/'");

            if (_serialPort.IsOpen)
            {
                var file = GetNextFile();
                if (file != null)
                {
                    if (IsDebug) Logger("info::file", "File found on spool");

                    if (Alive())
                    {
                        if (IsDebug) Logger("info::link", "System respond ACK to ENQ");
                    }
                    else
                    {
                        if (IsDebug) Logger("info::link", "Bad response to ENQ");
                    }

                    int server3 = 0;
                    int server2 = 0;
                    int server1 = 0;
                    int plu5 = 0;
                    int plu4 = 0;
                    int plu3 = 0;
                    int plu2 = 0;
                    int plu1 = 0;
                    int count4 = 0;
                    int count3 = 0;
                    int count2 = 0;
                    int count1 = 0;

                    string keyValue = null;
                    List<string> KeyList = new List<string>();
                    using (StreamReader sr = new StreamReader(file.FullName))
                    {
                        string inp;
                        while ((inp = sr.ReadLine()) != null)
                        {
                            // Console.WriteLine(inp);
                            string[] parts = inp.Split(new char[] {',', '='});
                            if ((parts.Length == 2))
                            {
                                if (parts[0].Trim() == "server3")
                                {
                                    server3 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "server2")
                                {
                                    server2 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "server1")
                                {
                                    server1 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "plu5")
                                {
                                    plu5 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "plu4")
                                {
                                    plu4 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "plu3")
                                {
                                    plu3 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "plu2")
                                {
                                    plu2 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "plu1")
                                {
                                    plu1 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "count4")
                                {
                                    count4 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "count3")
                                {
                                    count3 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "count2")
                                {
                                    count2 = Byte.Parse(parts[1].Trim());
                                }
                                else if (parts[0].Trim() == "count1")
                                {
                                    count1 = Byte.Parse(parts[1].Trim());
                                }
                            }
                        }
                    }

                    string infoProduct = "server=" + server3.ToString() + server2.ToString() + server1.ToString() +
                                         ", " +
                                         "plu=" + plu5.ToString() + plu4.ToString() + plu3.ToString() +
                                         plu2.ToString() + plu1.ToString() + ", " +
                                         "count=" + count4.ToString() + count3.ToString() + count2.ToString() +
                                         count1.ToString();
                    if (IsDebug)
                    {
                        Logger("file::read", infoProduct);
                    }


                    var initByte = new byte[]
                    {
                        0x2, 0x2F, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x2B, 0x30, 0x30, 0x30, 0x30,
                        0x3, 0x0
                    };

                    initByte[K_START_TELEG] = (byte) ((int) initByte[K_START_TELEG] + server3);
                    initByte[K_START_TELEG + 1] = (byte) ((int) initByte[K_START_TELEG + 1] + server2);
                    initByte[K_START_TELEG + 2] = (byte) ((int) initByte[K_START_TELEG + 2] + server1);
                    initByte[K_START_TELEG + 3] = (byte) ((int) initByte[K_START_TELEG + 3] + plu5);
                    initByte[K_START_TELEG + 4] = (byte) ((int) initByte[K_START_TELEG + 4] + plu4);
                    initByte[K_START_TELEG + 5] = (byte) ((int) initByte[K_START_TELEG + 5] + plu3);
                    initByte[K_START_TELEG + 6] = (byte) ((int) initByte[K_START_TELEG + 6] + plu2);
                    initByte[K_START_TELEG + 7] = (byte) ((int) initByte[K_START_TELEG + 7] + plu1);
                    initByte[K_PLUS_CHAR + 1] = (byte) ((int) initByte[K_PLUS_CHAR + 1] + count4);
                    initByte[K_PLUS_CHAR + 2] = (byte) ((int) initByte[K_PLUS_CHAR + 2] + count3);
                    initByte[K_PLUS_CHAR + 3] = (byte) ((int) initByte[K_PLUS_CHAR + 3] + count2);
                    initByte[K_PLUS_CHAR + 4] = (byte) ((int) initByte[K_PLUS_CHAR + 4] + count1);

                    initByte[K_CHECKSUM] = ComputeAdditionChecksum(initByte);
                    
                    try
                    {
                        _serialPort.Write(initByte, 0, sizeof(byte) * initByte.Length);
                    }
                    catch (Exception e)
                    {
                        Logger("error::serial", e.Message.FormatErrorForLog());
                        return;
                    }

                    Wait();

                    var readByte = new byte[]
                    {
                        0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
                        0x0
                    };

                    try
                    {
                        _serialPort.Read(readByte, 0, readByte.Length);
                    }
                    catch (Exception e)
                    {
                        Logger("error::serial", e.Message.FormatErrorForLog());
                        return;
                    }

                    if (readByte[0] != 6 && readByte[0] != 0)
                    {
                        if (IsDebug) Logger("info::link", "Bad response, retry next time");
                    }
                    else
                    {
                        Logger("info::link", "Product Command success: " + infoProduct);

                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch (Exception e)
                        {
                            Logger("error::file", "Could not delete " + file.FullName + ", " + e.Message.FormatErrorForLog());
                            Wait();
                            try
                            {
                                File.Delete(file.FullName);
                            }
                            catch (Exception f)
                            {
                                Logger("error::file", "Could not delete " + file.FullName + ", " + e.Message.FormatErrorForLog());
                                Wait();
                                File.Delete(file.FullName);
                            }

                        }
                    }
                }
                else
                {
                    if (Alive())
                    {
                        if (IsDebug) Logger("info::link", "System respond ACK to ENQ");
                    }
                    else
                    {
                        if (IsDebug) Logger("info::link", "Bad response to ENQ");
                    }
                }
            }
            else
            {
                Logger("error::serial", "Serial port closed");
            }
        }

        public void RequestToReceive()
        {
            if (IsDebug) Logger("info::link", "Begin request to receive message 'l'");
        }

        private bool Alive()
        {
            var initByte = new byte[] {0x5};
            
            try
            {
                _serialPort.Write(initByte, 0, initByte.Length);
            }
            catch (Exception e)
            {
                Logger("error::serial", e.Message.FormatErrorForLog());
                return false;
            }

            Wait();

            var readByte = new byte[]
                {0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0};

            try
            {
                _serialPort.Read(readByte, 0, readByte.Length);
            }
            catch (Exception e)
            {
                Logger("error::serial", e.Message.FormatErrorForLog());
                return false;
            }


            Wait();

            if (readByte[0] != 6 && readByte[0] != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Wait()
        {
            System.Threading.Thread.Sleep(this.WaitTime);
        }

        private byte ComputeAdditionChecksum(byte[] data)
        {
            byte sum = 0;
            unchecked // Let overflow occur without exceptions
            {
                for (var index = 1; index <= K_END_CHAR; index++)
                {
                    byte b = data[index];
                    sum ^= b;
                }
            }

            return sum;
        }

        public FileInfo GetNextFile()
        {
            string[] filePaths = Directory.GetFiles(this.SpoolPath, "*.fps");

            for (int i = 0; i < filePaths.Length; i++)
            {
                string fileName = filePaths[i];
                var destination = Path.Combine(this.SpoolPath, fileName);
                FileInfo file = new FileInfo(destination);

                if (file.Name.StartsWith("fp_"))
                {
                    return file;
                }
            }

            return null;
        }
    }
}