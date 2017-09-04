using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DEV = CH.Regatron.HPPS.Device;
using UI = CH.Regatron.HPPS.Device.TopCon.ControlInterface;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using System.Net;

using TopConAPI;
namespace BMS_Master_Control
{
    class Program
    {
        static void Main(string[] args)
        {
            //Code for opening com port and receiving data

            //SerialPort RegatronSerialPort = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);
            
            //Initialising arrays to store raw data
            int[] voltage = new int[] { 0, 0, 0, 0 };
            int[] temp = new int[] {  0, 0, 0, 0 };
            int[] SOC = new int[] { 0, 0, 0, 0 };
            int current = 0;

            //Initialising arrays to store float converted values
            float[] f_voltage = new float[] { 3.75F, 3.7F, 3.4F, 3.9F };
            float[] f_temp = new float[] { 0, 0, 0, 0 };
            float[] f_SOC = new float[] { 0, 0, 0, 0 };
            float f_current = 0;

            //Regatron Settings
            //---Charging Settings---
            float regatron_update_current = 0;

            //Initialising 
            float init_volt = (float)0;
            float init_current = (float)0;
            float init_power = (float)0;
            int time = 0;
            UInt16 cell_balance = 1;
            bool isFinalStageOfCharging = false;

            bool isCharging;
            char[] delimiter = new char[] { ' ', '\n' };

            Console.WriteLine("For charging, press the character 'c' then press Enter. For discharging, press the character 'd' then press Enter.");
            Console.Write("Charging or discharging: ");
            string c_d = Console.ReadLine();

            if(c_d == "c")
            {
                isCharging = true;
                init_volt = (float)18;

                Console.Write("Please specify charging current: ");
                string charging_current = Console.ReadLine();
                init_current = float.Parse(charging_current);

                if(init_current > 15)
                {
                    Console.WriteLine("Charging current exceeding 2C (15A) limit.");
                    init_current =(float)7.5;
                }
                Console.WriteLine("Batteries will be charged at " + init_current + "A");
                init_power = (float)0.2;

                regatron_update_current = init_current;
            }
            else
            {
                isCharging = false;
                init_volt = (float)0;
                //Discharging at 7.5A, when regatron is set to 0. As electronic load is set to sink 7.5A.
                Console.WriteLine("Batteries will be discharged at 7.5A");
                init_current = (float)0;
                init_power = (float)0;
            }

            //Initialisation of Regatron
            //Console.WriteLine(init_volt + " " + init_current + " " + init_power + " " + regatron_reference_current);
            //Console.WriteLine("What values regatron will receive: Voltage: " + init_volt + " Current: " + init_current + " Power: " + init_power);
            Console.ReadKey();
            new TC_Update(ref init_volt, ref regatron_update_current, ref init_power);

            SerialPort BMSSerialPort = new SerialPort("COM11", 9600, Parity.None, 8, StopBits.One);

            BMSSerialPort.Open();
            Console.WriteLine("--------Opened BMS Serial Port--------");
            BMSSerialPort.Write(c_d);
            Thread.Sleep(1000);

            string current_time = DateTime.Now.ToString("yyyy-MM-dd-HH:mm");
            while (true)
            {
                Thread.Sleep(2000);
                float voltVal = 0;
                BMSSerialPort.Write("s");
                string toParse = BMSSerialPort.ReadLine();
                while (toParse.Length < 52)
                {
                    BMSSerialPort.DiscardInBuffer();
                    toParse = BMSSerialPort.ReadLine();
                }

                //string toParse = "V1 36700 28200 8650 V2 37000 30300 8820 V6 36800 29800 8790 V7 37000 31300 9050 I 15000 t 678\n";
                string[] words = toParse.Split(delimiter);
                Console.WriteLine(toParse);

                splitToArrays(ref words, ref voltage, ref temp, ref SOC, ref current, ref time, ref cell_balance);
                arrayConversion(ref f_voltage, ref f_temp, ref f_SOC, ref f_current, ref voltage, ref temp, ref SOC, ref current);

                Console.WriteLine("Voltages: " + f_voltage[0] + " " + f_voltage[1] + " " +  f_voltage[2] + " " + f_voltage[3]);

                if (isCharging)
                {
                    voltVal = f_voltage.Max();
                }
                else
                {
                    voltVal = f_voltage.Min();
                }
                
                
                Console.WriteLine("Current: " + f_current);
                Console.WriteLine("Time: " + time);

                while (Console.KeyAvailable)
                    Console.ReadKey(true);

                new Program().pushToFirebase(f_voltage, f_temp, f_SOC, f_current, time, isCharging, current_time).Wait();

                //Stop discharging if voltage is below threshold
                if ((!isCharging) && (voltVal < 3.2F))
                {
                    BMSSerialPort.Close();
                    Console.WriteLine("--------Closed BMS Serial Port--------");

                    bool switchSupplyOff = true;
                    float end_voltage = (float)18;
                    float end_current = (float)7.55; //Supply slightly more than current sinking to load (7.5A)
                    float end_power = (float)0;

                    Console.WriteLine("What values regatron will receive: Voltage: " + end_voltage + " Current: " + end_current + " Power: " + end_power);

                    //Last TC update to effectively have cells at "rest"
                    new TC_Update(ref end_voltage, ref end_current, ref end_power);

                    Console.WriteLine("Cells Discharged. To Switch off TopCon Supply Press Enter.");
                    Console.WriteLine("Note: Doing so will cause batteries to discharge if electronic load is still connected");
                    Console.ReadKey();
                    new TC_Switch_Off(switchSupplyOff);
                    Environment.Exit(0);
                }

                //Stop charging if voltage is above threshold
                if ((isCharging) && (voltVal > 4.18F))
                {

                    while (!isFinalStageOfCharging)
                    {

                        if (regatron_update_current > 1.5F)
                        {
                            BMSSerialPort.Close();
                            Console.WriteLine("--------Closed BMS Serial Port--------");
                            regatron_update_current = regatron_update_current - 1;
                            Console.WriteLine("What values regatron will receive: Voltage: " + init_volt + " Current: " + regatron_update_current + " Power: " + init_power);
                            new TC_Update(ref init_volt, ref regatron_update_current, ref init_power);
                            Console.WriteLine("--------Opened BMS Serial Port--------");
                            BMSSerialPort.Open();
                        }
                        else if ((regatron_update_current > 1) && (regatron_update_current <= 1.5F))
                        {
                            BMSSerialPort.Close();
                            regatron_update_current = 1;
                            Console.WriteLine("What values regatron will receive: Voltage: " + init_volt + " Current: " + regatron_update_current + " Power: " + init_power);
                            new TC_Update(ref init_volt, ref regatron_update_current, ref init_power);
                            Console.WriteLine("--------Opened BMS Serial Port--------");
                            BMSSerialPort.Open();
                        }
                        else if ((regatron_update_current > 0.5) && (regatron_update_current <= 1))
                        {
                            BMSSerialPort.Close();
                            regatron_update_current = (float)0.5;
                            Console.WriteLine("What values regatron will receive: Voltage: " + init_volt + " Current: " + regatron_update_current + " Power: " + init_power);
                            new TC_Update(ref init_volt, ref regatron_update_current, ref init_power);
                            Console.WriteLine("--------Opened BMS Serial Port--------");
                            BMSSerialPort.Open();
                        }
                        else if ((regatron_update_current <= (float)0.5) && (voltVal >= 4.19))
                        {
                            BMSSerialPort.Close();
                            regatron_update_current = (float)0.15;
                            isFinalStageOfCharging = true;
                            Console.WriteLine("Final Stage of Charging");
                            Console.WriteLine("What values regatron will receive: Voltage: " + init_volt + " Current: " + regatron_update_current + " Power: " + init_power);
                            new TC_Update(ref init_volt, ref regatron_update_current, ref init_power);
                            Console.WriteLine("--------Opened BMS Serial Port--------");
                            BMSSerialPort.Open();
                        }
                        else
                        {
                            break;
                        }
                    }

                    Console.WriteLine("This is the value of the voltVal: " + voltVal);

                    while (isFinalStageOfCharging)
                    {
                        if (voltVal >= 4.2F)
                        {
                            BMSSerialPort.Close();
                            Console.WriteLine("--------Closed BMS Serial Port--------");

                            bool switchSupplyOff = true;
                            float end_voltage = (float)0;
                            float end_current = (float)0;
                            float end_power = (float)0;

                            Console.WriteLine("What values regatron will receive: Voltage: " + end_voltage + " Current: " + end_current + " Power: " + end_power);
                            //Last TC update to effectively have cells at "rest"
                            new TC_Update(ref end_voltage, ref end_current, ref end_power);

                            if (cell_balance == (UInt16)0)
                            {
                                Console.WriteLine("Cells Charged and Balanced.");
                                Console.WriteLine("To Switch off TopCon Supply Press Enter.");
                            }
                            else
                            {
                                Console.WriteLine("Cells Charged but not fully balanced.");
                                Console.WriteLine("To Switch off TopCon Supply Press Enter.");
                            }
                            Console.ReadKey();
                            new TC_Switch_Off(switchSupplyOff);
                            Environment.Exit(0);
                        }else
                        {
                            break;
                        }

                    }



                }


            }



        }

        static void splitToArrays(ref string[] words, ref int[] v, ref int[] t, ref int[] soc, ref int cur, ref int tm, ref UInt16 bal)
        {
            int arrayCounter = 0;

            for (int i = 1; i < (words.Length - 7); i = i + 4)
            {
                //Arrays of unedited voltages, temps and SOCs from received dataPacket.
                v[arrayCounter] = Convert.ToInt32(words[i]);
                t[arrayCounter] = Convert.ToInt32(words[i + 1]);
                soc[arrayCounter] = Convert.ToInt32(words[i + 2]);
                arrayCounter++;
            }
            //Unedited received current value
            cur = Convert.ToInt32(words[words.Length - 5]);
            tm = Convert.ToInt32(words[words.Length - 3]);
            bal = Convert.ToUInt16(words[words.Length - 1]);

        }

        static void arrayConversion(ref float[] f_v, ref float[] f_t, ref float[] f_soc, ref float f_cur, ref int[] v, ref int[] t, ref int[] soc, ref int cur)
        {
            //Conversion to float of actual voltages, temps, SOCS and Current.
            f_v = Array.ConvertAll(v, x => (float)x / 10000);
            f_t = Array.ConvertAll(t, x => (float)x / 100);
            f_soc = Array.ConvertAll(soc, x => (float)x / 100);
            f_cur = ((float)cur) / (float)1000.0;
        }

		private async Task pushToFirebase(float[] voltage, float[] temperature, float[] state_of_charge, float current, int time_elapsed, bool is_charging, string current_time)
        {
            var firebaseUrl = "https://battery-monitor-3ffa3.firebaseio.com";
            var firebase = new FirebaseClient(firebaseUrl);

            StatusData status = new StatusData();
            status.cell1_voltage = voltage[0];
            status.cell2_voltage = voltage[1];
            status.cell7_voltage = voltage[2];
            status.cell8_voltage = voltage[3];

            status.cell1_soc = state_of_charge[0];
            status.cell2_soc = state_of_charge[1];
            status.cell7_soc = state_of_charge[2];
            status.cell8_soc = state_of_charge[3];

            status.cell1_temp = temperature[0];
            status.cell2_temp = temperature[1];
            status.cell7_temp = temperature[2];
            status.cell8_temp = temperature[3];

            status.current = current;
            status.pack_voltage = voltage.Sum();
            status.time_elapsed = time_elapsed;
            status.is_charging = is_charging;

            await firebase
			  .Child("status")
                .PutAsync(status);

            var logs = await firebase
                    .Child("logs")
                    .Child(current_time)
                .PostAsync(status);

            Console.WriteLine("Pushed to Firebase.");

		}
    }
}

namespace TopConAPI
{
    class TC_Update
    {
        //-- this is the 'main' functionality: creating a TopCon object, connecting and asking for information
        public TC_Update(ref float ref_voltage, ref float ref_current, ref float ref_power)
        {
            //-- create a TopCon object (use as DEVICE_1 of three)
            DEV.TopCon _myTc = new DEV.TopCon(
                new DEV.TopConConfiguration_Dummy(),
                TopCon.Broker.Device.DEVICE_1);
            //-- ask for COMport to be used
            try
            {
                int comportNumber = 6;
                Console.Write(" TopCon object created \n Now trying to connect to TopCon on COM [" + comportNumber + "] : ");
                //-- connect to the TopCon
                _myTc.Connect(comportNumber);
                //Console.Write(" - connected; \n\n Now trying to fetch control interface to RS232: ");
                //-- now grab the control to the RS232 interface CHECK PAGE15/66
                _myTc.SetControlInterface(UI.Rs232);
                //Console.WriteLine(" - done -->> if HMI available: [Remote] LED is lit \n\n Now trying to read some information from the TopCon device :");
                //-- Read serial number and present that to the user
                //Console.WriteLine("\n+ Serial number of device = [" + _myTc.GetSerialNumberOfDevice() + "]");

                //-- update TopCon configuration (needed 1x)
                _myTc.UpdateTopConConfigurationWithTopConData();

                //Set the TopCon Power Supply Limits. Values in Volts, Amps and kiloWatts respectively
                _myTc.SetReferenceVoltage(ref_voltage);
                _myTc.SetReferenceCurrent(ref_current);
                _myTc.SetReferencePower(ref_power);

                //Set the Sink Limits (Q4). Values in Volts, Amps and kiloWatts respectively
                _myTc.SetLimitVoltageQ4(0);
                _myTc.SetLimitCurrentQ4(0);
                _myTc.SetLimitPowerQ4(0);

                //DEV.TopConConfiguration myTCconfig = new TopConConfiguration_DummY();

                _myTc.SetPowerON();

                Console.WriteLine("\n TopCon status is : [{0:D}]]", _myTc.GetSystemState());
                Console.WriteLine(" TopCon is in RUN? " + _myTc.IsInRunState());
                Console.WriteLine(" TopCon is in Ready?" + _myTc.IsInReadyState());

                //-- Read reference values for UIP and present them
                Console.WriteLine("Reference values: ");
                Console.WriteLine("+ U: [" + _myTc.GetReferenceVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetReferenceCurrent() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetReferencePower() + " kW]");

                //--Read actual values for UIP and present them
                Console.WriteLine("\nActual values: ");
                Console.WriteLine("+ U: [" + _myTc.GetActualVoltage() + " V]");
                Console.WriteLine("+ I: [" + _myTc.GetActualCurrent() + " A]");
                Console.WriteLine("+ P: [" + _myTc.GetActualPower() + " kW]");

                //_myTc.SetPowerOFF();

                //-- housekeeping
                _myTc.Disconnect();

                //Console.WriteLine("If disconnected the value should be zero: " + _myTc.GetConnectedCOMPortNumber());
                Console.WriteLine("\n now disconnected! ");

            }
            catch (Exception e) //-- only in case something failed!
            {
                Console.WriteLine(" An error occurred: [" + e.Message + "] \n-->> ending program.");
            }
            //Console.WriteLine(" \n Press enter to leave: ");
            while (Console.KeyAvailable)
                Console.ReadKey(true);
        } //-- of constructor & functionality.
    } //-- of class

    class TC_Switch_Off
    {
        //-- this is the 'main' functionality: creating a TopCon object, connecting and asking for information
        public TC_Switch_Off(bool switchSupplyOff)
        {
            //-- create a TopCon object (use as DEVICE_1 of three)
            DEV.TopCon _myTc = new DEV.TopCon(
                new DEV.TopConConfiguration_Dummy(),
                TopCon.Broker.Device.DEVICE_1);
            //-- ask for COMport to be used
            try
            {
                int comportNumber = 6;
                //-- connect to the TopCon
                _myTc.Connect(comportNumber);

                Console.Write(" Now trying to turn off to TopCon Device");
                //-- now grab the control to the RS232 interface CHECK PAGE15/66
                _myTc.SetControlInterface(UI.Rs232);

                //-- update TopCon configuration (needed 1x)
                _myTc.UpdateTopConConfigurationWithTopConData();

                _myTc.SetReferenceVoltage(0);
                _myTc.SetReferenceCurrent(0);
                _myTc.SetReferencePower(0);

                _myTc.SetPowerOFF();
                Console.WriteLine("\n TopCon status is : [{0:D}]]", _myTc.GetSystemState());
                Console.WriteLine(" TopCon is in RUN? " + _myTc.IsInRunState());
                Console.WriteLine(" TopCon is in Ready?" + _myTc.IsInReadyState());
                Console.ReadKey();

                //-- housekeeping
                _myTc.Disconnect();

                Console.WriteLine("\n System off and now disconnected! ");
            }
            catch (Exception e) //-- only in case something failed!
            {
                Console.WriteLine(" An error occurred: [" + e.Message + "] \n-->> ending program.");
            }
            Console.WriteLine(" \n Press enter to leave: ");
            Console.ReadLine();
        } //-- of constructor & functionality.

    } //-- of class
}//-- of using



/*while (true)
{
string toParse = "V1 31700 28200 8650 V2 37000 30300 8820 V6 36800 29800 8790 V7 37000 31300 9050 I 15000 t 678 b 0\n";
string[] words = toParse.Split(delimiter);
float voltVal = 0;

splitToArrays(ref words, ref voltage, ref temp, ref SOC, ref current, ref time, ref isBalanced);
arrayConversion(ref f_voltage, ref f_temp, ref f_SOC, ref f_current, ref voltage, ref temp, ref SOC, ref current);

Console.WriteLine("Voltages: " + f_voltage[0] + " " + f_voltage[1] + " " + f_voltage[2] + " " + f_voltage[3]);

if (isCharging)
{
    voltVal = f_voltage.Max();
}
else
{
    voltVal = f_voltage.Min();
}


Console.WriteLine("Current: " + f_current);
Console.WriteLine("Time: " + time);

//new Program().pushToFirebase(f_voltage, f_temp, f_SOC, f_current, time, isCharging, current_time).Wait();

//Stop discharging if voltage is below threshold
if ((!isCharging) && (voltVal< 3.2F))
{
    //BMSSerialPort.Close();
    Console.WriteLine("--------Closed BMS Serial Port--------");

    bool switchSupplyOff = true;
float end_voltage = (float)18;
float end_current = (float)7.55; //Supply slightly more than current sinking to load (7.5A)
float end_power = (float)0;

Console.WriteLine("What values regatron will receive: Voltage: " + end_voltage + " Current: " + end_current + " Power: " + end_power);

    //Last TC update to effectively have cells at "rest"
    //new TC_Update(ref end_voltage, ref end_current, ref end_power, ref regatron_reference_current);

    Console.WriteLine("Cells Discharged \n To Switch off TopCon Supply Press Enter.");
    Console.WriteLine("Note: Doing so will cause batteries to discharge if electronic load is still connected");
    Console.ReadKey();
    //new TC_Switch_Off(switchSupplyOff);
    Environment.Exit(0);
}
}*/