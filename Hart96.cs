using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeDrinkotechSever
{
    public class Hart96{
        private static Hart96 instance;
        
        static bool _continue;
        static SerialPort _serialPort;
        private const int K_DIV_SIGN = 2-1;
        private const int K_START_TELEG = 3-1;
        private const int K_PLUS_CHAR = 11-1;
        private const int K_END_CHAR = 17-1;
        private const int K_CHECKSUM = 18-1;

        private Hart96()
        {
            _serialPort = new SerialPort();
        }

        public static Hart96 Instance
        {
            get{
                if(instance == null){

                    instance = new Hart96();
                }
                return instance;
            }

        }

        /// <summary>
        /// Return is if port opened
        /// </summary>
        public bool IsOpened {
            get { return _serialPort.IsOpen; }
        } 

        /// <summary>
        /// Open com port
        /// </summary>
        /// <param name="port">port</param>
        /// <returns>empty on success else return error string</returns>
        public string OpenComPort(string port)
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.PortName = "port"; 
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

                    return string.Empty;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
            else
            {
                return "port already opened";
            }

        }

        /// <summary>
        /// Close com port
        /// </summary>
        /// <param name="port">port</param>
        /// <returns>empty on success else return error string</returns>
        public string CloseComPort(string port)
        {
            if (_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Close();

                    return string.Empty;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
            else
            {
                return "port already closed";
            }
        }

        public void RequestToSend()
        {

        }

    }
}
