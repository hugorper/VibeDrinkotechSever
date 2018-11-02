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
                        return;
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

                    initByte[K_CHECKSUM] = ComputeCreditAdditionChecksum(initByte);
                    
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

            if (_serialPort.IsOpen)
            {
                
                if (Alive())
                {
                    if (IsDebug) Logger("info::link", "System respond ACK to ENQ");
                }
                else
                {
                    if (IsDebug) Logger("info::link", "Bad response to ENQ");
                    return;
                }

                // 0x6f is BCC
                var initByte = new byte[]
                {
                    0x2, 0x6c, 0x3, 0x6f
                };

                // ask if something to send from drinkotech
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
                    0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 
                    0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 
                    0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 
                };

                if (IsDebug) Logger("info::link", "Check if product available");

                try
                {
                    _serialPort.Read(readByte, 0, readByte.Length);
                }
                catch (Exception e)
                {
                    Logger("error::serial", e.Message.FormatErrorForLog());
                    return;
                }


                //  Reply if no beverage has been dispensed(empty message)
                //    STX = 1 byte(02)H
                //    ETX = 1 byte(03)H
                //    BCC = 1 byte
                if (readByte[0] == 2 && readByte[1] == 3 && readByte[2] == 3)       // nothing to read
                {
                    if (IsDebug) Logger("info::link", "Nothing to read");
                }
                else if (readByte[0] == 2 && readByte[27] == 3)
                {
                    //Reply if a beverage has been dispensed :
                    //  STX =               1 byte (02)H
                    //  waiter number =     3 bytes ( '001' to '495 )
                    //  table number =      5 bytes ( '00001' to '09999' )
                    //  PLU number =        5 bytes ( '00001' to '09999' )
                    //  Qty =               5 bytes ( '00000' to '65535' )
                    //  sign =              1 byte (2B)H, (2D)H or (2A)H
                    //  Qty_CD =            5 bytes ( '00001' to '65535' )
                    //  Location number =   2 bytes ( '00' to '15' )
                    //  ETX =               1 byte (03)H
                    //  BCC =               1 byte

                    Wait();

                    // check checksum
                    var bcc = ComputeDebitAdditionChecksum(readByte);
                    if (bcc == readByte[28])
                    {
                        int waiter = Int16.Parse($"{ATS(readByte[1])}{ATS(readByte[2])}{ATS(readByte[3])}");
                        int table = Int16.Parse($"{ATS(readByte[4])}{ATS(readByte[5])}{ATS(readByte[6])}{ATS(readByte[7])}{ATS(readByte[8])}");
                        int plu = Int16.Parse($"{ATS(readByte[9])}{ATS(readByte[10])}{ATS(readByte[11])}{ATS(readByte[12])}{ATS(readByte[13])}");
                        int qty = Int16.Parse($"{ATS(readByte[14])}{ATS(readByte[15])}{ATS(readByte[16])}{ATS(readByte[17])}{ATS(readByte[18])}");
                        string sign = "";
                        if (readByte[19] == 0x2b) // +
                        {
                            sign = "plus";
                        }
                        else if (readByte[19] == 0x2d) // -
                        {
                            sign = "minus";

                        }
                        else if (readByte[19] == 0x2A) // *
                        {
                            sign = "multiply";

                        }
                        int qtyCD = Int16.Parse($"{ATS(readByte[20])}{ATS(readByte[21])}{ATS(readByte[22])}{ATS(readByte[23])}{ATS(readByte[24])}");
                        int location = Int16.Parse($"{ATS(readByte[25])}{ATS(readByte[26])}");

                        if (!WriteDebitFile(waiter, table, plu, qty, sign, qtyCD, location))
                        {
                            Logger("info::link", "Send NAK");
                            // checksum fail
                            var errorByte = new byte[] {0x15}; // NAK
            
                            try
                            {
                                _serialPort.Write(initByte, 0, initByte.Length);
                            }
                            catch (Exception e)
                            {
                                Logger("error::serial", e.Message.FormatErrorForLog());
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (IsDebug) Logger("info::link", "Checksum fail return NAK");
                        // checksum fail
                        var errorByte = new byte[] {0x15}; // NAK
            
                        try
                        {
                            _serialPort.Write(initByte, 0, initByte.Length);
                        }
                        catch (Exception e)
                        {
                            Logger("error::serial", e.Message.FormatErrorForLog());
                            return;
                        }
                    }
                }
            }
        }

        // Ascii to string
        private string ATS(byte value)
        {
            return (value - 0x30).ToString(); // 30 is 0 ascii
        } 

        private bool WriteDebitFile(int waiter, int table, int plu, int qty, string sign, int qtyCD, int location)
        {
            string fileName = Path.Combine(this.SpoolPath, $"debit_{waiter}_{table}_{plu}_{qty}_{sign}_{qtyCD}_{location}_{Guid.NewGuid().ToString()}.debit");
            try
            {
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.Write(fileName);         
                    sw.Close();
                }

                return true;
            }
            catch (Exception e)
            {
                Logger("info::file", "Could not create output file, send NAK");
                return false;
            }
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

        private byte ComputeCreditAdditionChecksum(byte[] data)
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

        private byte ComputeDebitAdditionChecksum(byte[] data)
        {
            byte sum = 0;
            unchecked // Let overflow occur without exceptions
            {
                for (var index = 1; index <= 27; index++)
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