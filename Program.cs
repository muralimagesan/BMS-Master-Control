using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DEV = CH.Regatron.HPPS.Device;
using UI = CH.Regatron.HPPS.Device.TopCon.ControlInterface;
using System.Threading.Tasks;

using TopConAPITest;
namespace BMS_Master_Control
{
    class Program
    {
        static void Main(string[] args)
        {
            //Code for opening com port and receiving data
            SerialPort BMSSerialPort = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);
            //SerialPort RegatronSerialPort = new SerialPort("COM8", 9600, Parity.None, 8, StopBits.One);

            int[] voltage = new int[] { 0, 0, 0, 0 };
            int[] temp = new int[] { 0, 0, 0, 0 };
            int[] SOC = new int[] { 0, 0, 0, 0 };
            int current = 0;
            float[] f_voltage = new float[] { 0, 0, 0, 0 };
            float[] f_temp = new float[] { 0, 0, 0, 0 };
            float[] f_SOC = new float[] { 0, 0, 0, 0 };
            float f_current = 0;
            float fake_voltage = 10;
            float fake_current = 2;
            float fake_power = (float)0.02;

            char[] delimiter = new char[] { ' ', '\n' };

            Console.WriteLine("For charging please press the character 'c' then Enter. For discharging please press Enter");
            string c_d = Console.ReadLine();
            bool isCharging;

            if(c_d == "c")
            {
                isCharging = true;
            }
            else
            {
                isCharging = false;
            }

            Console.WriteLine(isCharging);
            Console.ReadKey();

            while (true)
            {

                float voltVal = 0;

                BMSSerialPort.Open();
                Console.WriteLine("--------Opened BMS Serial Port--------");
                string toParse = BMSSerialPort.ReadLine();
                while (toParse.Length < 50)
                {
                    BMSSerialPort.DiscardInBuffer();
                    Console.WriteLine("sup");
                    toParse = BMSSerialPort.ReadLine();
                }
                //string toParse = "V1 36700 28200 8650 V2 37000 30300 8820 V6 36800 29800 8790 V7 37000 31300 9050 I 15000 \n";
                string[] words = toParse.Split(delimiter);
                Console.WriteLine(toParse);

                splitToArrays(ref words, ref voltage, ref temp, ref SOC, ref current);
                arrayConversion(ref f_voltage, ref f_temp, ref f_SOC, ref f_current, ref voltage, ref temp, ref SOC, ref current);

                BMSSerialPort.Close();
                Console.WriteLine(f_voltage[0] + " " + f_voltage[1] + " " +  f_voltage[2] + " " + f_voltage[3]);

                if (isCharging)
                {
                    voltVal = f_voltage.Max();
                }
                else
                {
                    voltVal = f_voltage.Min();
                }
                


                /*Console.Write(f_temp[0] + " ");
                Console.Write(f_SOC[0] + " ");
                Console.WriteLine(f_current);*/

                Console.WriteLine("--------Closed BMS Serial Port--------");



                new TC_Update(ref voltVal, ref fake_current, ref fake_power, ref isCharging);
                //fake_voltage++;
                //fake_current++;
                //fake_power++;
            }

        }

        static void splitToArrays(ref string[] words, ref int[] v, ref int[] t, ref int[] soc, ref int cur)
        {
            int arrayCounter = 0;

            for (int i = 1; i < (words.Length - 3); i = i + 4)
            {
                //Arrays of unedited voltages, temps and SOCs from received dataPacket.
                v[arrayCounter] = Convert.ToInt32(words[i]);
                t[arrayCounter] = Convert.ToInt32(words[i + 1]);
                soc[arrayCounter] = Convert.ToInt32(words[i + 2]);
                arrayCounter++;
            }
            //Unedited received current value
            cur = Convert.ToInt32(words[words.Length - 2]);

        }

        static void arrayConversion(ref float[] f_v, ref float[] f_t, ref float[] f_soc, ref float f_cur, ref int[] v, ref int[] t, ref int[] soc, ref int cur)
        {
            //Conversion to float of actual voltages, temps, SOCS and Current.
            f_v = Array.ConvertAll(v, x => (float)x / 10000);
            f_t = Array.ConvertAll(t, x => (float)x / 100);
            f_soc = Array.ConvertAll(soc, x => (float)x / 100);
            f_cur = ((float)cur) / (float)1000.0;
        }
    }
}

namespace TopConAPITest
{
    class TC_Update
    {
        //-- this is the 'main' functionality: creating a TopCon object, connecting and asking for information
        public TC_Update(ref float ref_voltage, ref float ref_current, ref float ref_power, ref bool isCharging)
        {
            //-- create a TopCon object (use as DEVICE_1 of three)
            DEV.TopCon _myTc = new DEV.TopCon(
                new DEV.TopConConfiguration_Dummy(),
                TopCon.Broker.Device.DEVICE_1);
            //-- ask for COMport to be used
            try
            {
                //Console.Write(" Connect to TopCon on which COMPort? (try 1 if unsure) : ");
                //int comportNumber = int.Parse(Console.ReadLine());
                int comportNumber = 8;
                Console.Write(" TopCon object created \n Now trying to connect to TopCon on COM [" + comportNumber + "] : ");
                //-- connect to the TopCon
                _myTc.Connect(comportNumber);
                Console.Write(" - connected; \n\n Now trying to fetch control interface to RS232: ");
                //-- now grab the control to the RS232 interface CHECK PAGE15/66
                _myTc.SetControlInterface(UI.Rs232);
                Console.WriteLine(" - done -->> if HMI available: [Remote] LED is lit \n\n Now trying to read some information from the TopCon device :");
                //-- Read serial number and present that to the user
                Console.WriteLine("\n+ Serial number of device = [" + _myTc.GetSerialNumberOfDevice() + "]");
                //-- Read state from TopCon and show as number and as text
                Console.WriteLine("+ TopCon status = [" + _myTc.GetSystemState() + "/" + _myTc.GetSystemStateAsString() + "]");

                //-- update TopCon configuration (needed 1x)
                _myTc.UpdateTopConConfigurationWithTopConData();

                switch (isCharging)
                {
                    case true:
                        if(ref_voltage > 4 && ref_voltage < 4.18)
                        {
                            _myTc.SetReferenceCurrent(0.5 * _myTc.GetReferenceCurrent());
                        }
                        if(ref_voltage > 4.18)
                        {
                            _myTc.SetReferenceCurrent(0);
                            Console.WriteLine("Cells Fully Charged, Press any key to exit");
                            Console.ReadKey();
                            Environment.Exit(0);

                        }
                        break;

                    case false:
                        if (ref_voltage < 3.0 && ref_voltage > 2.8)
                        {
                            _myTc.SetReferenceCurrent(0.5 * _myTc.GetReferenceCurrent());
                        }
                        if (ref_voltage < 2.72)
                        {
                            _myTc.SetReferenceCurrent(0);
                            Console.WriteLine("Cells Discharged, Press any key to exit");
                            Console.ReadKey();
                            Environment.Exit(0);
                        }
                        break;


                }

                //Set the TopCon Power Supply Limits. Values in Volts, Amps and kiloWatts respectively
                //_myTc.SetReferenceVoltage(ref_voltage);
                //_myTc.SetReferenceCurrent(ref_current);
                //_myTc.SetReferencePower(ref_power);

                _myTc.SetReferenceVoltage(30);  //Should be 20
                _myTc.SetReferenceCurrent(1);   //Should be 7.5
                _myTc.SetReferencePower(0.05);

                //Set the Sink Limits (Q4). Values in Volts, Amps and kiloWatts respectively
                _myTc.SetLimitVoltageQ4(20);
                _myTc.SetLimitCurrentQ4(-2);
                _myTc.SetLimitPowerQ4(-0.15);

                //DEV.TopConConfiguration myTCconfig = new TopConConfiguration_DummY();

                
                _myTc.SetPowerON();
                Console.WriteLine("\n TopCon status is : [{0:D}]]", _myTc.GetSystemState());
                Console.WriteLine(" TopCon is in RUN? " + _myTc.IsInRunState());
                Console.WriteLine(" TopCon is in Ready?" + _myTc.IsInReadyState());
                Console.WriteLine("\nActual values: ");
                Console.ReadKey();
                Console.WriteLine("+ U: [" + _myTc.GetActualVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetActualCurrent() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetActualPower() + " kW]");

                _myTc.SetPowerOFF();
                Console.WriteLine("\n TopCon status is : [{0:D}]]", _myTc.GetSystemState());
                Console.WriteLine(" TopCon is in RUN? " + _myTc.IsInRunState());
                Console.WriteLine(" TopCon is in Ready?" + _myTc.IsInReadyState());
                Console.ReadKey();

                //-- Read reference values for UIP and present them
                Console.WriteLine("Reference values: ");
                Console.WriteLine("+ U: [" + _myTc.GetReferenceVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetReferenceCurrent() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetReferencePower() + " kW]");
                
                //--Read Sink values for UIP and present them
                Console.WriteLine("\nSink Values (Q4): ");
                Console.WriteLine("+ U: [" + _myTc.GetLimitVoltageQ4() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetLimitCurrentQ4() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetLimitPowerQ4() + " kW]");
                
                //-- Read actual values from UIP and present them
                Console.WriteLine("\nActual values: ");
                Console.WriteLine("+ U: [" + _myTc.GetActualVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetActualCurrent() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetActualPower() + " kW]");

                Console.WriteLine(_myTc.GetTopConConfig().MaximumSystemVoltage);
                
                Console.WriteLine(_myTc.GetTopConConfig().MaximumSystemCurrent);
                Console.WriteLine(_myTc.GetTopConConfig().MaximumSystemPower);

                //-- housekeeping
                _myTc.Disconnect();

                Console.WriteLine("\n now disconnected! ");
            }
            catch (Exception e) //-- only in case something failed!
            {
                Console.WriteLine(" An error occurred: [" + e.Message + "] \n-->> ending program.");
            }
            Console.WriteLine(" \n Press enter to leave: ");
            Console.ReadLine();
        } //-- of constructor & functionality.
    } //-- of class
} //-- of using
