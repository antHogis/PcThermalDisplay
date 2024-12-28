using System;
using System.IO.Ports;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace WindowsThermalSerialSender
{
    enum HardwareSerialIdentifier : ushort
    {
        CPU = 0,
        GPU = 1,
    }
    class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }


    class HardwareInfoRetriever
    {
        private readonly Computer computer;
        private (int, int)? cpuTempIndex;
        private (int, int)? gpuTempIndex;

        public HardwareInfoRetriever()
        {

            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
            };

            computer.Open();

            FindIndices();
        }

        public float? GetCpuTemp()
        {
            if (cpuTempIndex == null)
            {
                return null;
            }

            return FindWithIndex(cpuTempIndex.Value);
        }


        public float? GetGpuTemp()
        {
            if (gpuTempIndex == null)
            {
                return null;
            }

            return FindWithIndex(gpuTempIndex.Value);
        }

        private float? FindWithIndex((int, int) index)
        {
            computer.Accept(new UpdateVisitor());

            int hardwareIndex = index.Item1;
            int sensorIndex = index.Item2;

            var hardware = computer.Hardware[hardwareIndex];
            var sensor = hardware.Sensors[sensorIndex];

            return sensor.Value;
        }

        private void FindIndices()
        {
            computer.Accept(new UpdateVisitor());

            for (int hardwareIndex = 0; hardwareIndex < computer.Hardware.Count; hardwareIndex++)
            {
                var hardware = computer.Hardware[hardwareIndex];

                for (int sensorIndex = 0; sensorIndex < hardware.Sensors.Length; sensorIndex++)
                {
                    var sensor = hardware.Sensors[sensorIndex];

                    if (hardware.HardwareType == HardwareType.Cpu && sensor.SensorType == SensorType.Temperature)
                    {
                        cpuTempIndex = (hardwareIndex, sensorIndex);

                    }
                    else if (hardware.HardwareType == HardwareType.GpuNvidia && sensor.SensorType == SensorType.Temperature)
                    {
                        gpuTempIndex = (hardwareIndex, sensorIndex);

                    }
                }
            }
        }
    }

    class Sender
    {
        private readonly SerialPort serialPort;

        public Sender(string portName)
        {
            serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            Console.WriteLine("Opening port", serialPort.PortName);
            serialPort.Open();
        }

        public void Run()
        {
            Console.WriteLine("Opening port", serialPort.PortName);
            serialPort.Open();

            Random rand = new Random();

            while (true)
            {
                string data1 = rand.Next(1, 999).ToString().PadLeft(3, '0');
                string data2 = rand.Next(1, 999).ToString().PadLeft(3, '0');

                SendTemp(HardwareSerialIdentifier.CPU, data1);
                SendTemp(HardwareSerialIdentifier.GPU, data2);
                Thread.Sleep(1000);
            }
        }

        public void SendData(HardwareInfoRetriever retriever)
        {
            var cpuTemp = retriever.GetCpuTemp()?.ToString("00.0");
            var gpuTemp = retriever.GetGpuTemp()?.ToString("00.0");

            if (cpuTemp != null)
            {

                SendTemp(HardwareSerialIdentifier.CPU, cpuTemp);
            }
            if (gpuTemp != null)
            {

            SendTemp(HardwareSerialIdentifier.GPU, gpuTemp);
            }
        }

        private void SendTemp(HardwareSerialIdentifier type, string temp)
        {
            string data = ((int)type) + temp.Replace(".", "");
            Console.WriteLine("Sending data " + data);
            serialPort.Write(data + '\n');
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var retriever = new HardwareInfoRetriever();
                var sender = new Sender("COM3");

                while (true)
                {
                    sender.SendData(retriever);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error : " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Restarting in 5 seconds");
                Thread.Sleep(5000);
                Main(args);
            }
        }
    }
}
