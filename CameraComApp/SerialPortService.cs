using System;
using System.IO.Ports;

namespace CameraComApp
{
    public class SerialPortService
    {
        private SerialPort _serialPort;
        public event EventHandler<string> CommandReceived;

        public void OpenPort(string portName)
        {
            ClosePort();
            try
            {
                _serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening COM port: {ex.Message}");
            }
        }

        public void ClosePort()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        public void SendData(string data)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.WriteLine(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending data: {ex.Message}");
                }
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadLine().Trim();
                CommandReceived?.Invoke(this, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading COM port: {ex.Message}");
            }
        }
    }
}