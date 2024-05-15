/*
 * ATHKS (Akustik Test Havuzu Konumlandırma Sistemi) class
 * February 2024
 * Creator: Cansın Canberi
 * 
 * This class represents the control system for the Akustik Test Havuzu Konumlandırma Sistemi (ATHKS).
 * It facilitates communication with both PLC and CNC units using Modbus TCP protocol.
 * The class provides methods for connecting to and disconnecting from the system, enabling/disabling the system, referencing the system, 
 * checking system status, setting and getting speed override, and reading/writing I/Os.
 * Controls 4 carries, each with x,y,z and c axes.
 */ 

using EasyModbus;
using System;
using System.Threading;

public class ATHKS
{
    private ModbusClientTCP modbusClient;
    private ModbusClient PLC;
    public Carrier carrier0;
    public Carrier carrier1;
    public Carrier carrier2;
    public Carrier carrier3;
    public float velOverride=1;
    //Class constructor with default IPv4, CNC and PLC ports
    public ATHKS(string IPv4Addr="192.168.0.10", int portCNC=1502, int portPLC=502)
    {
        modbusClient = new ModbusClientTCP(IPv4Addr, portCNC);
        PLC = new ModbusClient(IPv4Addr, portPLC);
        carrier0 = new Carrier(modbusClient, 0, 1, 3, 2);
        carrier1 = new Carrier(modbusClient, 0, 4, 6, 5);
        carrier2 = new Carrier(modbusClient, 7, 8, 10, 9);
        carrier3 = new Carrier(modbusClient, 7, 11, 13, 12);
    }
    //Connect to the ATHKS System, both PLC and CNC units
    public bool Connect()
    {
        try
        {
            modbusClient.Connect();
            PLC.Connect();
            modbusClient.Init();            
            return true;
        }
        catch
        {
            return false;
        }
    }
    //Disconnect from the ATHKS System
    public void Disconnect()
    {
        modbusClient.Disconnect();
        PLC.Disconnect();
    }
    //Enable the ATHKS System - all carriers
    public void Enable()
    {
        carrier0.Enable();
        carrier1.Enable();
        Thread.Sleep(1000);
        carrier2.Enable();
        carrier3.Enable();
    }
    //Disable the ATHKS System - all carriers
    public void Disable()
    {
        carrier0.Disable();
        carrier1.Disable();
        Thread.Sleep(500);
        carrier2.Disable();
        carrier3.Disable();
    }
    //Reference the ATHKS System one by one
    public void Reference()
    {
        carrier0.Reference();
        carrier1.Reference();
        carrier2.Reference();
        carrier3.Reference();
    }
    //Check the ATHKS System if enabled
    public bool IsEnabled()
    {
        return (carrier0.IsEnabled() && carrier1.IsEnabled() && carrier2.IsEnabled() && carrier3.IsEnabled());
    }
    //Check the ATHKS System is referenced
    public bool IsReferenced()
    {
        return (carrier0.IsReferenced() && carrier1.IsReferenced() && carrier2.IsReferenced() && carrier3.IsReferenced());
    }
    //Set ATHKS System is speed override, override=0 will pause the operation
    public void SetOverride(float velOverride)
    {
        modbusClient.SetSpeedOverride(this.velOverride);
    }
    //Get ATHKS System is speed override
    public float GetOverride()
    {
        return velOverride;
    }
    //Read System Messages
    public enum SystemMessage
    {
        Invalid = -1, // When data isn't as expected
        PressReady,   // Red LED active but no buzzer
        ServoError,   // Red LED and buzzer active
        ReadyToMove,  // Yellow (orange) LED active
        MoveActive  // Green LED active
    }
    //Read Status
    public SystemMessage GetSystemMessage()
    {

        bool[] values = PLC.ReadCoils(806, 4);

        if (values.Length != 4)
        {
            return SystemMessage.Invalid;
        }

        // Assign variables for each LED and buzzer
        bool redLed = values[0];
        bool orangeLed = values[1];
        bool greenLed = values[2];
        bool buzzer = values[3];

        // Determine the correct system message based on the conditions
        if (redLed && !buzzer)
        {
            return SystemMessage.PressReady;
        }
        else if (redLed && buzzer)
        {
            return SystemMessage.ServoError;
        }
        else if (orangeLed)
        {
            return SystemMessage.ReadyToMove;
        }
        else if (greenLed)
        {
            return SystemMessage.MoveActive;
        }

        // If none of the above conditions are met, return Invalid
        return SystemMessage.Invalid;
    }
    //Read I0 at given address
    public bool GetCoil(int address)
    {

        bool[] values = PLC.ReadCoils(address, 1);

        if (values.Length > 0)
        {
            return values[0];
        }
        return false;
    }
    public bool GetInput(int address)
    {

        bool[] values = PLC.ReadDiscreteInputs(address, 1);

        if (values.Length > 0)
        {
            return values[0];
        }
        return false;
    }
    //Set I0 at given address
    public void SetCoil(int address, bool value)
    {
        PLC.WriteSingleCoil(address, value);
    }
}
