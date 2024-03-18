/*
 * Functional Test Program for Automated Test Havuzu Konumlandırma Sistemi (ATHKS)
 * February 2024
 * Creator: Cansın Canberi
 * 
 * This program conducts functional tests for the ATHKS system, including connecting to the Modbus server,
 * enabling and referencing carriers, setting I/Os, overriding speeds, setting default speeds and accelerations,
 * and performing individual and grouped carrier moves.
 */

using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        // Replace "your_IP_address" with the actual values
        string ipAddress = "192.168.0.10";
        // Create an instance of the ATHKS class
        ATHKS system = new ATHKS(ipAddress);

        // TEST0 - Connect to the Modbus server
        if (!system.Connect())
        {
            Console.WriteLine("Modbus connection failed...");
            return;
        }
        Console.WriteLine("Connected to the system over modbus!");

        // TEST1 - Enabling Carriers
        system.Enable();
        if (!system.IsEnabled())
        {
            Console.WriteLine("System enable failed...");
            return;
        }
        Console.WriteLine("All Carriers are enabled!");

        // TEST2 - Referencing Carriers
        system.Reference();
        if (!system.IsReferenced())
        {
            Console.WriteLine("System reference failed...");
            return;
        }
        Console.WriteLine("All Carriers are referenced!");

        // TEST3 - IOs
        system.SetIO(800, true);
        if (!system.GetIO(800))
        {
            Console.WriteLine("I/O test failed...");
            return;
        }
        Console.WriteLine("I/O test passed...");

        // TEST4 - Override
        system.SetOverride(1.1f);
        if (!Math.Equals(system.GetOverride(), 1.1))
        {
            Console.WriteLine("System override failed...");
            return;
        }
        Console.WriteLine("Override test passed...");

        // TEST5 - Set Defaults
        Vector4D DefVel = new Vector4D(1000, 1000, 1000, 100);
        Vector4D DefAcc = new Vector4D(1000, 1000, 1000, 100);
        system.carrier0.SetDefaultSpeed(DefVel);
        system.carrier1.SetDefaultAcc(DefAcc);
        Console.WriteLine("Defaults set for carriers...");

        // TEST6 - Individual Moves (non-blocking)
        Vector4D currentPosCarrier0 = system.carrier0.ReadPos();
        Vector4D currentPosCarrier1 = system.carrier1.ReadPos();
        // Default vel and acc, absolute move to safety height
        system.carrier0.MoveZ(100, true, system.carrier0.safetyHeight);
        // A little delay between motions to see concurrency
        Thread.Sleep(2000);
        // Given vel and acc, incremental move
        system.carrier1.MoveZ(100, false, 50);
        // Wait for motions to finish, individual axes moves are nonblocking
        system.carrier0.WaitMotion();
        system.carrier1.WaitMotion();
        // Calculate the target positions for carrier1
        Vector4D targetPosCarrier1 = currentPosCarrier1 + new Vector4D(0, 0, 100, 0);
        // Check if the actual positions match the target positions
        if (!(currentPosCarrier0 == new Vector4D(0, 0, 100, 0)) ||
            !(currentPosCarrier1 == targetPosCarrier1))
        {
            Console.WriteLine("Test6: Individual moves verification failed...");
            return;
        }
        Console.WriteLine("Test6: Individual moves verification successful.");

        // TEST7 - Carrier Moves (blocking)
        Vector4D Target0 = new Vector4D(100, 200, 300, 400);
        // Absolute move to the target with default parameters
        system.carrier0.Move(Target0, true);
        // Incremental move to the target with given speed (half default)
        system.carrier1.Move(Target0, true, DefVel * 0.5f);
        // Moves to predefined positions
        system.carrier0.HomePos = new Vector4D(0, 0, 0, 0);
        system.carrier0.GoHome();
        system.carrier1.LoadPos = new Vector4D(10, 10, 10, -10);
        system.carrier1.GoLoad();
        // Check if the actual positions match the target positions
        if (!(currentPosCarrier0 == system.carrier0.HomePos) ||
            !(currentPosCarrier1 == system.carrier1.LoadPos))
        {
            Console.WriteLine("Test7: Carrier moves verification failed...");
            return;
        }
        Console.WriteLine("Test7: Carrier moves verification successful.");

        // Disable and disconnect from the system
        system.Disable();
        system.Disconnect();
        Console.WriteLine("All tests passed successfully.");
    }
}
