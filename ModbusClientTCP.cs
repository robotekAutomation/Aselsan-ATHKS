/*
 * Robotek PLC and CNC unit control system communication class
 * February 2024
 * Creator: Cansın Canberi
 * 
 * This class provides functionality to communicate with Modbus TCP servers for controlling Robotek PLC and CNC units.
 * It includes methods for connecting to and disconnecting from the server, reading and writing words (16-bit integers) and floating-point values,
 * moving motors, checking motor status, enabling/disabling motors, referencing motors, and handling emergency stops.
 *
 * License: MIT License
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Threading;
using EasyModbus;

public class ModbusClientTCP
{
    private ModbusClient modbusClient;

    // Constructor
    public ModbusClientTCP(string ipAddress, int port)
    {
        modbusClient = new ModbusClient(ipAddress, port);
    }

    // Connect to the Modbus TCP server
    public void Connect()
    {
        modbusClient.Connect();
    }

    // Disconnect from the Modbus TCP server
    public void Disconnect()
    {
        modbusClient.Disconnect();
    }

    // Read a single word from the Modbus TCP server
    public ushort ReadWord(int address)
    {
        int[] registerValues = modbusClient.ReadInputRegisters(address, 1);
        return (ushort)registerValues[0];
    }

    // Write a single word to the Modbus TCP server
    public int WriteWord(int address, int value)
    {
        modbusClient.WriteSingleRegister(address, value);
        return 0;
    }
    public float ReadFloat(int address)
    {
        int[] registerValues = modbusClient.ReadInputRegisters(address, 2);
        byte[] bytes = new byte[4];
        bytes[0] = (byte)(registerValues[0] & 0xFF);
        bytes[1] = (byte)((registerValues[0] >> 8) & 0xFF);
        bytes[2] = (byte)(registerValues[1] & 0xFF);
        bytes[3] = (byte)((registerValues[1] >> 8) & 0xFF);

        return BitConverter.ToSingle(bytes, 0);
    }
    // Write a floating-point value to the Modbus TCP server
    public int WriteFloat(int address, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        int[] registerValues = new int[2];
        registerValues[1] = BitConverter.ToInt16(bytes, 2);
        registerValues[0] = BitConverter.ToInt16(bytes, 0);
        modbusClient.WriteMultipleRegisters(address, registerValues);
        return 0;
    }

    // Move the motor connected to the specified ID
    public void Move(int ID, float target, float speed, float acc, bool abs)
    {
        WriteFloat(3000 + ID * 2, target);
        WriteFloat(3100 + ID * 2, speed);
        WriteFloat(3200 + ID * 2, acc);
        if (abs)
        {
            WriteWord(3300 + ID, 101);
        }
        else
        {
            WriteWord(3300 + ID, 100);
        }
        bool tmp = IsMoving(ID);
        WriteWord(3300 + ID, 0);
    }

    // Check if the motor connected to the specified ID is moving
    public bool IsMoving(int ID)
    {
        ushort status = ReadWord(900 + ID);
        Thread.Sleep(10); // Prevent excessive usage
        return GetBit(status, 2) == 0;
    }

    // Enable the motor connected to the specified ID
    public void Enable(int ID)
    {
        WriteWord(3300 + ID, 10);
    }

    // Cancel the movement of the motor connected to the specified ID
    public void CancelMove(int ID)
    {
        WriteWord(3300 + ID, 80);
    }

    // Stop the motor connected to the specified ID in case of an emergency
    public void EmergencyStop(int ID)
    {
        WriteWord(3300 + ID, 90);
    }

    // Check if the motor connected to the specified ID is enabled
    public bool IsEnabled(int ID)
    {
        ushort status = ReadWord(900 + ID);
        Thread.Sleep(10); // Prevent excessive usage
        return GetBit(status, 0) == 1;
    }

    // Reference the motor connected to the specified ID
    public void Reference(int ID)
    {
        WriteWord(3300 + ID, 20);
    }

    // Check if the motor connected to the specified ID is referenced
    public bool IsReferenced(int ID)
    {
        ushort status = ReadWord(900 + ID);
        Thread.Sleep(10); // Prevent excessive usage
        return GetBit(status, 1) == 1;
    }

    // Initialize the Modbus TCP server
    public void Init()
    {
        WriteWord(2048, 1);
        WaitOn(1000, 2);
    }

    // Wait until the specified address holds the specified value
    public void WaitOn(int addr, int val)
    {
        while (ReadWord(addr) != val) ;
    }

    // Disable the motor connected to the specified ID
    public void Disable(int ID)
    {
        WriteWord(3300 + ID, 99);
    }

    // Get the position of the motor connected to the specified ID
    public float GetPos(int ID)
    {
        return ReadFloat(ID * 2);
    }

    // Get the velocity of the motor connected to the specified ID
    public float GetVel(int ID)
    {
        return ReadFloat(600 + ID * 2);
    }

    // Get the status of the motor connected to the specified ID
    public int GetStatus(int ID)
    {
        return ReadWord(900 + ID);
    }

    // Get the value of a specific bit in a word
    private int GetBit(ushort word, int bitPosition)
    {
        return (word >> bitPosition) & 1;
    }

    // Convert two words to a floating-point value
    private float WordArrayToFloat(ushort highOrderValue, ushort lowOrderValue)
    {
        byte[] bytes = new byte[4];
        bytes[2] = (byte)(highOrderValue >> 8);
        bytes[3] = (byte)(highOrderValue & 0xFF);
        bytes[0] = (byte)(lowOrderValue >> 8);
        bytes[1] = (byte)(lowOrderValue & 0xFF);
        return BitConverter.ToSingle(bytes, 0);
    }
}
