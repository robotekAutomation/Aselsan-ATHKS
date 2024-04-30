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
        // Create an instance of the ATHKS class
        ATHKS system = new ATHKS();

        // TEST0 - Connect to the Modbus server
        if (!system.Connect())
        {
            Console.WriteLine("Modbus connection failed...");
            return;
        }
        Console.WriteLine("Connected to the system over modbus!");

        // TEST1 - Enabling Carriers
        //enable carrier0.y axis
        system.carrier0.Enable('y');
        //enable all 4 carrier0 axess
        system.carrier0.Enable();
        //enable all 14 axes 
        system.Enable();
        if (!system.IsEnabled())
        {
            Console.WriteLine("Test2: System enable failed...");
            return;
        }
        Console.WriteLine("Test2: All Carriers are enabled!");

        // TEST2 - Referencing Carriers
        //reference carrier0.y axis
        system.carrier0.Reference('y');
        //reference all 4 carrier0 axess
        system.carrier0.Reference();
        //reference all 14 axes 
        system.Reference();
        if (!system.IsReferenced())
        {
            Console.WriteLine("Test3: System reference failed...");
            return;
        }
        Console.WriteLine("Test3: All Carriers are referenced!");

        // TEST3 - IOs
        system.SetCoil(800, true);
        if (!system.GetCoil(800))
        {
            Console.WriteLine("Test4: I/O test failed...");
            return;
        }
        Console.WriteLine("Test4: I/O test passed...");

        // TEST5 - Set Defaults
        Vector4D DefVel = new Vector4D(1000, 1000, 1000, 100);
        Vector4D DefAcc = new Vector4D(1000, 1000, 1000, 100);
        system.carrier0.SetDefaultSpeed(DefVel);
        system.carrier1.SetDefaultAcc(DefAcc);
        system.SetOverride(1.1f);
        Console.WriteLine("Defaults set for carriers...");

        // TEST6 - Individual Moves
        // Default vel and acc, absolute move to safety height, un/non-blocking move (does not wait for motion to finish)
        system.carrier0.UMoveZ(100, true, system.carrier0.safetyHeight);
        // A little delay between motions to see concurrency
        Thread.Sleep(2000);
        // Given vel and acc, incremental move
        Vector4D currentPosCarrier1 = system.carrier1.ReadPos();
        system.carrier1.UMoveZ(100, false, 50);
        // Wait for motions to finish, individual axes moves are nonblocking
        system.carrier0.WaitMotion();
        system.carrier1.WaitMotion();
        Vector4D currentPosCarrier0 = system.carrier0.ReadPos();
        // Calculate the target positions for carrier1
        Vector4D targetPosCarrier1 = currentPosCarrier1 + new Vector4D(0, 0, 100, 0);
        // Check if the actual positions match the target positions
        if (!(currentPosCarrier0 == new Vector4D(0, 0, 100, 0)) ||
            !(currentPosCarrier1 == targetPosCarrier1))
        {
            Console.WriteLine("Test5: Individual moves verification failed...");
            return;
        }
        Console.WriteLine("Test5: Individual moves verification successful.");

        // TEST7 - Carrier Moves (blocking)
        Vector4D Target0 = new Vector4D(100, 200, 300, 400);
        // Absolute move to the target with default parameters, blocking move (does wait for motion to finish)
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
            Console.WriteLine("Test6: Carrier moves verification failed...");
            return;
        }
        Console.WriteLine("Test6: Carrier moves verification successful.");

        // Disable and disconnect from the system
        system.Disable();
        system.Disconnect();
        Console.WriteLine("All tests passed successfully.");
    }
}
