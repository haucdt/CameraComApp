using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using System.Threading.Tasks;

namespace CameraComApp
{
    public class CameraService
    {
        private VideoCaptureDevice _videoDevice;
        private readonly FilterInfoCollection _videoDevices;
        private readonly QrCodeReader _qrReader;
        private Timer _timeoutTimer;
        private bool _isReadingQr;
        private DateTime _lastFrameProcessed = DateTime.MinValue;
        private readonly double _minFrameIntervalMs = 100; // Process frames at ~10 FPS max

        public event EventHandler<Bitmap> FrameReceived;
        public event EventHandler<string> QrCodeDetected;
     //   public event EventHandler<string> CameraStatusChanged;

        public CameraService()
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _qrReader = new QrCodeReader();
        }

        public List<string> GetAvailableCameras()
        {
            List<string> cameras = new List<string>();
            foreach (FilterInfo device in _videoDevices)
            {
                cameras.Add(device.Name);
            }
            return cameras;
        }

        public void StartCamera(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _videoDevices.Count)
                return;

            StopCamera();
            _videoDevice = new VideoCaptureDevice(_videoDevices[deviceIndex].MonikerString);
            _videoDevice.NewFrame += VideoDevice_NewFrame;
       //     CameraStatusChanged?.Invoke(this, $"Camera started: {_videoDevices[deviceIndex].Name}");
            _videoDevice.Start();

        }

        public void StopCamera()
        {
            if (_videoDevice != null && _videoDevice.IsRunning)
            {
                _videoDevice.SignalToStop();
                _videoDevice.NewFrame -= VideoDevice_NewFrame;
                _videoDevice = null;
            }
            StopQrCodeReading();
        }

        public bool IsCameraRunning => _videoDevice != null && _videoDevice.IsRunning;

        private void VideoDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Limit frame processing rate
            if ((DateTime.Now - _lastFrameProcessed).TotalMilliseconds < _minFrameIntervalMs)
                return;

            _lastFrameProcessed = DateTime.Now;
            var frame = (Bitmap)eventArgs.Frame.Clone();
            FrameReceived?.Invoke(this, frame);

            if (_isReadingQr)
            {
                // Process QR code in a separate task to avoid blocking
                Task.Run(() =>
                {
                    string qrCode = _qrReader.ReadQrCode(frame);
                    if (!string.IsNullOrEmpty(qrCode))
                    {
                        QrCodeDetected?.Invoke(this, qrCode);
                        qrCode ="";
                        _isReadingQr = false;
                        StopQrCodeReading();
                    }
                    frame.Dispose();
                });
            }
            else
            {
                frame.Dispose();
            }
        }

        public void StartQrCodeReading(int timeoutMs, Action timeoutCallback)
        {
            _isReadingQr = true;
            _timeoutTimer = new Timer(timeoutMs);
            _timeoutTimer.Elapsed += (s, e) =>
            {
                if (_isReadingQr)
                {
                    timeoutCallback?.Invoke();
                    StopQrCodeReading();
                }
            };
            _timeoutTimer.AutoReset = false;
            _timeoutTimer.Start();
        }

        private void StopQrCodeReading()
        {
            _isReadingQr = false;
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Stop();
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }
        }
    }
}