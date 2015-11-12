/*
 * Device.cs - Developed by Dan Wager for AndroidLib.dll
 */

using System.IO;
using System.Threading;

namespace RegawMOD.Android
{
    /// <summary>
    /// Manages connected Android device's info and commands
    /// </summary>
    public partial class Device
    {
        private BatteryInfo battery;
        private BuildProp buildProp;
        private BusyBox busyBox;
        private FileSystem fileSystem;
        //private PackageManager packageManager;
        private Phone phone;
        //private Processes processes;
        private Su su;
        private string serialNumber;
        private DeviceState state;

        /// <summary>
        /// Initializes a new instance of the Device class
        /// </summary>
        /// <param name="deviceSerial">Serial number of Android device</param>
        internal Device(string deviceSerial)
        {
            this.serialNumber = deviceSerial;
            Update();
        }

        private DeviceState SetState()
        {
            string state = null;

            using (StringReader r = new StringReader(Adb.Devices()))
            {
                string line;

                while (r.Peek() != -1)
                {
                    line = r.ReadLine();

                    if (line.Contains(this.serialNumber))
                        state = line.Substring(line.IndexOf('\t') + 1);
                }
            }

            if (state == null)
            {
                using (StringReader r = new StringReader(Fastboot.Devices()))
                {
                    string line;

                    while (r.Peek() != -1)
                    {
                        line = r.ReadLine();

                        if (line.Contains(this.serialNumber))
                            state = line.Substring(line.IndexOf('\t') + 1);
                    }
                }
            }

            switch (state)
            {
                case "device":
                    return DeviceState.ONLINE;
                case "recovery":
                    return DeviceState.RECOVERY;
                case "fastboot":
                    return DeviceState.FASTBOOT;
                case "sideload":
                    return DeviceState.SIDELOAD;
                case "unauthorized":
                    return DeviceState.UNAUTHORIZED;
                default:
                    return DeviceState.UNKNOWN;
            }
        }

        /// <summary>
        /// Gets the device's <see cref="BatteryInfo"/> instance
        /// </summary>
        /// <remarks>See <see cref="BatteryInfo"/> for more details</remarks>
        public BatteryInfo Battery { get { return this.battery; } }

        /// <summary>
        /// Gets the device's <see cref="BuildProp"/> instance
        /// </summary>
        /// <remarks>See <see cref="BuildProp"/> for more details</remarks>
        public BuildProp BuildProp { get { return this.buildProp; } }

        /// <summary>
        /// Gets the device's <see cref="BusyBox"/> instance
        /// </summary>
        /// <remarks>See <see cref="BusyBox"/> for more details</remarks>
        public BusyBox BusyBox { get { return this.busyBox; } }

        /// <summary>
        /// Gets the device's <see cref="FileSystem"/> instance
        /// </summary>
        /// <remarks>See <see cref="FileSystem"/> for more details</remarks>
        public FileSystem FileSystem { get { return this.fileSystem; } }
        
        ///// <summary>
        ///// Gets the device's <see cref="PackageManager"/> instance
        ///// </summary>
        ///// <remarks>See <see cref="PackageManager"/> for more details</remarks>
        //public PackageManager PackageManager { get { return this.packageManager; } }

        /// <summary>
        /// Gets the device's <see cref="Phone"/> instance
        /// </summary>
        /// <remarks>See <see cref="Phone"/> for more details</remarks>
        public Phone Phone { get { return this.phone; } }

        ///// <summary>
        ///// Gets the device's <see cref="Processes"/> instance
        ///// </summary>
        ///// <remarks>See <see cref="Processes"/> for more details</remarks>
        //public Processes Processes { get { return this.processes; } }

        /// <summary>
        /// Gets the device's <see cref="Su"/> instance
        /// </summary>
        /// <remarks>See <see cref="Su"/> for more details</remarks>
        public Su Su { get { return this.su; } }

        /// <summary>
        /// Gets the device's serial number
        /// </summary>
        public string SerialNumber { get { return this.serialNumber; } }

        /// <summary>
        /// Gets a value indicating the device's current state
        /// </summary>
        /// <remarks>See <see cref="DeviceState"/> for more details</remarks>
        public DeviceState State { get { return this.state; } internal set { this.state = value; } }

        /// <summary>
        /// Gets a value indicating if the device has root
        /// </summary>
        public bool HasRoot { get { return this.su.Exists; } }

        /// <summary>
        /// Reboots the device regularly from fastboot
        /// </summary>
        public void FastbootReboot()
        {
            if (this.State == DeviceState.FASTBOOT)
                new Thread(new ThreadStart(FastbootRebootThread)).Start();
        }

        private void FastbootRebootThread()
        {
            Fastboot.ExecuteFastbootCommandNoReturn(Fastboot.FormFastbootCommand(this, "reboot"));
        }

        /// <summary>
        /// Reboots the device regularly
        /// </summary>
        public void Reboot()
        {
            new Thread(new ThreadStart(RebootThread)).Start();
        }

        private void RebootThread()
        {
            Adb.ExecuteAdbCommandNoReturn(Adb.FormAdbCommand(this, "reboot"));
        }

        /// <summary>
        /// Reboots the device into recovery
        /// </summary>
        public void RebootRecovery()
        {
            new Thread(new ThreadStart(RebootRecoveryThread)).Start();
        }

        private void RebootRecoveryThread()
        {
            Adb.ExecuteAdbCommandNoReturn(Adb.FormAdbCommand(this, "reboot", "recovery"));
        }

        /// <summary>
        /// Reboots the device into the bootloader
        /// </summary>
        public void RebootBootloader()
        {
            new Thread(new ThreadStart(RebootBootloaderThread)).Start();
        }

        private void RebootBootloaderThread()
        {
            Adb.ExecuteAdbCommandNoReturn(Adb.FormAdbCommand(this, "reboot", "bootloader"));
        }

        /// <summary>
        /// Pulls a file from the device
        /// </summary>
        /// <param name="fileOnDevice">Path to file to pull from device</param>
        /// <param name="destinationDirectory">Directory on local computer to pull file to</param>
        /// /// <param name="timeout">The timeout for this operation in milliseconds (Default = -1)</param>
        /// <returns>True if file is pulled, false if pull failed</returns>
        public bool PullFile(string fileOnDevice, string destinationDirectory, int timeout = Command.DEFAULT_TIMEOUT)
        {
            AdbCommand adbCmd = Adb.FormAdbCommand(this, "pull", "\"" + fileOnDevice + "\"", "\"" + destinationDirectory + "\"");
            return (Adb.ExecuteAdbCommandReturnExitCode(adbCmd.WithTimeout(timeout)) == 0);
        }

        /// <summary>
        /// Pushes a file to the device
        /// </summary>
        /// <param name="filePath">The path to the file on the computer you want to push</param>
        /// <param name="destinationFilePath">The desired full path of the file after pushing to the device (including file name and extension)</param>
        /// <param name="timeout">The timeout for this operation in milliseconds (Default = -1)</param>
        /// <returns>If the push was successful</returns>
        public bool PushFile(string filePath, string destinationFilePath, int timeout = Command.DEFAULT_TIMEOUT)
        {
            AdbCommand adbCmd = Adb.FormAdbCommand(this, "push", "\"" + filePath + "\"", "\"" + destinationFilePath + "\"");
            return (Adb.ExecuteAdbCommandReturnExitCode(adbCmd.WithTimeout(timeout)) == 0);
        }

        /// <summary>
        /// Pulls a full directory recursively from the device
        /// </summary>
        /// <param name="location">Path to folder to pull from device</param>
        /// <param name="destination">Directory on local computer to pull file to</param>
        /// <param name="timeout">The timeout for this operation in milliseconds (Default = -1)</param>
        /// <returns>True if directory is pulled, false if pull failed</returns>
        public bool PullDirectory(string location, string destination, int timeout = Command.DEFAULT_TIMEOUT)
        {
            AdbCommand adbCmd = Adb.FormAdbCommand(this, "pull", "\"" + (location.EndsWith("/") ? location : location + "/") + "\"", "\"" + destination + "\"");
            return (Adb.ExecuteAdbCommandReturnExitCode(adbCmd.WithTimeout(timeout)) == 0);
        }

        /// <summary>
        /// Installs an application from the local computer to the Android device
        /// </summary>
        /// <param name="location">Full path of apk on computer</param>
        /// <param name="timeout">The timeout for this operation in milliseconds (Default = -1)</param>
        /// <returns>True if install is successful, False if install fails for any reason</returns>
        public bool InstallApk(string location, int timeout = Command.DEFAULT_TIMEOUT)
        {
            return !Adb.ExecuteAdbCommand(Adb.FormAdbCommand(this, "install", "\"" + location + "\"").WithTimeout(timeout), true).Contains("Failure");
        }


        /// <summary>
        /// Experimental Root Method Might be what Kingo Root Uses
        /// </summary>
        /// <returns>True Or False For Rooted Device</returns>
        public bool RootDevice(string BusyBoxLocation,string SULocation,string SUAPKs)
        {
            //this method is generated from 
            //http://forum.xda-developers.com/showthread.php?t=2684210
            //have a look if your interested
            bool rtnBool = false;

            //Lets get the steps here


            //step 1
            //Push the files to the local tmp folder
            //adb push busybox /data/local/tmp 
            if (PushFile(BusyBoxLocation, "/data/local/tmp"))
            {
                //Proceed To Step 2
                //adb push su /data/local/tmp 
                if(PushFile(SULocation,"/data/local/tmp"))
                {
                    //Proeed To Step 3
                    //run SU Shell
                    PushFile(SUAPKs, "/data/local/tmp");

                    try
                    {
                        AdbCommand adbCmd = Adb.FormAdbShellCommand(this,true, "chmod","6755","/data/local/tmp/su ");
                        Adb.ExecuteAdbCommandNoReturn(adbCmd);

                        adbCmd = Adb.FormAdbShellCommand(this, true, "chmod", "755", "/data/local/tmp/busybox");
                        Adb.ExecuteAdbCommandNoReturn(adbCmd);


                        adbCmd = Adb.FormAdbShellCommand(this, true, "chmod", "644", "/data/local/tmp/Superuser.apk");
                        Adb.ExecuteAdbCommandNoReturn(adbCmd);


                        rtnBool = true;
                    }
                    catch
                    {
                        rtnBool = false;
                    }
                }
                else
                {
                    rtnBool = false;
                }
            }
            else
            {
                return rtnBool = false;
            }
            return rtnBool;
        }

        /// <summary>
        /// Uninstalls an application from the Android Device but needs com.name
        /// </summary>
        /// <param name="comname">The com name (e.g. com.name)</param>
        /// <param name="timeout">The timeout for this operation in milliseconds (Default = -1)</param>
        /// <returns>True if uninstall is successful, False if install fails for any reason</returns>
        public bool UninstallApk(string comname, int timeout = Command.DEFAULT_TIMEOUT)
        {
            return !Adb.ExecuteAdbCommand(Adb.FormAdbCommand(this, "uninstall", comname).WithTimeout(timeout), true).Contains("Failure");
        }

        /// <summary>
        /// Uninstalls an application from the Android Device but needs com.name
        /// </summary>
        /// <param name="comname">The Application name (e.g. TESTAPP)</param>
        /// <param name="timeout">The timeout for this operation in milliseconds (Default = -1)</param>
        /// <returns>True if uninstall is successful, False if install fails for any reason</returns>

        public bool UninstallApkName(string AppName, int timeout = Command.DEFAULT_TIMEOUT)
        {
            if(Adb.ExecuteAdbCommand(Adb.FormAdbCommand(this,"shell", "pm list packages -f " + AppName ).WithTimeout(timeout), true).Contains("apk="))
            {
                string name = Adb.ExecuteAdbCommand(Adb.FormAdbCommand(this, "shell", "pm list packages -f " + AppName).WithTimeout(timeout), true);
                name = name.Substring(name.IndexOf('=') + 1);
                return !Adb.ExecuteAdbCommand(Adb.FormAdbCommand(this, "shell", "'pm list packages -f " + name + "'").WithTimeout(timeout), true).Contains("Failure");
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Unlocks the device and goes to the home screen
        /// </summary>

        public void unlock()
        {
            AdbCommand adbCmd = Adb.FormAdbCommand("svc","power","stayon","usb");
            Adb.ExecuteAdbCommandNoReturn(adbCmd);
            adbCmd = Adb.FormAdbShellCommand(this, false, "input", "keyevent 82 # unlock");
            Adb.ExecuteAdbCommandNoReturn(adbCmd);
        }


        /// <summary>
        /// Updates all values in current instance of <see cref="Device"/>
        /// </summary>
        public void Update()
        {
            this.state = SetState();

            this.su = new Su(this);
            this.battery = new BatteryInfo(this);
            this.buildProp = new BuildProp(this);
            this.busyBox = new BusyBox(this);
            this.phone = new Phone(this);
            this.fileSystem = new FileSystem(this);
        }


        /// <summary>
        /// Saves a screen shot for the device on the local disk to make it more user friendly
        /// </summary>
        /// <param name="savelocation">local disk save location</param>
        public void CaputrueScreenShot(string savelocation)
        {
            
            //Take The Screen shot
            Adb.ExecuteAdbCommandNoReturn(Adb.FormAdbShellCommand(this,false, "screencap /sdcard/screen.png"));
            //Pull the data
            if (File.Exists(savelocation + @"\screen.png"))
            {
                File.Delete(savelocation + @"\screen.png");
            }
            PullFile("/sdcard/screen.png", savelocation + @"\screen.png");

        }
    }
}