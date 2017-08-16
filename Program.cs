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
            float fake_voltage = 5;
            float fake_current = 5;
            float fake_power = 7;

            char[] delimiter = new char[] { ' ', '\n' };


            while (true)
            {

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
                Console.Write(f_temp[0] + " ");
                Console.Write(f_SOC[0] + " ");
                Console.WriteLine(f_current);

                Console.WriteLine("--------Closed BMS Serial Port--------");



                new TCAPITest(ref fake_voltage, ref fake_current, ref fake_power);
                fake_voltage++;
                fake_current++;
                fake_power++;
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
    class TCAPITest
    {
        //-- this is the 'main' functionality: creating a TopCon object, connecting and asking for information
        public TCAPITest(ref float ref_voltage, ref float ref_current, ref float ref_power)
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
                //Console.WriteLine(" - done -->> if HMI available: [Remote] LED is lit \n\n Now trying to read some information from the TopCon device :");
                Console.WriteLine("Now trying to read some information from the TopCon device :");
                //-- Read serial number and present that to the user
                Console.WriteLine("\n+ Serial number of device = [" + _myTc.GetSerialNumberOfDevice() + "]");
                //-- Read state from TopCon and show as number and as text
                Console.WriteLine("+ TopCon status = [" + _myTc.GetSystemState() + "/" + _myTc.GetSystemStateAsString() + "]");

                //-- update TopCon configuration (needed 1x)
                _myTc.UpdateTopConConfigurationWithTopConData();

                //Set reference values for UIP
                _myTc.SetReferenceVoltage(ref_voltage);
                _myTc.SetReferenceCurrent(ref_current);
                _myTc.SetReferencePower(ref_power);
                                
                //_myTc.MaximumSystemVoltage; //-- upper voltage limit (commonly: nominal voltage)
                /*myTConfig.MiminumSystemVoltage; //-- lower voltage limit (commonly: 0V)
                myTCconfig.MaximumSystemCurrent; //-- upper current limit (commonly: nominal current)
                myTCconfig.MiminumSystemCurrent; //-- lower current limit
                myTCconfig.MaximumSystemPower; //-- upper power limit (commonly: nominal power)
                myTCconfig.MiminumSystemPower; //-- lower power limit
                myTCconfig.MaximumSystemResistance; //-- upper resistance limit (commonly: 12 Ohms)
                myTCconfig.MiminumSystemResistance; //-- lower resistance limit*/

                //-- Read reference values for UIP and present them
                Console.WriteLine("Reference values: ");
                Console.WriteLine("+ U: [" + _myTc.GetReferenceVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetReferenceCurrent() + " V]");
                Console.WriteLine("+ P: [" + _myTc.GetReferencePower() + " V]");
                //-- Read actual values from UIP and present them
                Console.WriteLine("\nActual values: ");
                Console.WriteLine("+ U: [" + _myTc.GetActualVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetActualCurrent() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetActualPower() + " kW]");
                //-- housekeeping
                _myTc.Disconnect();
                Console.WriteLine("\n now disconnected! ");
            }
            catch (Exception e) //-- only in case something failed!
            {
                Console.WriteLine(" An error occurred: [" + e.Message + "] \n-->> ending program.");
            }
            Console.WriteLine(" \n Press enter to leave: ");
            //Console.ReadLine();
        } //-- of constructor & functionality.
    } //-- of class
} //-- of using
